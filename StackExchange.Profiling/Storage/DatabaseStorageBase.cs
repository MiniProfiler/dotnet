namespace StackExchange.Profiling.Storage
{
    using System;
    using System.Collections.Generic;

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

        /// <summary>
        /// Giving freshly selected collections, this method puts them in the correct
        /// hierarchy under the 'result' MiniProfiler.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="timingsWrapper">The timings to map</param>
        protected void MapTimings(MiniProfiler result, DbTimingsWrapper timingsWrapper)
        {
            result.ClientTimings = timingsWrapper.ClientTimings;
            result.CustomLinks = timingsWrapper.CustomLinks;
            result.Root = timingsWrapper.Root;
        }

        /// <summary>
        /// Stores the different timings related to a MiniProfiler that is to be inserted into a DbStorage
        /// </summary>
        /// <remarks>Is to be stored in </remarks>
        protected class DbTimingsWrapper
        {
            public Dictionary<string, string> CustomLinks { get; set; }
            public Timing Root { get; set; }
            public ClientTimings ClientTimings { get; set; }
        }
    }



}
