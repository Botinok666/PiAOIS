using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Animation;
using WinRTXamlToolkit.Tools;

namespace PiAOIS.Util
{
    using ChartPoint = Tuple<DateTime, double>;
    class Sensors
    {
        private readonly StorageFileQueryResult queryResult = null;
        private IEnumerable<string> filesList = null;
        public Sensors(StorageFileQueryResult queryResult)
        {
            this.queryResult = queryResult;
            filesList = new List<string>();
        }
        public async void PollSensors()
        {
            IEnumerable<StorageFile> filesNew;
            List<string> filesDiff;
            try 
            { 
                filesNew = (await queryResult.GetFilesAsync())
                    .OrderByDescending(w => w.DateCreated)
                    .Take(Const.pointsCount);
                filesDiff = filesNew
                    .Select(x => x.Name)
                    .Except(filesList)
                    .ToList();
            }
            catch (FileNotFoundException)
            { 
                return; 
            }
            int newPoints = filesDiff.Count();
            if (newPoints < 1)
                return;
            filesList = filesNew.Select(x => x.Name);
            var data = Data.Data.GetInstance();
            List<ChartPoint>[] pointsToAdd = new List<ChartPoint>[data.Points.Length];
            for (int j = 0; j < pointsToAdd.Length; j++)
                pointsToAdd[j] = new List<ChartPoint>();

            for (int j = 0; j < newPoints; j++)
            {
                string text;
                DateTime fileCreated;
                try
                {
                    StorageFile file = await queryResult.Folder.GetFileAsync(filesDiff[j]);
                    text = await FileIO.ReadTextAsync(file);
                    fileCreated = file.DateCreated.LocalDateTime;
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                if (!JsonObject.TryParse(text, out JsonObject jsonObj) ||
                    jsonObj.Count < pointsToAdd.Length)
                    continue;
                for (int k = 0; k < pointsToAdd.Length; k++)
                {
                    if (!Enum.IsDefined(typeof(GraphKeys), k))
                        continue;
                    string key = ((GraphKeys)k).ToString();
                    if (jsonObj[key]?.ValueType == JsonValueType.Number)
                        pointsToAdd[k].Add(new ChartPoint(fileCreated, jsonObj[key].GetNumber()));
                }
            }
            List<ChartPoint>[] points = data
                .Points
                .Select(x => x.Skip(pointsToAdd[0].Count).ToList())
                .ToArray();
            for (int j = 0; j < pointsToAdd.Length; j++)
                pointsToAdd[j].ForEach(x => points[j].Add(x));
            data.Points = points;
        }
    }
}
