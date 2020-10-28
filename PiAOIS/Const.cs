using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiAOIS
{
    static class Const
    {
        public const int pointsCount = 16;
        public const int repeatRate = 2000; //In ms
        public const string remoteServer = "http://localhost:8086";
        public const string remoteErr = "It looks like " + remoteServer + " isn't running";
        public const string remoteStart = "Connecting to " + remoteServer + "…";
        public const string remoteStop = "Polling " + remoteServer + " was stopped";
        public const string remoteOk = "Connected to " + remoteServer;
        public const string incorrectPass = "Incorrect DB password provided";
        public static readonly string[] units = new string[] { "W", "V", "°C" };
        public static readonly SensorSelect[] selects = new SensorSelect[]
        {
            new SensorSelect() { Class = "CPU [#0]: Intel Xeon E5-1660 v4: Enhanced", Name = "CPU Package Power" },
            new SensorSelect() { Class = "GPU [#0]: NVIDIA GeForce GTX 1070 Ti: ", Name = "GPU Power" },
            new SensorSelect() { Class = "CPU [#0]: Intel Xeon E5-1660 v4", Name = "Core 0 VID" },
            new SensorSelect() { Class = "GPU [#0]: NVIDIA GeForce GTX 1070 Ti: ", Name = "GPU Core Voltage" },
            new SensorSelect() { Class = "CPU [#0]: Intel Xeon E5-1660 v4: DTS", Name = "CPU Package" },
            new SensorSelect() { Class = "GPU [#0]: NVIDIA GeForce GTX 1070 Ti: ", Name = "GPU Temperature" }
        };
    }
    public struct SensorSelect
    {
        public string Class;
        public string Name;
    }
}
