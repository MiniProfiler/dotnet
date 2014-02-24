using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        #region FindAndModify

        public override FindAndModifyResult FindAndModify(IMongoQuery query, IMongoSortBy sortBy, IMongoUpdate update, IMongoFields fields,
            bool returnNew, bool upsert)
        {
            return FindAndModifyImpl(query, sortBy, false, update, returnNew, fields, upsert);
        }

        public override FindAndModifyResult FindAndRemove(IMongoQuery query, IMongoSortBy sortBy)
        {
            return FindAndModifyImpl(query, sortBy, true, null, false, null, false);
        }

        private FindAndModifyResult FindAndModifyImpl(IMongoQuery query, IMongoSortBy sortBy, bool remove, IMongoUpdate update, bool returnNew, IMongoFields fields, bool upsert)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.FindAndModify(query, sortBy, update, fields, returnNew, upsert);
            sw.Stop();

            var commandStringBuilder = new StringBuilder(1024);
            commandStringBuilder.AppendFormat("{0}.findAndModify(query, sort, remove, update, new, fields, upsert)", Name);

            if (query != null)
                commandStringBuilder.AppendFormat("\nquery = {0}", query.ToBsonDocument());
            else
                commandStringBuilder.Append("\nquery = null");

            if (sortBy != null)
                commandStringBuilder.AppendFormat("\nsort = {0}", sortBy.ToBsonDocument());
            else
                commandStringBuilder.Append("\nsort = null");

            commandStringBuilder.AppendFormat("\nremove = {0}", remove ? "true" : "false");

            if (update != null)
                commandStringBuilder.AppendFormat("\nupdate = {0}", update.ToBsonDocument());
            else
                commandStringBuilder.Append("\nupdate = null");

            commandStringBuilder.AppendFormat("\nnew = {0}", returnNew ? "true" : "false");

            if (fields != null)
                commandStringBuilder.AppendFormat("\nfields = {0}", fields.ToBsonDocument());
            else
                commandStringBuilder.Append("\nfields = null");

            commandStringBuilder.AppendFormat("\nupsert = {0}", upsert ? "true" : "false");

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Update);

            return result;
        }

        #endregion
    }
}
