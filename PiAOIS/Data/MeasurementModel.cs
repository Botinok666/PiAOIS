using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiAOIS.Data
{
    [BsonIgnoreExtraElements]
    class MeasurementModel
    {
        public SensorModel[] Sensors { get; set; }
        public DateTime Time { get; set; }
    }
}
