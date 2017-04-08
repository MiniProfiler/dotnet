using StackExchange.Profiling.Storage;
using StackExchange.Redis;
using Xunit;

namespace Tests.Redis
{
    public class ConnectionTests
    {
        [Fact]
        public void ConnectionString()
        {
            var storage = new RedisStorage("localhost:6379");
            storage.GetUnviewedIds("");
        }

        [Fact]
        public void ConnectionOptions()
        {
            var storage = new RedisStorage(new ConfigurationOptions
            {
                EndPoints = {{"localhost", 6379}}
            });
            storage.GetUnviewedIds("");
        }

        [Fact]
        public void Multiplexer()
        {
            var multiplexer = ConnectionMultiplexer.Connect("localhost:6379");
            var storage = new RedisStorage(multiplexer);
            storage.GetUnviewedIds("");
        }

        [Fact]
        public void IDatabase()
        {
            var multiplexer = ConnectionMultiplexer.Connect("localhost:6379");
            var database = multiplexer.GetDatabase();
            var storage = new RedisStorage(database);
            storage.GetUnviewedIds("");
        }
    }
}
