using PiAOIS.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        StorageFolder jsonFolder = null;
        Data.Data data = null;
        Sensors sensors = null;
        RandomData randomData = null;
        private int _isRunning = 0;
        public MainPage()
        {
            InitializeComponent();
            InitCharts();
        }
        private void InitCharts()
        { 
            new List<Chart>()
            {
                ChartTemperature, ChartTemperature, ChartHumidity, ChartHumidity, ChartWind
            }
            .Select((chart, idx) => new { chart, idx })
            .ForEach(pair => pair.chart.Series.Add(new LineSeries()
                {
                    ItemsSource = new ChartCollection(),
                    Title = new Title() { Content = Const.GetGraphs[pair.idx].Title },
                    IndependentValueBinding = new Binding() { Path = new PropertyPath("Item1") },
                    DependentValueBinding = new Binding() { Path = new PropertyPath("Item2") },
                    IsSelectionEnabled = true
                })
            );
            var chartSeries = GetChartSeries();
            for (int j = 0; j < Const.pointsCount; j++)
            {
                DateTime dateTime = DateTime.Now.AddSeconds(j - Const.pointsCount);
                chartSeries
                    .Select((chart, idx) => new { chart, idx })
                    .ForEach(pair => pair.chart.Add(
                        new ChartPoint(dateTime, Const.GetGraphs[pair.idx].DefaultValue)));
            }
        }
        private ChartCollection[] GetChartSeries()
        {
            return new ChartCollection[] {
                (ChartTemperature.Series[0] as LineSeries).ItemsSource as ChartCollection,
                (ChartTemperature.Series[1] as LineSeries).ItemsSource as ChartCollection,
                (ChartHumidity.Series[0] as LineSeries).ItemsSource as ChartCollection,
                (ChartHumidity.Series[1] as LineSeries).ItemsSource as ChartCollection,
                (ChartWind.Series[0] as LineSeries).ItemsSource as ChartCollection
            };
        }
        private async void TurnOnBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning == 0)
            {
                Interlocked.Exchange(ref _isRunning, 1);
                if (jsonFolder is null)
                {
                    await new Windows.UI.Popups.MessageDialog("Выберите папку для сохранения JSON файлов").ShowAsync();
                    FolderPicker folderPicker = new FolderPicker();
                    folderPicker.FileTypeFilter.Add("*");
                    jsonFolder = await folderPicker.PickSingleFolderAsync();
                    if (jsonFolder is null)
                    {
                        Interlocked.Exchange(ref _isRunning, 0);
                        return;
                    }
                    randomData = new RandomData(jsonFolder);
                    var names = Enum.GetValues(typeof(GraphKeys)).OfType<GraphKeys>();
                    for (int j = 0; j < names.Count(); j++)
                        randomData.AddSensor(names.ElementAt(j).ToString(), Const.GetGraphs[j].LowerBound,
                            Const.GetGraphs[j].UpperBound, Const.GetGraphs[j].DefaultValue);

                    data = Data.Data.GetInstance();
                    data.Points = GetChartSeries()
                        .Select(x => x.ToList())
                        .ToArray();
                    data.DataAdded += Data_DataAdded;
                    data.RainChanged += Data_RainChanged;
                    data.TempThreshold = double.NaN;

                    var queryResult = jsonFolder.CreateFileQueryWithOptions(
                        new QueryOptions(CommonFileQuery.DefaultQuery, new List<string>() { ".json" }));
                    queryResult.ContentsChanged += QueryResult_ContentsChanged;
                    var files = await queryResult.GetFilesAsync();
                    files.ForEach(async x => 
                    {
                        try
                        {
                            await x.DeleteAsync();
                        }
                        catch (FileNotFoundException) { }
                    });
                    sensors = new Sensors(queryResult);
                }
                randomData.Start();
                DebugText.Text += Environment.NewLine + Const.systemOn;
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.PaleGreen);
            }
            else
            {
                randomData?.Stop();
                DebugText.Text += Environment.NewLine + Const.systemOff;
                TurnOnBtn.Foreground = new SolidColorBrush(Colors.Black);
            }
            DebugScroll.ChangeView(0, DebugScroll.ScrollableHeight, 1);
        }

        private async void Data_RainChanged(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                if (data.IsRaining)
                {
                    WeatherImg.Source = new BitmapImage(new Uri(WeatherImg.BaseUri, "Assets/rain.png"));
                    DebugText.Text += Environment.NewLine + Const.rainyDay;
                }
                else
                {
                    WeatherImg.Source = new BitmapImage(new Uri(WeatherImg.BaseUri, "Assets/sun.png"));
                }
            });
        }

        private void QueryResult_ContentsChanged(IStorageQueryResultBase sender, object args)
        {
            sensors?.PollSensors();
        }

        private async void Data_DataAdded(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                var chartSeries = GetChartSeries();
                chartSeries.ForEach(x => x.Clear());
                for (int j = 0; j < chartSeries.Length; j++)
                    data.Points[j].ForEach(x => chartSeries[j].Add(x));
                SetFrontEndValues();
                HandleTriggers();
                if (DebugText.Text.Count(c => c.Equals('\n')) > 30)
                    DebugText.Text = DebugText.Text.Substring(DebugText.Text.LastIndexOf('\n'));
                DebugScroll.ChangeView(0, DebugScroll.ScrollableHeight, 1);
            });
        }

        private void SetFrontEndValues()
        {
            InsideTemp.Text = (data.Points[(int)GraphKeys.tempInt]
                .LastOrDefault()
                ?.Item2
                ?? Const.GetGraphs[(int)GraphKeys.tempInt].DefaultValue).ToString("F1");
            OutsideTemp.Text = (data.Points[(int)GraphKeys.tempExt]
                .LastOrDefault()
                ?.Item2
                ?? Const.GetGraphs[(int)GraphKeys.tempExt].DefaultValue).ToString("F1");
            InsideHumidity.Text = (data.Points[(int)GraphKeys.humidInt]
                .LastOrDefault()
                ?.Item2
                ?? Const.GetGraphs[(int)GraphKeys.humidInt].DefaultValue).ToString("F0");
            OutsideHumidity.Text = (data.Points[(int)GraphKeys.humidExt]
                .LastOrDefault()
                ?.Item2
                ?? Const.GetGraphs[(int)GraphKeys.humidExt].DefaultValue).ToString("F0");
            WindSpeed.Text = (data.Points[(int)GraphKeys.wind]
                .LastOrDefault()
                ?.Item2
                ?? Const.GetGraphs[(int)GraphKeys.wind].DefaultValue).ToString("F1");
        }
        private void HandleTriggers()
        {
            //Temperature: heater
            if (!double.IsNaN(data.TempThreshold))
            {
                var insideTemp = double.Parse(InsideTemp.Text);
                if (insideTemp < data.TempThreshold)
                {
                    if (!data.HeaterIsOn)
                    {
                        data.HeaterIsOn = true;
                        DebugText.Text += Environment.NewLine + Const.heatingOn;
                    }
                }
                else if (insideTemp + Const.temperatureHyst > data.TempThreshold)
                {
                    if (data.HeaterIsOn)
                    {
                        data.HeaterIsOn = false;
                        DebugText.Text += Environment.NewLine + Const.heatingOff;
                    }
                }
            }
            //Wind
            var wind = double.Parse(WindSpeed.Text);
            if (wind > Const.windThreshold)
            {
                if (!data.WindIsHigh)
                {
                    data.WindIsHigh = true;
                    DebugText.Text += Environment.NewLine + Const.highWind;
                }
            }
            else if (wind + Const.windSpeedHyst < Const.windThreshold)
            {
                if (data.WindIsHigh)
                    data.WindIsHigh = false;
            }
        }
        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            var x = TempUserInput.Value;
            var min = Const.GetGraphs[(int)GraphKeys.tempInt].LowerBound;
            var max = Const.GetGraphs[(int)GraphKeys.tempInt].UpperBound;
            data.TempThreshold = Math.Min(max, Math.Max(x, min));
            TempUserInput.PlaceholderText = $"{(int)data.TempThreshold}°C";
            TempUserInput.Text = "";
        }
    }
}
