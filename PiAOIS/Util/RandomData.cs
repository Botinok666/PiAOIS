using MersenneTwister;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using MathNet.Numerics.Distributions;

namespace PiAOIS.Util
{
    class RandomData
    {
        private class SensorProperties
        {
            public string Name { get; set; }
            public double LowerLimit { get; set; }
            public double Value { get; set; }
            public double UpperLimit { get; set; }
        }
        private readonly List<SensorProperties> sensors;
        private readonly StorageFolder jsonFolder;
        CancellationTokenSource cts = null;
        private readonly object lockObj;
        private bool isRunning = false;
        public RandomData(StorageFolder jsonFolder)
        {
            sensors = new List<SensorProperties>();
            lockObj = new object();
            this.jsonFolder = jsonFolder;
        }
        public void AddSensor(string Name, double LowerLimit, double UpperLimit, double Init)
        {
            lock (lockObj)
            {
                sensors.Add(new SensorProperties()
                {
                    Name = Name,
                    LowerLimit = LowerLimit,
                    UpperLimit = UpperLimit,
                    Value = Init
                });
            }
        }
        public int SenorsCount { get => sensors.Count; }
        public void Start()
        {
            if (sensors.Count < 1 || isRunning)
                return;
            isRunning = true;
            cts = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(new WaitCallback(Worker), cts.Token);
        }
        public void Stop()
        {
            cts?.Cancel();
        }
        private async void Worker(object obj)
        {
            Random random = MTRandom.Create();
            var distributions = new List<IContinuousDistribution>() 
            {   
                Normal.WithMeanVariance(0, 1.5, random), 
                new Cauchy(0.0, 2.0, random),
                new Exponential(1.5, random),
                new Gamma(5.0, 1.0, random), 
                new Laplace(0, 2.0, random),
                new Cauchy(0.0, 1.5, random)
            };
            var means = new double[] { 0, 0, 1 / 1.5, 5, 0, 0 };
            double avgDeviation = 1; //Random distributions may return rather large values, so we clip them
            CancellationToken cancellationToken = (CancellationToken)obj;
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (lockObj)
                {
                    for (int j = 0; j < sensors.Count; j++)
                    {
                        var x = sensors[j];
                        var sample = distributions[j].Sample() - means[j];
                        sample = Math.Min(avgDeviation, Math.Max(-avgDeviation, sample)); //Clipping of sample happens here
                        x.Value += (x.UpperLimit - x.LowerLimit) * Const.changeRate * sample;
                        x.Value = Math.Min(x.UpperLimit, Math.Max(x.LowerLimit, x.Value)); //And here we clip the final value
                    }
                }
                JsonObject jsonValues = new JsonObject();
                sensors.ForEach(x => jsonValues[x.Name] = JsonValue.CreateNumberValue(Math.Round(x.Value, 2)));
                StorageFile storageFile = await jsonFolder.CreateFileAsync(
                    string.Format("rnd{0}-{1}-{2}.json", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second), 
                    CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(storageFile, jsonValues.Stringify());
                try
                {
                    await Task.Delay(Const.repeatRate, cancellationToken).ConfigureAwait(true);
                }
                catch (TaskCanceledException)
                { 
                    break; 
                }
            }
            lock (lockObj)
            { 
                isRunning = false; 
            }
        }
    }
}
