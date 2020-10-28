using PiAOIS.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using Windows.Storage;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using System.Threading.Tasks;
using PiAOIS.Util;

namespace PiAOIS
{
    public static class Model
    {
        private static readonly string dbName = "sensors.sqlite";
        private static Dictionary<string, int> classIDs = new Dictionary<string, int>();
        private static Dictionary<string, int> unitsIDs = new Dictionary<string, int>();
        public static async void InitializeDatabase()
        {
            var dbfile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(dbName);
            if (dbfile is null)
                dbfile = await ApplicationData.Current.LocalFolder.CreateFileAsync(dbName);
            using (SqliteConnection db = new SqliteConnection($"Filename={dbfile.Path}"))
            {
                await db.OpenAsync();
                string tableCommand = 
                    //"DROP TABLE SensorData; DROP TABLE SensorClass; DROP TABLE SensorUnits; " +
                    "CREATE TABLE IF NOT EXISTS SensorData (" +
                        "ID INTEGER PRIMARY KEY, " +
                        "ClassID INT NOT NULL, " +
	                    "Name TEXT NOT NULL, " +
                        "Value TEXT NOT NULL, " +
	                    "UnitsID INT NOT NULL, " +
                        "UpdateTime INT NOT NULL);" +
                    "CREATE TABLE IF NOT EXISTS SensorClass (" +
                        "ID INTEGER PRIMARY KEY, " +
                        "Name TEXT UNIQUE);" +
                    "CREATE TABLE IF NOT EXISTS SensorUnits (" +
                        "ID INTEGER PRIMARY KEY, " +
                        "Name TEXT UNIQUE);";
                await new SqliteCommand(tableCommand, db).ExecuteNonQueryAsync();
            }
        }
        public static async Task InsertRows(IEnumerable<RemoteSensors> sensors)
        {                
            var classes = sensors
                .Select(x => x.SensorClass)
                .Distinct();
            var units = sensors
                .Select(x => x.SensorUnit)
                .Distinct();
            if (classIDs.Count == 0 || 0 == unitsIDs.Count)
            {
                classIDs = await FillDictionary("SensorClass", classes);
                unitsIDs = await FillDictionary("SensorUnits", units);
            }
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbName);
            using (SqliteConnection db = new SqliteConnection($"Filename={path}"))
            {
                await db.OpenAsync();
                var command = new SqliteCommand("INSERT INTO SensorData " +
                    "(ClassID, Name, Value, UnitsID, UpdateTime) VALUES " +
                    "(@ClassID, @Name, @Value, @UnitsID, @UpdateTime)", db);
                var parameters = new SqliteParameter[]
                {
                    new SqliteParameter("@ClassID", DbType.Int32),
                    new SqliteParameter("@Name", DbType.String),
                    new SqliteParameter("@Value", DbType.String),
                    new SqliteParameter("@UnitsID", DbType.Int32),
                    new SqliteParameter("@UpdateTime", DbType.Int32)
                };
                command.Parameters.AddRange(parameters);
                await Task.WhenAll(sensors
                    .ToList()
                    .Select(async x => 
                    {
                        parameters[0].Value = classIDs[x.SensorClass];
                        parameters[1].Value = x.SensorName;
                        parameters[2].Value = Crypto.Encrypt(x.SensorValue);
                        parameters[3].Value = unitsIDs[x.SensorUnit];
                        parameters[4].Value = x.SensorUpdateTime;
                        await command.ExecuteNonQueryAsync();
                    })
                );
            }
        }
        public static async Task<IEnumerable<RemoteSensors>> GetSensorData
            (SensorSelect key, int count, string pass)
        {
            if (classIDs.Count == 0 || 0 == unitsIDs.Count)
                return new List<RemoteSensors>();
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbName);
            using (SqliteConnection db = new SqliteConnection($"Filename={path}"))
            {
                await db.OpenAsync();
                var adapter = new SqliteCommand("SELECT sc.Name, su.Name, " +
                    "sd.Name, sd.Value, sd.UpdateTime FROM SensorData sd " +
                    "INNER JOIN SensorClass sc ON sc.ID = sd.ClassID " +
                    "INNER JOIN SensorUnits su ON su.ID = sd.UnitsID " +
                    $"WHERE sd.ClassID={classIDs[key.Class]} AND sd.Name='{key.Name}' " +
                    $"ORDER BY sd.UpdateTime DESC LIMIT {count}", db);
                using (var reader = await adapter.ExecuteReaderAsync())
                {
                    List<RemoteSensors> sensors = new List<RemoteSensors>();
                    bool validPass = true;
                    while (await reader.ReadAsync())
                    {
                        string encrypted = await reader.GetFieldValueAsync<string>(3);
                        validPass &= Crypto.TryDecryptFloat(pass, encrypted, out float value);
                        sensors.Add(new RemoteSensors()
                        {
                            SensorClass = await reader.GetFieldValueAsync<string>(0),
                            SensorUnit = await reader.GetFieldValueAsync<string>(1),
                            SensorName = await reader.GetFieldValueAsync<string>(2),
                            Value = value,
                            SensorUpdateTime = reader.GetInt32(4)
                        });
                    }
                    return validPass ? sensors : new List<RemoteSensors>();
                }
            }
        }
        private static async Task<Dictionary<string, int>> FillDictionary(string table, IEnumerable<string> names)
        {
            var dbNames = new List<KeyValuePair<string, int>>();
            bool readAgain = false;
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbName);
            using (SqliteConnection db = new SqliteConnection($"Filename={path}"))
            {
                await db.OpenAsync();
                var adapter = new SqliteCommand($"SELECT ID, Name FROM {table}", db);
                using (var reader = await adapter.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        dbNames.Add(new KeyValuePair<string, int>(reader.GetString(1), reader.GetInt32(0)));
                    var newNames = names
                        .Except(dbNames.Select(x => x.Key));
                    if (newNames.Count() > 0)
                    {
                        var liteCommand = new SqliteCommand($"INSERT INTO {table} "
                            + "(Name) VALUES (@Name)", db);
                        var liteParameter = new SqliteParameter("@Name", DbType.String);
                        liteCommand.Parameters.Add(liteParameter);
                        newNames
                            .ToList()
                            .ForEach(async x =>
                            {
                                liteParameter.Value = x;
                                await liteCommand.ExecuteNonQueryAsync();
                            });
                        readAgain = true;
                    }
                }
                if (readAgain)
                {
                    dbNames.Clear();
                    using (var reader = await adapter.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            dbNames.Add(new KeyValuePair<string, int>(reader.GetString(1), reader.GetInt32(0)));
                    }
                }
                return dbNames.ToDictionary(x => x.Key, x => x.Value);
            }
        }
    }
}
