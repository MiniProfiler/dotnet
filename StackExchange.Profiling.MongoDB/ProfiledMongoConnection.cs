using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Internal;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledMongoCollection : MongoCollection
    {
        public ProfiledMongoCollection(MongoDatabase database, string name, MongoCollectionSettings settings)
            : base(database, name, settings)
        {
        }

        public override long Count()
        {
            var sw = new Stopwatch();

            sw.Start();
            var count = base.Count();
            sw.Stop();

            var commandString = string.Format("{0}.count()", Name);

            Utils.AddMongoTiming(
                new MongoTiming(MiniProfiler.Current, commandString)
                {
                    Id = Guid.NewGuid(),
                    DurationMilliseconds = sw.ElapsedMilliseconds,
                    FirstFetchDurationMilliseconds = sw.ElapsedMilliseconds,
                    ExecuteType = ExecuteType.Command.ToString().ToLower()
                });

            return count;
        }
    }
}
