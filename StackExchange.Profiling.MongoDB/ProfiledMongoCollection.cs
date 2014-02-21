using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson;
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

        public override AggregateResult Aggregate(IEnumerable<BsonDocument> operations)
        {
            var operationsList = operations.ToList();

            var sw = new Stopwatch();

            sw.Start();
            var result = base.Aggregate(operationsList);
            sw.Stop();

            string commandString = string.Format("{0}.aggregate(pipeline)\npipeline = \n{1}", Name,
                string.Join("\n", operationsList.Select(operation => string.Format("   {0}", operation))));

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return result;
        }

        public override long Count(IMongoQuery query)
        {
            var sw = new Stopwatch();

            sw.Start();
            var count = base.Count(query);
            sw.Stop();

            string commandString = query == null ? string.Format("{0}.count()", Name) : string.Format("{0}.count(query)\n\nquery = {1}", Name, query);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return count;
        }

        #endregion
    }
}
