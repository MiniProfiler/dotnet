using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MongoDb database.
    /// </summary>
    public class MongoDbStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Returns a new <see cref="MongoDbStorage"/>.
        /// </summary>
        public MongoDbStorage(string connectionString)
            :base(connectionString)
        {

        }

        /// <summary>
        /// Stores <param name="profiler"/> to MongoDB under its <see cref="MiniProfiler.Id"/>; 
        /// stores all child Timings and SqlTimings to their respective tables.
        /// </summary>
        public override void Save(MiniProfiler profiler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads the MiniProfiler identifed by 'id' from the database.
        /// </summary>
        public override MiniProfiler Load(Guid id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// NOT IMPLEMENTED 
        /// </summary>
        public override void SetUnviewed(string user, Guid id)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public override void SetViewed(string user, Guid id)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public override List<Guid> GetUnviewedIds(string user)
        {
            return null;
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Decending)
        {
            return null;
        }

        protected override System.Data.Common.DbConnection GetConnection()
        {
            throw new NotImplementedException();
        }
    }
}
