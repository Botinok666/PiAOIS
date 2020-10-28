using PiAOIS.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.DataVisualization;
using WinRTXamlToolkit.Tools;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using System.Collections.ObjectModel;
using Windows.Storage.Search;
using System.Threading;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI;
using PiAOIS.Data;

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
        private Sensors sensors = null;
        private readonly DispatcherTimer dispatcher;
        public MainPage()
        {
            InitializeComponent();
            Model.InitializeDatabase();
            dispatcher = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(Const.repeatRate)
            };
            dispatcher.Tick += Dispatcher_Tick;
        }
        private void SetChartSeries(List<IEnumerable<RemoteSensors>> collection)
        {
            if (collection.Count < 1 || collection[0].Count() < 1)
                return;
            var charts = new List<Chart>() { ChartPower, ChartVoltage, ChartTemperature };
            collection
                .GroupBy(x => x.First().SensorUnit)
                .ForEach(x => 
                {
                    int tab = TabCharts.SelectedIndex;
                    if (x.Key.Contains(Const.units[tab], StringComparison.OrdinalIgnoreCase))
                    {
                        if (charts[tab].Series.Count == 0)
                        {
                            x.ForEach(y => charts[tab].Series.Add(new LineSeries()
                            {
                                ItemsSource = new ChartCollection(),
                                Title = new Title() { Content = y.First().SensorName + ", " + x.Key },
                                IndependentValueBinding = new Binding() { Path = new PropertyPath("Item1") },
                                DependentValueBinding = new Binding() { Path = new PropertyPath("Item2") },
                                IsSelectionEnabled = true
                            }));
                        }
                        x.ForEach(y =>
                        {
                            string title = y.First().SensorName + ", " + x.Key;
                            var points = y
                                .Select(z =>
                                {
                                    DateTime time = DateTimeOffset
                                        .FromUnixTimeSeconds(z.SensorUpdateTime)
                                        .LocalDateTime;
                                    return new ChartPoint(time, z.Value);
                                });
                            var series = charts[tab].Series
                                .OfType<LineSeries>()
                                .Where(z => (z.Title as Title).Content.Equals(title))
                                .FirstOrDefault();
                            if (!(series is null))
                                series.ItemsSource = new ChartCollection(points);
                        });
                    }
                });
        }
        private void TurnOnBtn_Click(object _0, RoutedEventArgs _1)
        {
            if (TurnOnBtn.IsOn)
            {
                if (sensors is null)
                    sensors = new Sensors();
                dispatcher.Start();
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.Orange);
                DebugText.Text = Const.remoteStart;
            }
            else
            {
                dispatcher.Stop();
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.Black);
                DebugText.Text = Const.remoteStop;
            }
        }
        private async void Dispatcher_Tick(object sender, object e)
        {
            if (!TurnOnBtn.IsOn)
                return;
            if (await sensors?.PollSensors())
            {
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.ForestGreen);
                DebugText.Text = Const.remoteOk;
            }
            else
            {
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.Orange);
                DebugText.Text = Const.remoteErr;
            }
            var collection = (await Data.Data.GetInstance()
                .GetPoints(DBPass.Password)
                ).ToList();
            if (collection.Count < 1 || collection[0].Count() < 1)
                DebugText.Text = Const.incorrectPass;
            else
                SetChartSeries(collection);
        }
    }
}
