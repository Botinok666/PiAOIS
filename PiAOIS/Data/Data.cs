using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRTXamlToolkit.Tools;

namespace PiAOIS.Data
{
    using ChartPoint = Tuple<DateTime, double>;
    class Data
    {
        private static Data instance = null;
        private List<ChartPoint>[] _points = null;
        private bool _isRaining = false;
        public event EventHandler DataAdded;
        public event EventHandler RainChanged;
        private Data() {}
        public static Data GetInstance()
        {
            if (instance is null)
                instance = new Data();
            return instance;
        }
        public List<ChartPoint>[] Points
        { 
            get => _points;
            set
            {
                if (_points is null)
                {
                    _points = new List<ChartPoint>[value.Length];
                    for (int j = 0; j < value.Length; j++)
                        _points[j] = new List<ChartPoint>();
                }
                else
                    _points.ForEach(x => x.Clear());
                lock (value.SyncRoot)
                {
                    for (int j = 0; j < value.Length; j++)
                        value[j].ForEach(x => _points[j].Add(x));
                }
                OnDataAdded(new EventArgs());
            }
        }
        /// <summary>
        /// Threshold in Celsius degrees
        /// </summary>
        public double TempThreshold { get; set; }
        public bool HeaterIsOn { get; set; }
        public bool WindIsHigh { get; set; }
        public bool IsRaining 
        { 
            get => _isRaining; 
            set
            {
                if (_isRaining != value)
                    OnRainChanged(new EventArgs());
                _isRaining = value;
            }
        }
        protected virtual void OnDataAdded(EventArgs e)
        {
            EventHandler handler = DataAdded;
            handler?.Invoke(this, e);
        }
        protected virtual void OnRainChanged(EventArgs e)
        {
            RainChanged?.Invoke(this, e);
        }
    }
}
