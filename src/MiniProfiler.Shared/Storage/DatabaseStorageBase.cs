using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to save MiniProfiler results to a MSSQL database, allowing more permanent storage and querying of slow results.
    /// </summary>
    public abstract class DatabaseStorageBase : IAsyncStorage, IDatabaseStorageConnectable
    {
        /// <summary>
        /// The table <see cref="MiniProfiler"/>s are stored in.
        /// </summary>
        public readonly string MiniProfilersTable = "MiniProfilers";

        /// <summary>
        /// The table <see cref="Timing"/>s are stored in.
        /// </summary>
        public readonly string MiniProfilerTimingsTable = "MiniProfilerTimings";

        /// <summary>
        /// The table <see cref="ClientTiming"/>s are stored in.
        /// </summary>
        public readonly string MiniProfilerClientTimingsTable = "MiniProfilerClientTimings";

        /// <summary>
        /// Gets or sets how we connect to the database used to save/load MiniProfiler results.
        /// </summary>
        protected string ConnectionString { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseStorageBase"/> class. 
        /// Returns a new <c>SqlServerDatabaseStorage</c> object that will insert into the database identified by connectionString.
        /// </summary>
        /// <param name="connectionString">The connection String</param>
        protected DatabaseStorageBase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseStorageBase"/> class. 
        /// Returns a new <c>SqlServerDatabaseStorage</c> object that will insert into the database identified by connectionString.
        /// </summary>
        /// <param name="connectionString">The connection String</param>
        /// <param name="profilersTable">The table name to use for MiniProfilers.</param>
        /// <param name="timingsTable">The table name to use for MiniProfiler Timings.</param>
        /// <param name="clientTimingsTable">The table name to use for MiniProfiler Client Timings.</param>
        protected DatabaseStorageBase(string connectionString, string profilersTable, string timingsTable, string clientTimingsTable)
        {
            ConnectionString = connectionString;
            MiniProfilersTable = profilersTable;
            MiniProfilerTimingsTable = timingsTable;
            MiniProfilerClientTimingsTable = clientTimingsTable;
        }

        /// <summary>
        /// Returns a connection to the data store.
        /// </summary>
        protected abstract DbConnection GetConnection();

        /// <summary>
        /// Saves 'profiler' to a database under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public abstract void Save(MiniProfiler profiler);

        /// <summary>
        /// Asynchronously saves 'profiler' to a database under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public abstract Task SaveAsync(MiniProfiler profiler);

        /// <summary>
        /// Returns the MiniProfiler identified by 'id' from the database or null when no MiniProfiler exists under that 'id'.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public abstract MiniProfiler Load(Guid id);

        /// <summary>
        /// Asynchronously returns the MiniProfiler identified by 'id' from the database or null when no MiniProfiler exists under that 'id'.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public abstract Task<MiniProfiler> LoadAsync(Guid id);

        /// <summary>
        /// Whether this storage provider should call SetUnviewed methods (separately) after saving.
        /// </summary>
        public virtual bool SetUnviewedAfterSave => false;

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public abstract void SetUnviewed(string user, Guid id);

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public abstract Task SetUnviewedAsync(string user, Guid id);

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public abstract void SetViewed(string user, Guid id);

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public abstract Task SetViewedAsync(string user, Guid id);

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfilerOptions.UserProvider</c>.</param>
        /// <returns>The list of keys for the supplied user</returns>
        public abstract List<Guid> GetUnviewedIds(string user);

        /// <summary>
        /// Asynchronously returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfilerOptions.UserProvider</c>.</param>
        /// <returns>The list of keys for the supplied user</returns>
        public abstract Task<List<Guid>> GetUnviewedIdsAsync(string user);

        /// <summary>
        /// Returns the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results.</param>
        /// <param name="start">Search window start.</param>
        /// <param name="finish">Search window end.</param>
        /// <param name="orderBy">Result order.</param>
        /// <returns>The list of GUID keys.</returns>
        public abstract IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending);

        /// <summary>
        /// Asynchronously returns the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results.</param>
        /// <param name="start">Search window start.</param>
        /// <param name="finish">Search window end.</param>
        /// <param name="orderBy">Result order.</param>
        /// <returns>The list of GUID keys.</returns>
        public abstract Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending);

        /// <summary>
        /// Connects timings from the database, shared here for use in multiple providers.
        /// </summary>
        /// <param name="profiler">The profiler to connect the timing tree to.</param>
        /// <param name="timings">The raw list of Timings to construct the tree from.</param>
        /// <param name="clientTimings">The client timings to connect to the profiler.</param>
        protected void ConnectTimings(MiniProfiler profiler, List<Timing> timings, List<ClientTiming> clientTimings)
        {
            if (profiler?.RootTimingId.HasValue == true && timings.Count > 0)
            {
                var rootTiming = timings.SingleOrDefault(x => x.Id == profiler.RootTimingId.Value);
                if (rootTiming != null)
                {
                    profiler.Root = rootTiming;
                    foreach (var timing in timings)
                    {
                        timing.Profiler = profiler;
                    }
                    timings.Remove(rootTiming);
                    var timingsLookupByParent = timings.ToLookup(x => x.ParentTimingId, x => x);
                    PopulateChildTimings(rootTiming, timingsLookupByParent);
                }
                if (clientTimings.Count > 0 || profiler.ClientTimingsRedirectCount.HasValue)
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
        /// Flattens the timings down into a single list.
        /// </summary>
        /// <param name="timing">The <see cref="Timing"/> to flatten into <paramref name="timingsCollection"/>.</param>
        /// <param name="timingsCollection">The collection to add all timings in the <paramref name="timing"/> tree to.</param>
        protected void FlattenTimings(Timing timing, List<Timing> timingsCollection)
        {
            timingsCollection.Add(timing);
            if (timing.HasChildren)
            {
                foreach (var child in timing.Children)
                {
                    FlattenTimings(child, timingsCollection);
                }
            }
        }

        private List<string> _tableCreationScripts;

        /// <summary>
        /// The table creation scripts for this database storage.
        /// Generated by the <see cref="GetTableCreationScripts"/> implemented by the provider.
        /// </summary>
        public List<string> TableCreationScripts => _tableCreationScripts ??= GetTableCreationScripts().ToList();

        /// <summary>
        /// Creates needed tables. Run this once on your database.
        /// </summary>
        /// <remarks>
        /// Works in SQL server and <c>sqlite</c> (with documented removals).
        /// </remarks>
        protected abstract IEnumerable<string> GetTableCreationScripts();

        DbConnection IDatabaseStorageConnectable.GetConnection() => GetConnection();
    }

    /// <summary>
    /// Interface for accessing the <see cref="DatabaseStorageBase"/>'s connection, for testing.
    /// </summary>
    public interface IDatabaseStorageConnectable
    {
        /// <summary>
        /// Gets the connection for a <see cref="IDatabaseStorageConnectable"/> for testing.
        /// </summary>
        /// <returns>The connection for this storage.</returns>
        DbConnection GetConnection();
    }
}
