using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to save MiniProfiler results to a MSSQL database, allowing more permanent storage and
    /// querying of slow results.
    /// </summary>
    public abstract class DatabaseStorageBase : IStorage
    {
        /// <summary>
        /// How we connect to the database used to save/load MiniProfiler results.
        /// </summary>
        protected string ConnectionString { get; set; }

        /// <summary>
        /// Returns a new SqlServerDatabaseStorage object that will insert into the database identified by connectionString.
        /// </summary>
        public DatabaseStorageBase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Saves 'profiler' to a database under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        public abstract void Save(MiniProfiler profiler);

        /// <summary>
        /// Returns the MiniProfiler identified by 'id' from the database or null when no MiniProfiler exists under that 'id'.
        /// </summary>
        public abstract MiniProfiler Load(Guid id);

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        public abstract void SetUnviewed(string user, Guid id);

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        public abstract void SetViewed(string user, Guid id);

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.Settings.UserProvider"/>.</param>
        public abstract List<Guid> GetUnviewedIds(string user);

        /// <summary>
        /// Implement a basic list search here
        /// </summary>
        /// <param name="maxResults"></param>
        /// <param name="start"></param>
        /// <param name="finish"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public abstract IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Decending);

        /// <summary>
        /// Giving freshly selected collections, this method puts them in the correct
        /// hierarchy under the 'result' MiniProfiler.
        /// </summary>
        protected void MapTimings(MiniProfiler result, List<Timing> timings, List<SqlTiming> sqlTimings, List<SqlTimingParameter> sqlParameters, ClientTimings clientTimings)
        {
            var stack = new Stack<Timing>();

            for (int i = 0; i < timings.Count; i++)
            {
                var cur = timings[i];
                foreach (var sqlTiming in sqlTimings)
                {
                    if (sqlTiming.ParentTimingId == cur.Id)
                    {
                        cur.AddSqlTiming(sqlTiming);

                        var parameters = sqlParameters.Where(p => p.ParentSqlTimingId == sqlTiming.Id);
                        if (parameters.Count() > 0)
                        {
                            sqlTiming.Parameters = parameters.ToList();
                        }
                    }
                }

                if (stack.Count > 0)
                {
                    Timing head;
                    while ((head = stack.Peek()).Id != cur.ParentTimingId)
                    {
                        stack.Pop();
                    }

                    head.AddChild(cur);
                }
                stack.Push(cur);
            }

            result.ClientTimings = clientTimings;

            // TODO: .Root does all the above work again, but it's used after [DataContract] deserialization; refactor it out somehow
            result.Root = timings.First();
        }

    }
}
