using System.Collections.Generic;
using System.Diagnostics;
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

        public override long Count(IMongoQuery query)
        {
            var sw = new Stopwatch();

            sw.Start();
            var count = base.Count(query);
            sw.Stop();

            string commandString = query == null ? string.Format("{0}.count()", Name) : string.Format("{0}.count(query)\n\nquery = {1}", Name, query);

            ProfilerUtils.AddMongoTiming(
                new MongoTiming(MiniProfiler.Current, commandString)
                {
                    DurationMilliseconds = sw.ElapsedMilliseconds,
                    FirstFetchDurationMilliseconds = sw.ElapsedMilliseconds,
                    ExecuteType = ExecuteType.Command.ToString().ToLower()
                });

            return count;
        }
    }
}
