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
            ChartTemperature.Series.Add(new LineSeries() { 
                ItemsSource = new ChartCollection(), 
                Title = new Title() { Content = "Улица\n°C" },
                IndependentValueBinding = new Binding() { Path = new PropertyPath("Item1") },
                DependentValueBinding = new Binding() { Path = new PropertyPath("Item2") },
                IsSelectionEnabled = true
            });
            ChartTemperature.Series.Add(new LineSeries() { 
                ItemsSource = new ChartCollection(), 
                Title = new Title() { Content = "Помещение\n°C" },
                IndependentValueBinding = new Binding() { Path = new PropertyPath("Item1") },
                DependentValueBinding = new Binding() { Path = new PropertyPath("Item2") },
                IsSelectionEnabled = true
            });
            ChartHumidity.Series.Add(new LineSeries()
            {
                ItemsSource = new ChartCollection(),
                Title = new Title() { Content = "Влажность\n%" },
                IndependentValueBinding = new Binding() { Path = new PropertyPath("Item1") },
                DependentValueBinding = new Binding() { Path = new PropertyPath("Item2") },
                IsSelectionEnabled = true
            });
            ChartLux.Series.Add(new LineSeries() { 
                ItemsSource = new ChartCollection(), 
                Title = new Title() { Content = "Освещённость\nлюкс" },
                IndependentValueBinding = new Binding() { Path = new PropertyPath("Item1") },
                DependentValueBinding = new Binding() { Path = new PropertyPath("Item2") },
                IsSelectionEnabled = true
            });
            ChartPressure.Series.Add(new LineSeries() { 
                ItemsSource = new ChartCollection(), 
                Title = new Title() { Content = "Давление\nмм" },
                IndependentValueBinding = new Binding() { Path = new PropertyPath("Item1") },
                DependentValueBinding = new Binding() { Path = new PropertyPath("Item2") },
                IsSelectionEnabled = true
            });
            var tempOutside = (ChartTemperature.Series[0] as LineSeries).ItemsSource as ChartCollection;
            var tempInside = (ChartTemperature.Series[1] as LineSeries).ItemsSource as ChartCollection;
            var humidity = (ChartHumidity.Series[0] as LineSeries).ItemsSource as ChartCollection;
            var airPressure = (ChartPressure.Series[0] as LineSeries).ItemsSource as ChartCollection;
            var lighting = (ChartLux.Series[0] as LineSeries).ItemsSource as ChartCollection;
            for (int j = 0; j < Const.pointsCount; j++)
            {
                DateTime dateTime = DateTime.Now.AddSeconds(j - Const.pointsCount);
                tempOutside.Add(new ChartPoint(dateTime, 20));
                tempInside.Add(new ChartPoint(dateTime, 25));
                humidity.Add(new ChartPoint(dateTime, 50));
                airPressure.Add(new ChartPoint(dateTime, 760));
                lighting.Add(new ChartPoint(dateTime, 50));
            }
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
                    randomData.AddSensor(GraphKeys.tempExt.ToString(), -20, 50, 20);
                    randomData.AddSensor(GraphKeys.tempInt.ToString(), 10, 40, 25);
                    randomData.AddSensor(GraphKeys.humidity.ToString(), 0, 100, 65);
                    randomData.AddSensor(GraphKeys.pressure.ToString(), 730, 790, 760);
                    randomData.AddSensor(GraphKeys.lighting.ToString(), 0, 100, 30);

                    data = Data.Data.GetInstance();
                    data.Points = new List<ChartPoint>[5] {
                        ((ChartTemperature.Series[0] as LineSeries).ItemsSource as ChartCollection).ToList(),
                        ((ChartTemperature.Series[1] as LineSeries).ItemsSource as ChartCollection).ToList(),
                        ((ChartHumidity.Series[0] as LineSeries).ItemsSource as ChartCollection).ToList(),
                        ((ChartPressure.Series[0] as LineSeries).ItemsSource as ChartCollection).ToList(),
                        ((ChartLux.Series[0] as LineSeries).ItemsSource as ChartCollection).ToList()
                    };
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
                var pointsObserve = new ChartCollection[] {
                    (ChartTemperature.Series[0] as LineSeries).ItemsSource as ChartCollection,
                    (ChartTemperature.Series[1] as LineSeries).ItemsSource as ChartCollection,
                    (ChartHumidity.Series[0] as LineSeries).ItemsSource as ChartCollection,
                    (ChartPressure.Series[0] as LineSeries).ItemsSource as ChartCollection,
                    (ChartLux.Series[0] as LineSeries).ItemsSource as ChartCollection
                };
                pointsObserve.ForEach(x => x.Clear());
                for (int j = 0; j < pointsObserve.Length; j++)
                    data.Points[j].ForEach(x => pointsObserve[j].Add(x));
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
