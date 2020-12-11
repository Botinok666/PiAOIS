using ConsoleDB.Data;
using ConsoleDB.Util;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace ConsoleDB
{
    class Program
    {
        private static HttpClient client;
        private static IMongoDatabase mongoDatabase;
        private const string remoteServer = "http://localhost:8086";
        private const string dbServer = "mongodb://localhost:27017";
        private static readonly string[] selects = new string[]
        {
            "CPU Package Power",
            "GPU Power",
            "Core Max",
            "GPU Temperature"
        };
        public static void Main(string[] _)
        {
            Console.WriteLine("Hello World! Press Esc to exit");
            client = new HttpClient();
            Crypto.Init();
            //Mongo driver setup
            BsonClassMap.RegisterClassMap<SensorModel>();
            BsonClassMap.RegisterClassMap<MeasurementModel>();
            var mongoClient = new MongoClient(dbServer);
            mongoDatabase = mongoClient.GetDatabase("machines");
            //Start timer
            var t = new Timer(Callback, null, 0, 2000);
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                    return;
            }
        }
        private async static void Callback(object o)
        {
            //Fetch data from HWInfo
            var response = await client?.GetAsync(remoteServer);
            if (response is null || !response.IsSuccessStatusCode)
                return;
            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (doc.RootElement.GetArrayLength() < 1)
                return;
            //Deserialize and filter measurements
            var sensors = doc
                .RootElement
                .EnumerateArray()
                .Select(x => JsonSerializer.Deserialize<Sensors>(x.GetRawText()))
                .Where(x => selects.Contains(x.SensorName))
                .ToList();
            //Trim excessive data from sensor classes
            sensors
                .ForEach(x => x.SensorClass = 
                    x.SensorClass[(x.SensorClass.IndexOf(':') + 2)..x.SensorClass.LastIndexOf(':')]);
            //Create DB object
            var measurement = new MeasurementModel()
            {
                Time = DateTime.Now,
                Sensors = sensors
                    .Select(x => new SensorModel()
                    {
                        Group = x.SensorClass,
                        Name = x.SensorName,
                        Value = Crypto.Encrypt(x.SensorValue)
                    })
                    .ToArray()
            };
            await mongoDatabase
                .GetCollection<MeasurementModel>("measurements")
                .InsertOneAsync(measurement);
        }
    }
}
