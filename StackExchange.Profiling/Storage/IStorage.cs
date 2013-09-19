using System;
using System.Collections.Generic;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// The list results order.
    /// </summary>
    public enum ListResultsOrder
    { 
        /// <summary>
        /// Ascending Order
        /// </summary>
        Ascending,
        
        /// <summary>
        /// Descending Order
        /// </summary>
        Descending
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
        /// list the result keys.
        /// </summary>
        /// <param name="maxResults">The max results.</param>
        /// <param name="start">The start.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="orderBy">order by.</param>
        /// <returns>the list of keys in the result.</returns>
        IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending);

        /// <summary>
        /// Stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The results of a profiling session.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being un-viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        void Save(MiniProfiler profiler);

        /// <summary>
        /// Returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>, which should map to <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        /// <returns>
        /// The <see cref="MiniProfiler"/>.
        /// </returns>
        MiniProfiler Load(Guid id);

        /// <summary>
        /// Sets a particular profiler session so it is considered "un-viewed"  
        /// </summary>
        /// <param name="user">
        /// The user.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        void SetUnviewed(string user, Guid id);

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">
        /// The user.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        void SetViewed(string user, Guid id);

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">
        /// User identified by the current <c>MiniProfiler.Settings.UserProvider</c>
        /// </param>
        /// <returns>the list of key values.</returns>
        List<Guid> GetUnviewedIds(string user);
    }
}
