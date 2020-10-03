using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiAOIS
{
    public enum GraphKeys
    { tempExt, tempInt, humidity, pressure, lighting }
    static class Const
    {
        public const int pointsCount = 16;
        public const int repeatRate = 2000; //In ms
        public const double changeRate = .06;
        public const double temperatureHyst = 1;
        public const double humidityHyst = 5;
        public const double lightingHyst = 5; 
        public static readonly string[] graphTitles = 
            { "Улица\n°C", "Помещение\n°C", "Влажность\n%", "Давление\nмм", "Освещённость\nлюкс" };
        public static readonly double[] defaultGraphValues = { 20, 25, 65, 760, 30 };
    }
}
