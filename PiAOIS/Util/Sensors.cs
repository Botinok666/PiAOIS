using PiAOIS.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Search;

namespace PiAOIS.Util
{
    class Sensors
    {
        public Sensors()
        {
        }
        public async Task<bool> PollSensors()
        {
            IEnumerable<RemoteSensors> sensors;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(Const.remoteServer);
                    if (response is null)
                        return false;
                    using (var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync()))
                    {
                        if (doc.RootElement.GetArrayLength() < 1)
                            return false;
                        sensors = doc.RootElement
                            .EnumerateArray()
                            .Select(x => JsonSerializer.Deserialize<RemoteSensors>(x.GetRawText()))
                            .Where(x => Const.selects.Contains(
                                new SensorSelect() { Class = x.SensorClass, Name = x.SensorName }))
                            .ToList();
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            Data.Data.GetInstance().AddDataToDB(sensors);
            return true;
        }
    }
}
