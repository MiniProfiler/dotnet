using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using StackExchange.Profiling.MongoDB;

namespace SampleWeb.Data
{
    public class MongoDataRepository
    {
        public string MongoUrl { get; private set; }
        public string DbName { get; set; }

        public MongoDataRepository(string mongoUrl, string dbName)
        {
            MongoUrl = mongoUrl;
            DbName = dbName;
        }

        private MongoClient _client;
        public MongoClient Client => _client ?? (_client = new MongoClient(MongoUrl));

        private MongoServer _server;
        public MongoServer Server => _server ?? (_server = ProfiledMongoServer.Create(Client));

        private MongoDatabase _database;
        public MongoDatabase Database => _database ?? (_database = Server.GetDatabase(DbName));

        private MongoCollection _fooCollection;
        public MongoCollection FooCollection => _fooCollection ?? (_fooCollection = Database.GetCollection("foo"));

        private MongoCollection _barCollection;
        public MongoCollection BarCollection => _barCollection ?? (_barCollection = Database.GetCollection("bar"));

        private MongoCollection<BazzItem> _bazzCollection;
        public MongoCollection<BazzItem> BazzCollection => _bazzCollection ?? (_bazzCollection = Database.GetCollection<BazzItem>("bazz"));
    }

    public class BazzItem
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public int SomeRandomInt { get; set; }

        public double SomeRandomDouble { get; set; }

        public DateTime CurrentTimestamp { get; set; }
    }
}
