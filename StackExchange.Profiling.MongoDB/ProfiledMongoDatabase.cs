using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Profiling.MongoDB.Utils;

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

        public override void Drop()
        {
            var sw = new Stopwatch();

            sw.Start();
            base.Drop();
            sw.Stop();

            string commandString = string.Format("{0}.drop()", Name);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);
        }

        
    }
}
