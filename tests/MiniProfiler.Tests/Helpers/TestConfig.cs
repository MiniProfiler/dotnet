using Jil;
using System;

namespace StackExchange.Profiling.Tests
{
    public static class TestConfig
    {
        private const string FileName = "TestConfig.json";

        public static Config Current { get; }

        static TestConfig()
        {
            Current = new Config();
            try
            {
                var json = Resource.Get(FileName);
                if (!string.IsNullOrEmpty(json))
                {
                    Current = JSON.Deserialize<Config>(json);
                    Console.WriteLine("  {0} found, using for configuration.", FileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Deserializing TestConfig.json: " + ex);
            }
        }

        public class Config
        {
            public bool RunLongRunning { get; set; }
            public bool EnableTestLogging { get; set; } = Environment.GetEnvironmentVariable(nameof(EnableTestLogging)) == "true";

            public string RedisConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(RedisConnectionString)) ?? "localhost:6379";
            public string SQLServerConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(SQLServerConnectionString)) ?? "Server=.;Database=tempdb;Trusted_Connection=True;";
            public string SQLServerCeConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(SQLServerCeConnectionString)) ?? "Data Source=TestData.sdf;";
            public string MySQLConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(MySQLConnectionString)) ?? "server=localhost;uid=root;pwd=root;database=test;Allow User Variables=true;SslMode=none";
            public string PostgreSqlConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(PostgreSqlConnectionString)) ?? "Server=localhost;Port=5432;Database=test;User Id=postgres;Password=postgres;";
            public string MongoDbConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(MongoDbConnectionString)) ?? "mongodb://localhost:27017/test";
        }
    }
}
