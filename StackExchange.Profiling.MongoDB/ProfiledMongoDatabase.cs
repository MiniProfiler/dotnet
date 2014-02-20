using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledMongoDatabase : MongoDatabase
    {
        public ProfiledMongoDatabase(MongoServer server, string name, MongoDatabaseSettings settings) : base(server, name, settings)
        {
        }

        public override MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(string collectionName, MongoCollectionSettings collectionSettings)
        {
            return new ProfiledMongoCollection<TDefaultDocument>(this, collectionName, collectionSettings);
        }
    }
}
