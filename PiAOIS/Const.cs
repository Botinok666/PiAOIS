using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiAOIS
{
    public enum GraphKeys
    { tempExt, tempInt, humidExt, humidInt, wind, rain }
    static class Const
    {
        public const int pointsCount = 16;
        public const int repeatRate = 2000; //In ms
        public const double changeRate = .04;
        public const double temperatureHyst = 1;
        public const double windThreshold = 10;
        public const double windSpeedHyst = 1.5;
        public const double rainThreshold = .9;
        public const double rainHyst = .05;
        public const string systemOn = "– System has started";
        public const string systemOff = "– System has stopped";
        public const string heatingOn = "– Heating is ON";
        public const string heatingOff = "– Heating is OFF";
        public const string rainyDay = "– It's rainy today! You should take an umbrella";
        public const string highWind = "– High wind, be careful!";
        public struct Graphs
        {
            public string Title;
            public double DefaultValue;
            public double LowerBound;
            public double UpperBound;
        }
        public static readonly Graphs[] GetGraphs = new Graphs[]
        {
            new Graphs() { Title = "Улица\n°C", DefaultValue = 26.5, LowerBound = -40, UpperBound = 80 },
            new Graphs() { Title = "Помещение\n°C", DefaultValue = 26.5, LowerBound = -5, UpperBound = 45 },
            new Graphs() { Title = "Улица\n%", DefaultValue = 60, LowerBound = 0, UpperBound = 100 },
            new Graphs() { Title = "Помещение\n%", DefaultValue = 60, LowerBound = 15, UpperBound = 85 },
            new Graphs() { Title = "Ветер\nкм/ч", DefaultValue = 5, LowerBound = .3, UpperBound = 30 },
            new Graphs() { Title = "Дождь", DefaultValue = .5, LowerBound = 0, UpperBound = 1 }
        };
    }

}
