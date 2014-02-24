using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using StackExchange.Profiling.MongoDB.Utils;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledMongoCollection<TDefaultDocument> : MongoCollection<TDefaultDocument>
    {
        public ProfiledMongoCollection(MongoDatabase database, string name, MongoCollectionSettings settings) : base(database, name, settings)
        {
        }

        #region Methods overrides

        public override MongoCursor<TDocument> FindAs<TDocument>(IMongoQuery query)
        {
            var serializer = BsonSerializer.LookupSerializer(typeof(TDocument));
            return new ProfiledMongoCursor<TDocument>(this, query, Settings.ReadPreference, serializer, null);
        }

        public override AggregateResult Aggregate(IEnumerable<BsonDocument> operations)
        {
            var operationsList = operations.ToList();

            var sw = new Stopwatch();

            sw.Start();
            var result = base.Aggregate(operationsList);
            sw.Stop();

            string commandString = string.Format("{0}.aggregate(pipeline)\n\npipeline = \n{1}", Name,
                string.Join("\n", operationsList.Select(operation => string.Format("   {0}", operation))));

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        public override long Count(IMongoQuery query)
        {
            var sw = new Stopwatch();

            sw.Start();
            var count = base.Count(query);
            sw.Stop();

            string commandString = query != null
                ? string.Format("{0}.count(query)\n\nquery = {1}", Name, query)
                : string.Format("{0}.count()", Name);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return count;
        }

        public override IEnumerable<BsonValue> Distinct(string key, IMongoQuery query)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Distinct(key, query);
            sw.Stop();

            string commandString = query != null
                ? string.Format("{0}.distinct(\"{1}\", query)\n\nquery = {2}", Name, key, query)
                : string.Format("{0}.distinct(\"{1}\")", Name, key);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        public override IEnumerable<TValue> Distinct<TValue>(string key, IMongoQuery query)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Distinct<TValue>(key, query);
            sw.Stop();

            string commandString = query != null
                ? string.Format("{0}.distinct(\"{1}\", query)\n\nquery = {2}", Name, key, query)
                : string.Format("{0}.distinct(\"{1}\")", Name, key);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        public override CommandResult Drop()
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Drop();
            sw.Stop();

            string commandString = string.Format("{0}.drop()", Name);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return result;
        }

        public override CommandResult DropIndexByName(string indexName)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.DropIndexByName(indexName);
            sw.Stop();

            string commandString = string.Format("{0}.dropIndex(\"{1}\")", Name, indexName);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return result;
        }

#pragma warning disable 672
        public override WriteConcernResult CreateIndex(IMongoIndexKeys keys, IMongoIndexOptions options)
#pragma warning restore 672
        {
            var sw = new Stopwatch();

            sw.Start();
#pragma warning disable 618
            var result = base.CreateIndex(keys, options);
#pragma warning restore 618
            sw.Stop();

            string commandString = options != null
                ? string.Format("{0}.ensureIndex(keys, options)\n\nkeys = {1}\n\noptions = {2}", Name, keys.ToBsonDocument(), options.ToBsonDocument())
                : string.Format("{0}.ensureIndex(keys, options)\n\nkeys = {1}", Name, keys.ToBsonDocument());

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return result;
        }

        #endregion
    }
}
