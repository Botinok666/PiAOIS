using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiAOIS.Data
{
    using ChartPoint = Tuple<DateTime, double>;
    class Data
    {
        private static Data instance = null;
        private List<ChartPoint>[] _points = null;
        public event EventHandler DataAdded;
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
                    _points = new List<ChartPoint>[value.Length];
                lock (value.SyncRoot)
                {
                    for (int j = 0; j < value.Length; j++)
                        _points[j] = new List<ChartPoint>(value[j]);
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
        public bool IsRaining { get; set; }
        protected virtual void OnDataAdded(EventArgs e)
        {
            EventHandler handler = DataAdded;
            handler?.Invoke(this, e);
        }
    }
}
