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
        Devices devices = null;
        RandomData randomData = null;

        public MainPage()
        {
            InitializeComponent();
            InitCharts();
        }
        private void InitCharts()
        { 
            new List<Chart>()
            {
                ChartTemperature, ChartTemperature, ChartHumidity, ChartPressure, ChartLux
            }
            .Select((chart, idx) => new { chart, idx })
            .ForEach(pair => pair.chart.Series.Add(new LineSeries()
                {
                    ItemsSource = new ChartCollection(),
                    Title = new Title() { Content = Const.graphTitles[pair.idx] },
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
                        new ChartPoint(dateTime, Const.defaultGraphValues[pair.idx])));
            }
        }
        private ChartCollection[] GetChartSeries()
        {
            return new ChartCollection[] {
                (ChartTemperature.Series[0] as LineSeries).ItemsSource as ChartCollection,
                (ChartTemperature.Series[1] as LineSeries).ItemsSource as ChartCollection,
                (ChartHumidity.Series[0] as LineSeries).ItemsSource as ChartCollection,
                (ChartPressure.Series[0] as LineSeries).ItemsSource as ChartCollection,
                (ChartLux.Series[0] as LineSeries).ItemsSource as ChartCollection
            };
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (toggleSw.IsOn)
            {
                if (jsonFolder is null)
                {
                    await new Windows.UI.Popups.MessageDialog("Выберите папку для сохранения JSON файлов").ShowAsync();
                    FolderPicker folderPicker = new FolderPicker();
                    folderPicker.FileTypeFilter.Add("*");
                    jsonFolder = await folderPicker.PickSingleFolderAsync();
                    if (jsonFolder is null)
                    {
                        toggleSw.IsOn = false;
                        return;
                    }
                    randomData = new RandomData(jsonFolder);
                    randomData.AddSensor(GraphKeys.tempExt.ToString(), -20, 50, Const.defaultGraphValues[0]);
                    randomData.AddSensor(GraphKeys.tempInt.ToString(), 10, 40, Const.defaultGraphValues[1]);
                    randomData.AddSensor(GraphKeys.humidity.ToString(), 0, 100, Const.defaultGraphValues[2]);
                    randomData.AddSensor(GraphKeys.pressure.ToString(), 730, 790, Const.defaultGraphValues[3]);
                    randomData.AddSensor(GraphKeys.lighting.ToString(), 0, 100, Const.defaultGraphValues[4]);

                    data = Data.Data.GetInstance();
                    data.Points = GetChartSeries()
                        .Select(x => x.ToList())
                        .ToArray();
                    data.DataAdded += Data_DataAdded;

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
                    devices = new Devices();
                }
                randomData.Start();
            }
            else
            {
                randomData?.Stop();
            }
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
                //Handle devices
                devices.ManageDevices();
                SwKitchen.IsOn = devices.KitchenVentIsOn;
                SwShower.IsOn = devices.ShowerVentIsOn;
                SwLight.IsOn = devices.OutsideLightIsOn;
            });
        }

        private void KitchenUD_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Data.Data.GetInstance().KitchenThreshold = e.NewValue;
        }

        private void ShowerUD_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Data.Data.GetInstance().ShowerThreshold = e.NewValue;
        }

        private void LightingUD_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Data.Data.GetInstance().LightingThreshold = e.NewValue;
        }
    }
}
