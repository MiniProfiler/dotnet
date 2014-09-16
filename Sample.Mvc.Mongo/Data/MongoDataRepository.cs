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
        public MongoClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new MongoClient(MongoUrl);
                }

                return _client;
            }
        }

        private MongoServer _server;
        public MongoServer Server
        {
            get
            {
                if (_server == null)
                {
                    _server = ProfiledMongoServer.Create(Client);
                }
                return _server;
            }
        }

        private MongoDatabase _database;
        public MongoDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    _database = Server.GetDatabase(DbName);
                }
                return _database;
            }
        }

        private MongoCollection _fooCollection;
        public MongoCollection FooCollection
        {
            get
            {
                if (_fooCollection == null)
                {
                    _fooCollection = Database.GetCollection("foo");
                }
                return _fooCollection;
            }
        }

        private MongoCollection _barCollection;
        public MongoCollection BarCollection
        {
            get
            {
                if (_barCollection == null)
                {
                    _barCollection = Database.GetCollection("bar");
                }
                return _barCollection;
            }
        }

        private MongoCollection<BazzItem> _bazzCollection;
        public MongoCollection<BazzItem> BazzCollection
        {
            get
            {
                if (_bazzCollection == null)
                {
                    _bazzCollection = Database.GetCollection<BazzItem>("bazz");
                }
                return _bazzCollection;
            }
        }
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
