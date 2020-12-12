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
        //Названия полей, по которым будут отбираться метрики
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
            //Инициализация драйвера Mongo
            BsonClassMap.RegisterClassMap<SensorModel>();
            BsonClassMap.RegisterClassMap<MeasurementModel>();
            var mongoClient = new MongoClient(dbServer);
            mongoDatabase = mongoClient.GetDatabase("machines");
            //Запустим таймер, который будет вызывать метод callback каждые 2 сек.
            var t = new Timer(Callback, null, 0, 2000);
            //Ждём, пока пользователь не нажмёт на Esc
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                    return;
            }
        }
        private async static void Callback(object o)
        {
            //Получим данные от расширения HWiNFO - оно выдаёт json по запросу главной страницы
            var response = await client?.GetAsync(remoteServer);
            if (response is null || !response.IsSuccessStatusCode)
                return;
            //Сначала преобразуем текст в объект
            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (doc.RootElement.GetArrayLength() < 1)
                return;
            //Полученный json содержит массив как корневой элемент. Представим его в виде массива,
            //затем преобразуем элементы в тип Sensors, и выберем из них те, в которых поле
            //SensorName совпадает с одним из заданных в массиве selects
            var sensors = doc
                .RootElement
                .EnumerateArray()
                .Select(x => JsonSerializer.Deserialize<Sensors>(x.GetRawText()))
                .Where(x => selects.Contains(x.SensorName))
                .ToList();
            //Вырежем полезную часть названия метрики - HWiNFO выдаёт названия, в которых три поля,
            //разделённых двоеточиями, нам нужно второе поле
            sensors
                .ForEach(x => x.SensorClass = 
                    x.SensorClass[(x.SensorClass.IndexOf(':') + 2)..x.SensorClass.LastIndexOf(':')]);
            //Создадим объект, представляющий запись в БД
            //Все метрики входят в него как массив, поле со значением шифруется
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
            //Запишем объект в БД
            await mongoDatabase
                .GetCollection<MeasurementModel>("measurements")
                .InsertOneAsync(measurement);
        }
    }
}
