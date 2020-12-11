using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleDB.Data
{
    class MeasurementModel
    {
        public SensorModel[] Sensors { get; set; }
        public DateTime Time { get; set; }
    }
}
