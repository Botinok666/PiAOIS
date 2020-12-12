using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.DataVisualization;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using Windows.UI;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using PiAOIS.Data;
using PiAOIS.Util;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PiAOIS
{
    using ChartPoint = Tuple<DateTime, double>;
    using ChartCollection = ObservableCollection<Tuple<DateTime, double>>;
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly DispatcherTimer dispatcher;
        private readonly string[] powerSelects = new string[]
        {
            "CPU Package Power",
            "GPU Power"
        };
        private readonly string[] tempSelects = new string[]
        {
            "Core Max",
            "GPU Temperature"
        };
        private const string dbServer = "mongodb://localhost:27017";
        private readonly IMongoDatabase mongoDatabase;
        public MainPage()
        {
            InitializeComponent();
            //Mongo driver setup
            BsonClassMap.RegisterClassMap<SensorModel>();
            BsonClassMap.RegisterClassMap<MeasurementModel>();
            var mongoClient = new MongoClient(dbServer);
            mongoDatabase = mongoClient.GetDatabase("machines");
            dispatcher = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000)
            };
            dispatcher.Tick += Dispatcher_Tick;
            //Init axes
            ChartTemperature.Axes.Add(new LinearAxis() 
            { 
                Title = new Title() { Content = "°C" },
                Orientation = AxisOrientation.Y,
                ShowGridLines = true
            });
            ChartPower.Axes.Add(new LinearAxis() 
            { 
                Title = new Title() { Content = "W" },
                Orientation = AxisOrientation.Y,
                ShowGridLines = true
            });
            ChartTemperature.Axes.Add(new DateTimeAxis()
            {
                Orientation = AxisOrientation.X
            });
            ChartPower.Axes.Add(new DateTimeAxis()
            {
                Orientation = AxisOrientation.X
            });
        }
        /// <summary>
        /// Задаёт для указанной диаграммы массивы точек
        /// </summary>
        /// <param name="chart">Диаграмма, в которой будут отображаться точки</param>
        /// <param name="collection">Набор метрик, полученных из БД</param>
        /// <param name="selects">Набор названий, по которым отбираются метрики для данной диаграммы</param>
        private void SetChartPoints(Chart chart, List<MeasurementModel> collection, string[] selects)
        {
            if (collection.Count < 1)
                return;
            //Сначала перегруппируем список измерений из иерархии в плоский вид. Для этого преобразуем
            //массивы значений метрик в плоский вид, и сразу же создадим анонимный класс, в котором
            //будет пара значений "измерение метрика". Так можно будет узнать, к какому измерению какая
            //метрика относится. Затем выберем только нужные нам метрики, указанные в списке selects.
            //Потом метрики нужно сгруппировать по полю Group, тогда получается список вида "ключ массив",
            //в котором ключ - название метрики, а массив содержит объекты анонимного класса
            collection
                .SelectMany(x => x.Sensors, (meas, sens) => new { meas, sens })
                .Where(x => selects.Contains(x.sens.Name))
                .GroupBy(x => x.sens.Group)
                .ToList()
                .ForEach(x =>
                {
                    //Найдём набор точек, соответствующий названию метрики из лямбды
                    var chartSeries = chart
                        .Series
                        .OfType<LineSeries>()
                        .Where(y => (y.Title as Title).Content.Equals(x.Key))
                        .FirstOrDefault();
                    if (chartSeries is null)
                    {
                        //Сюда программа зайдёт только один раз после запуска, т.к. набора точек ещё нет
                        chartSeries = new LineSeries()
                        {
                            ItemsSource = new ChartCollection(),
                            Title = new Title() { Content = x.Key },
                            IndependentValueBinding = new Binding() { Path = new PropertyPath("Item1") },
                            DependentValueBinding = new Binding() { Path = new PropertyPath("Item2") },
                            IsSelectionEnabled = true
                        };
                        chart.Series.Add(chartSeries);
                    }
                    //Преобразуем массив объектов анонимного класса в массив точек
                    //Для этого выберем из него время измерения и численное значение метрики
                    chartSeries.ItemsSource = new ChartCollection(x
                        .Select(y => new ChartPoint(
                            TimeZoneInfo.ConvertTimeFromUtc(y.meas.Time, TimeZoneInfo.Local), 
                            y.sens.NumericValue)
                        )
                    );
                });
        }
        private void TurnOnBtn_Click(object _0, RoutedEventArgs _1)
        {
            if (TurnOnBtn.IsOn)
            {
                dispatcher.Start();
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                dispatcher.Stop();
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
        private async void Dispatcher_Tick(object sender, object e)
        {
            //Выберем из таблицы БД 15 элементов без фильтра, но отсортированные по времени
            var collection = await mongoDatabase
                .GetCollection<MeasurementModel>("measurements")
                .Find(new BsonDocument())
                .SortByDescending(x => x.Time)
                .Limit(15)
                .ToListAsync();
            //Дешифруем все значения, и запишем что получилось в отдельное поле класса
            //Если в результате дешифрации получается не число, значит, представленный пароль 
            //неверный, и метод Decrypt возвращает NaN
            collection
                .SelectMany(x => x.Sensors)
                .ToList()
                .ForEach(x => x.NumericValue = Crypto.Decrypt(x.Value));
            //Проверим, удалось ли дешифровать. Если хотя бы один NaN есть, значит, не удалось
            if (collection
                .SelectMany(x => x.Sensors)
                .Any(x => double.IsNaN(x.NumericValue)))
            {
                //В таком случае прекратим запрашивать данные из БД
                //и запросим у пользователя пароль
                dispatcher.Stop();
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.Orange);
                var dialogResult = await PasswordDialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Primary)
                {
                    Crypto.SetPassword(userPassword.Password);
                    dispatcher.Start();
                }
                else
                {
                    TurnOnBtn.IsOn = false;
                    TurnOnBtn.Foreground = new SolidColorBrush(Colors.Black);
                }
                return; //Выходим, данных для отображения всё равно нет
            }
            //Если попали сюда, данные для отображения есть, выведем их на активную диаграмму
            if (TabCharts.SelectedIndex == 0)
                SetChartPoints(ChartTemperature, collection, tempSelects);
            else if (TabCharts.SelectedIndex == 1)
                SetChartPoints(ChartPower, collection, powerSelects);
        }
    }
}
