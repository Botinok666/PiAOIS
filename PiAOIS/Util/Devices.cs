using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiAOIS.Util
{
    class Devices
    {
        public Devices()
        {
            KitchenVentIsOn = false;
            ShowerVentIsOn = false;
            OutsideLightIsOn = false;
        }
        public void ManageDevices()
        {
            var data = Data.Data.GetInstance();
            double kitchenT = data.Points[(int)GraphKeys.tempInt]
                .LastOrDefault()
                ?.Item2
                ?? 25;
            double outsideT = data.Points[(int)GraphKeys.tempExt]
                .LastOrDefault()
                ?.Item2
                ?? 25;
            double showerRH = data.Points[(int)GraphKeys.humidity]
                .LastOrDefault()
                ?.Item2
                ?? 50;
            double outsideLx = data.Points[(int)GraphKeys.lighting]
                .LastOrDefault()
                ?.Item2
                ?? 50;
            if (KitchenVentIsOn)
            {
                if (kitchenT < outsideT || (kitchenT + Const.temperatureHyst) < data.KitchenThreshold)
                    KitchenVentIsOn = false;
            }
            else if (kitchenT > outsideT && kitchenT > data.KitchenThreshold)
            {
                KitchenVentIsOn = true;
            }
            if (ShowerVentIsOn)
            {
                if ((showerRH + Const.humidityHyst) < data.ShowerThreshold)
                    ShowerVentIsOn = false;
            }
            else if (showerRH > data.ShowerThreshold)
            {
                ShowerVentIsOn = true;
            }
            if (OutsideLightIsOn)
            {
                if ((outsideLx - Const.lightingHyst) > data.LightingThreshold)
                    OutsideLightIsOn = false;
            }
            else if (outsideLx < data.LightingThreshold)
            {
                OutsideLightIsOn = true;
            }
        }
        public bool KitchenVentIsOn { get; private set; }
        public bool ShowerVentIsOn { get; private set; }
        public bool OutsideLightIsOn { get; private set; }
    }
}
