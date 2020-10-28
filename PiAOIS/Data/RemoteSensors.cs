using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PiAOIS.Data
{
    public class RemoteSensors
    {
        [JsonIgnore]
        public string SensorApp { get; set; }
        public string SensorClass { get; set; }
        public string SensorName { get; set; }
        public string SensorValue { get; set; }
        public string SensorUnit { get; set; }
        public long SensorUpdateTime { get; set; }
        [JsonIgnore]
        public float Value { get; set; }
    }
}
