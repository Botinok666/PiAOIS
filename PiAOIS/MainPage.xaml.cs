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
        private void SetChartPoints(Chart chart, List<MeasurementModel> collection, string[] selects)
        {
            if (collection.Count < 1)
                return;
            collection
                .SelectMany(x => x.Sensors, (meas, sens) => new { meas, sens })
                .Where(x => selects.Contains(x.sens.Name))
                .GroupBy(x => x.sens.Group)
                .ToList()
                .ForEach(x =>
                {
                    var chartSeries = chart
                        .Series
                        .OfType<LineSeries>()
                        .Where(y => (y.Title as Title).Content.Equals(x.Key))
                        .FirstOrDefault();
                    if (chartSeries is null)
                    {
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
            var collection = await mongoDatabase
                .GetCollection<MeasurementModel>("measurements")
                .Find(new BsonDocument())
                .SortByDescending(x => x.Time)
                .Limit(15)
                .ToListAsync();
            collection
                .SelectMany(x => x.Sensors)
                .ToList()
                .ForEach(x => x.NumericValue = Crypto.Decrypt(x.Value));
            if (collection
                .SelectMany(x => x.Sensors)
                .Any(x => double.IsNaN(x.NumericValue)))
            {
                dispatcher.Stop();
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.Orange);
                //Ask user to provide password
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
                return;
            }
            if (TabCharts.SelectedIndex == 0)
                SetChartPoints(ChartTemperature, collection, tempSelects);
            else if (TabCharts.SelectedIndex == 1)
                SetChartPoints(ChartPower, collection, powerSelects);
        }
    }
}
