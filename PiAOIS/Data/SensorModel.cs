using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiAOIS.Data
{
    [BsonIgnoreExtraElements]
    class SensorModel
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        [BsonIgnore]
        public double NumericValue { get; set; }
    }
}
