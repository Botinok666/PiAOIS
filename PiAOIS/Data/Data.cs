using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiAOIS.Data
{
    class Data
    {
        private static Data instance = null;
        private Data() {}
        public static Data GetInstance()
        {
            if (instance is null)
                instance = new Data();
            return instance;
        }
        public async Task<IEnumerable<RemoteSensors>[]> GetPoints()
        {
            return await Task.WhenAll(Const.selects
                .Select(async x => await Model.GetSensorData(x, Const.pointsCount))
                .ToList());
        }
        public void AddDataToDB(IEnumerable<RemoteSensors> sensors)
        {
            Model.InsertRows(sensors);
        }
    }
}
