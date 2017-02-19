﻿using System;
using System.Collections;
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

        public override IEnumerable<BsonDocument> Aggregate(AggregateArgs args)
        {
            var sw = new Stopwatch();

            sw.Start();
            var underlyingEnumerable = base.Aggregate(args);
            sw.Stop();

            var profiledEnumerable = new ProfiledEnumerable<BsonDocument>(underlyingEnumerable);

            sw.Start();
            var profiledEnumerator = (ProfiledEnumerator<BsonDocument>) profiledEnumerable.GetEnumerator();
            sw.Stop();

            profiledEnumerator.EnumerationEnded += (sender, eventArgs) =>
            {
                var operationsList = args.Pipeline.ToList();

                string commandString = string.Format("db.{0}.aggregate(pipeline)\n\npipeline = \n{1}", Name,
                    string.Join("\n", operationsList.Select(operation => string.Format("   {0}", operation))));

                ProfilerUtils.AddMongoTiming(commandString, (long) (eventArgs.Elapsed + sw.Elapsed).TotalMilliseconds,
                    ExecuteType.Read);
            };

            return profiledEnumerable;
        }

        [Obsolete("Use the overload with an AggregateArgs parameter.")]
        public override AggregateResult Aggregate(IEnumerable<BsonDocument> operations)
        {
            var operationsList = operations.ToList();

            var sw = new Stopwatch();

            sw.Start();
            var result = base.Aggregate(operationsList);
            sw.Stop();

            string commandString = string.Format("db.{0}.aggregate(pipeline)\n\npipeline = \n{1}", Name,
                string.Join("\n", operationsList.Select(operation => string.Format("   {0}", operation))));

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        public override long Count(IMongoQuery query)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Count(query);
            sw.Stop();

            string commandString = query != null
                ? string.Format("db.{0}.count(query)\n\nquery = {1}", Name, query)
                : string.Format("db.{0}.count()", Name);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        public override IEnumerable<BsonValue> Distinct(string key, IMongoQuery query)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Distinct(key, query);
            sw.Stop();

            string commandString = query != null
                ? string.Format("db.{0}.distinct(\"{1}\", query)\n\nquery = {2}", Name, key, query)
                : string.Format("db.{0}.distinct(\"{1}\")", Name, key);

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
                ? string.Format("db.{0}.distinct(\"{1}\", query)\n\nquery = {2}", Name, key, query)
                : string.Format("db.{0}.distinct(\"{1}\")", Name, key);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        public override CommandResult Drop()
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Drop();
            sw.Stop();

            string commandString = string.Format("db.{0}.drop()", Name);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return result;
        }

        public override CommandResult DropIndexByName(string indexName)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.DropIndexByName(indexName);
            sw.Stop();

            string commandString = string.Format("db.{0}.dropIndex(\"{1}\")", Name, indexName);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return result;
        }

        public override WriteConcernResult CreateIndex(IMongoIndexKeys keys, IMongoIndexOptions options)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.CreateIndex(keys, options);
            sw.Stop();

            string commandString = options != null
                ? string.Format("db.{0}.ensureIndex(keys, options)\n\nkeys = {1}\n\noptions = {2}", Name, keys.ToBsonDocument(), options.ToBsonDocument())
                : string.Format("db.{0}.ensureIndex(keys, options)\n\nkeys = {1}", Name, keys.ToBsonDocument());

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return result;
        }

        #region FindAndModify

        public override FindAndModifyResult FindAndModify(FindAndModifyArgs args)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.FindAndModify(args);
            sw.Stop();

            var commandStringBuilder = new StringBuilder(1024);
            commandStringBuilder.AppendFormat("db.{0}.findAndModify(query, sort, update, new, fields, upsert)", Name);

            if (args.Query != null)
                commandStringBuilder.AppendFormat("\nquery = {0}", args.Query.ToBsonDocument());
            else
                commandStringBuilder.Append("\nquery = null");

            if (args.SortBy != null)
                commandStringBuilder.AppendFormat("\nsort = {0}", args.SortBy.ToBsonDocument());
            else
                commandStringBuilder.Append("\nsort = null");

            if (args.Update != null)
                commandStringBuilder.AppendFormat("\nupdate = {0}", args.Update.ToBsonDocument());
            else
                commandStringBuilder.Append("\nupdate = null");

            commandStringBuilder.AppendFormat("\nnew = {0}", args.VersionReturned == FindAndModifyDocumentVersion.Modified ? "true" : "false");

            if (args.Fields != null)
                commandStringBuilder.AppendFormat("\nfields = {0}", args.Fields.ToBsonDocument());
            else
                commandStringBuilder.Append("\nfields = null");

            commandStringBuilder.AppendFormat("\nupsert = {0}", args.Upsert ? "true" : "false");

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Update);

            return result;
        }

        #endregion

        #region FindAndRemove

        public override FindAndModifyResult FindAndRemove(FindAndRemoveArgs args)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.FindAndRemove(args);
            sw.Stop();

            var commandStringBuilder = new StringBuilder(1024);
            commandStringBuilder.AppendFormat("db.{0}.findAndModify(query, sort, remove, fields)", Name);

            if (args.Query != null)
                commandStringBuilder.AppendFormat("\nquery = {0}", args.Query.ToBsonDocument());
            else
                commandStringBuilder.Append("\nquery = null");

            if (args.SortBy != null)
                commandStringBuilder.AppendFormat("\nsort = {0}", args.SortBy.ToBsonDocument());
            else
                commandStringBuilder.Append("\nsort = null");

            commandStringBuilder.AppendFormat("\nremove = true");

            if (args.Fields != null)
                commandStringBuilder.AppendFormat("\nfields = {0}", args.Fields.ToBsonDocument());
            else
                commandStringBuilder.Append("\nfields = null");

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Update);

            return result;
        }

        #endregion

        #region Group

        public override IEnumerable<BsonDocument> Group(IMongoQuery query, IMongoGroupBy keys, BsonDocument initial, BsonJavaScript reduce, BsonJavaScript finalize)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Group(query, keys, initial, reduce, finalize);
            sw.Stop();

            var commandStringBuilder = new StringBuilder(1024);

            commandStringBuilder.AppendFormat("db.{0}.group({{key, reduce, initial", Name);

            if (query != null)
                commandStringBuilder.Append(", cond");

            if (initial != null)
                commandStringBuilder.Append(", initial");

            if (finalize != null)
                commandStringBuilder.Append(", finalize");

            commandStringBuilder.Append("})");

            commandStringBuilder.AppendFormat("\nkey = {0}", keys.ToBsonDocument());

            commandStringBuilder.Append("\nreduce = javascript");

            commandStringBuilder.AppendFormat("\ninitial = {0}", initial.ToBsonDocument());

            if (query != null)
                commandStringBuilder.AppendFormat("\ncond = {0}", query.ToBsonDocument());

            if (initial != null)
                commandStringBuilder.AppendFormat("\ninitial = {0}", initial.ToBsonDocument());

            if (finalize != null)
                commandStringBuilder.Append("\nfinalize = javascript");

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        public override IEnumerable<BsonDocument> Group(IMongoQuery query, BsonJavaScript keyFunction, BsonDocument initial, BsonJavaScript reduce, BsonJavaScript finalize)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Group(query, keyFunction, initial, reduce, finalize);
            sw.Stop();

            var commandStringBuilder = new StringBuilder(1024);

            commandStringBuilder.AppendFormat("db.{0}.group({{keyf, reduce, initial", Name);

            if (query != null)
                commandStringBuilder.Append(", cond");

            if (initial != null)
                commandStringBuilder.Append(", initial");

            if (finalize != null)
                commandStringBuilder.Append(", finalize");

            commandStringBuilder.Append("})");

            commandStringBuilder.AppendFormat("\nkeyf = javascript");

            commandStringBuilder.Append("\nreduce = javascript");

            commandStringBuilder.AppendFormat("\ninitial = {0}", initial.ToBsonDocument());

            if (query != null)
                commandStringBuilder.AppendFormat("\ncond = {0}", query.ToBsonDocument());

            if (initial != null)
                commandStringBuilder.AppendFormat("\ninitial = {0}", initial.ToBsonDocument());

            if (finalize != null)
                commandStringBuilder.Append("\nfinalize = javascript");

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        #endregion

        public override IEnumerable<WriteConcernResult> InsertBatch(Type nominalType, IEnumerable documents, MongoInsertOptions options)
        {
            var documentsList = documents.Cast<object>().ToList();

            var sw = new Stopwatch();

            sw.Start();
            var result = base.InsertBatch(nominalType, documentsList, options);
            sw.Stop();

            var commandStringBuilder = new StringBuilder(512);

            commandStringBuilder.AppendFormat("db.{0}.insert(", Name);

            if (documentsList.Count > 1)
                commandStringBuilder.AppendFormat("<{0} documents>", documentsList.Count);
            else
            {
                // handle ensureIndex specially
                if (Name == "system.indexes")
                    commandStringBuilder.AppendFormat("{0}", documentsList.First().ToBsonDocument());
                else
                    commandStringBuilder.Append("<document>");
            }

            commandStringBuilder.Append(")");

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Create);

            return result;
        }

        public override MapReduceResult MapReduce(MapReduceArgs args)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.MapReduce(args);
            sw.Stop();

            string commandString = string.Format("db.{0}.mapReduce(<map function>, <reduce function>, options)", Name);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Read);

            return result;
        }

        public override CommandResult ReIndex()
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.ReIndex();
            sw.Stop();

            string commandString = string.Format("db.{0}.reIndex()", Name);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Create);

            return result;
        }

        public override WriteConcernResult Remove(IMongoQuery query, RemoveFlags flags, WriteConcern writeConcern)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Remove(query, flags, writeConcern);
            sw.Stop();

            var commandStringBuilder = new StringBuilder(1024);

            commandStringBuilder.AppendFormat("db.{0}.remove", Name);

            if (query == null)
            {
                if ((flags & RemoveFlags.None) == RemoveFlags.None)
                    commandStringBuilder.Append("()");
                else if ((flags & RemoveFlags.Single) == RemoveFlags.Single)
                    commandStringBuilder.Append("({}, true)");
            }
            else
            {
                commandStringBuilder.Append("(");
                commandStringBuilder.AppendFormat("query");

                if ((flags & RemoveFlags.Single) == RemoveFlags.Single)
                    commandStringBuilder.Append(", true");

                commandStringBuilder.Append(")");

                commandStringBuilder.AppendFormat("\nquery = {0}", query.ToBsonDocument());
            }

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Create);

            return result;
        }

        public override WriteConcernResult Update(IMongoQuery query, IMongoUpdate update, MongoUpdateOptions options)
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Update(query, update, options);
            sw.Stop();

            var commandStringBuilder = new StringBuilder(1024);

            commandStringBuilder.AppendFormat("db.{0}.update(query, update", Name);

            var optionsList = new List<string>();

            if ((options.Flags & UpdateFlags.Upsert) == UpdateFlags.Upsert)
                optionsList.Add("upsert: true");

            if ((options.Flags & UpdateFlags.Multi) == UpdateFlags.Multi)
                optionsList.Add("multi: true");

            if (optionsList.Any())
                commandStringBuilder.AppendFormat("{{ {0} }}", string.Join(", ", optionsList));

            commandStringBuilder.Append(")");

            if (query != null)
                commandStringBuilder.AppendFormat("\nquery = {0}", query.ToBsonDocument());
            else
                commandStringBuilder.Append("\nquery = {}");

            if (update != null)
                commandStringBuilder.AppendFormat("\nupdate = {0}", update.ToBsonDocument());
            else
                commandStringBuilder.Append("\nupdate = {}");

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Update);

            return result;
        }
    }
}
