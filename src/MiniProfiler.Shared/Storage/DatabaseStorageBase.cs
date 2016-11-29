using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to save MiniProfiler results to a MSSQL database, allowing more permanent storage and querying of slow results.
    /// </summary>
    public abstract class DatabaseStorageBase : IStorage
    {
        /// <summary>
        /// Gets or sets how we connect to the database used to save/load MiniProfiler results.
        /// </summary>
        protected string ConnectionString { get; set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="DatabaseStorageBase"/> class. 
        /// Returns a new <c>SqlServerDatabaseStorage</c> object that will insert into the database identified by connectionString.
        /// </summary>
        /// <param name="connectionString">The connection String.</param>
        protected DatabaseStorageBase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Returns a connection to the data store.
        /// </summary>
        protected abstract DbConnection GetConnection();

        /// <summary>
        /// Saves 'profiler' to a database under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        public abstract void Save(MiniProfiler profiler);

        /// <summary>
        /// Returns the MiniProfiler identified by 'id' from the database or null when no MiniProfiler exists under that 'id'.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>the mini profiler</returns>
        public abstract MiniProfiler Load(Guid id);

        /// <summary>
        /// Sets a particular profiler session so it is considered "un-viewed"  
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
        public abstract void SetUnviewed(string user, Guid id);

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
        public abstract void SetViewed(string user, Guid id);

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">
        /// User identified by the current <c>MiniProfiler.Settings.UserProvider</c>.
        /// </param>
        /// <returns>the list of keys for the supplied user</returns>
        public abstract List<Guid> GetUnviewedIds(string user);

        /// <summary>
        /// Implement a basic list search here
        /// </summary>
        /// <param name="maxResults">The max number of results.</param>
        /// <param name="start">The start.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="orderBy">order By.</param>
        /// <returns>the list of GUID keys</returns>
        public abstract IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending);

        protected void ConnectTimings(MiniProfiler profiler, List<Timing> timings, List<ClientTiming> clientTimings)
        {
            if (profiler != null && profiler.RootTimingId.HasValue && timings.Any())
            {
                var rootTiming = timings.SingleOrDefault(x => x.Id == profiler.RootTimingId.Value);
                if (rootTiming != null)
                {
                    profiler.Root = rootTiming;
                    timings.ForEach(x => x.Profiler = profiler);
                    timings.Remove(rootTiming);
                    var timingsLookupByParent = timings.ToLookup(x => x.ParentTimingId, x => x);
                    PopulateChildTimings(rootTiming, timingsLookupByParent);
                }
                if (clientTimings.Any() || profiler.ClientTimingsRedirectCount.HasValue)
                {
                    profiler.ClientTimings = new ClientTimings
                    {
                        RedirectCount = profiler.ClientTimingsRedirectCount ?? 0,
                        Timings = clientTimings
                    };
                }
            }
        }

        /// <summary>
        /// Build the subtree of <see cref="Timing"/> objects with <paramref name="parent"/> at the top.
        /// Used recursively.
        /// </summary>
        /// <param name="parent">Parent <see cref="Timing"/> to be evaluated.</param>
        /// <param name="timingsLookupByParent">Key: parent timing Id; Value: collection of all <see cref="Timing"/> objects under the given parent.</param>
        private void PopulateChildTimings(Timing parent, ILookup<Guid, Timing> timingsLookupByParent)
        {
            if (timingsLookupByParent.Contains(parent.Id))
            {
                foreach (var timing in timingsLookupByParent[parent.Id].OrderBy(x => x.StartMilliseconds))
                {
                    parent.AddChild(timing);
                    PopulateChildTimings(timing, timingsLookupByParent);
                }
            }
        }

        /// <summary>
        /// Flattems the timings down into a single list.
        /// </summary>
        protected void FlattenTimings(Timing timing, List<Timing> timingsCollection)
        {
            timingsCollection.Add(timing);
            if (timing.HasChildren)
            {
                timing.Children.ForEach(x => FlattenTimings(x, timingsCollection));
            }
        }
    }
}
