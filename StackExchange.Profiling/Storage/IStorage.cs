using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling.Storage
{
    public enum ListResultsOrder
    { 
        Ascending,
        Decending
    }

    /*
     * Maybe ... to cut down on deserializtion 
    public class ProfileSummary
    {

        DateTime Started { get; set; }
        int TotalDurationMilliseconds { get; set; }
        int SqlDurationMilliseconds { get; set; }
    }
    */
    /// <summary>
    /// Provides saving and loading <see cref="MiniProfiler"/>s to a storage medium.
    /// </summary>
    public interface IStorage
    {

        /// <summary>
        /// Returns a list of profile guids (optionally in a particular date range)
        /// </summary>
        /// <param name="maxResults"></param>
        /// <param name="start"></param>
        /// <param name="finish"></param>
        /// <param name="orderBy">defaults to decending</param>
        /// <returns></returns>
        IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Decending);

        /// <summary>
        /// Stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The results of a profiling session.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being unviewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        void Save(MiniProfiler profiler);

        /// <summary>
        /// Returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>, which should map to <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        MiniProfiler Load(Guid id);

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        void SetUnviewed(string user, Guid id);

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        void SetViewed(string user, Guid id);

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.Settings.UserProvider"/>.</param>
        List<Guid> GetUnviewedIds(string user);

    }
}
