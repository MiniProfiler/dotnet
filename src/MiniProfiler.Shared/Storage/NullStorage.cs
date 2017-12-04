using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Empty storage no-nothing provider for doing nothing at all. Super efficient.
    /// </summary>
    public class NullStorage : IAsyncStorage
    {
        /// <summary>
        /// Returns no profilers.
        /// </summary>
        /// <param name="maxResults">No one cares.</param>
        /// <param name="start">No one cares.</param>
        /// <param name="finish">No one cares.</param>
        /// <param name="orderBy">No one cares.</param>
        public IEnumerable<Guid> List(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending) => Enumerable.Empty<Guid>();
        /// <summary>
        /// Returns no profilers.
        /// </summary>
        /// <param name="maxResults">No one cares.</param>
        /// <param name="start">No one cares.</param>
        /// <param name="finish">No one cares.</param>
        /// <param name="orderBy">No one cares.</param>
        public Task<IEnumerable<Guid>> ListAsync(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending) => Task.FromResult(Enumerable.Empty<Guid>());
        /// <summary>
        /// Saves nothing.
        /// </summary>
        /// <param name="profiler">No one cares.</param>
        public void Save(MiniProfiler profiler) { /* no-op */ }
        /// <summary>
        /// Saves nothing.
        /// </summary>
        /// <param name="profiler">No one cares.</param>
        public Task SaveAsync(MiniProfiler profiler) => Task.CompletedTask;
        /// <summary>
        /// Returns null.
        /// </summary>
        /// <param name="id">No one cares.</param>
        public MiniProfiler Load(Guid id) => null;
        /// <summary>
        /// Returns null.
        /// </summary>
        /// <param name="id">No one cares.</param>
        public Task<MiniProfiler> LoadAsync(Guid id) => Task.FromResult((MiniProfiler)null);
        /// <summary>
        /// Sets nothing.
        /// </summary>
        /// <param name="user">No one cares.</param>
        /// <param name="id">No one cares.</param>
        public void SetUnviewed(string user, Guid id) { /* no-op */ }
        /// <summary>
        /// Sets nothing.
        /// </summary>
        /// <param name="user">No one cares.</param>
        /// <param name="id">No one cares.</param>
        public Task SetUnviewedAsync(string user, Guid id) => Task.CompletedTask;
        /// <summary>
        /// Sets nothing.
        /// </summary>
        /// <param name="user">No one cares.</param>
        /// <param name="id">No one cares.</param>
        public void SetViewed(string user, Guid id) { /* no-op */ }
        /// <summary>
        /// Sets nothing.
        /// </summary>
        /// <param name="user">No one cares.</param>
        /// <param name="id">No one cares.</param>
        public Task SetViewedAsync(string user, Guid id) => Task.CompletedTask;
        /// <summary>
        /// Gets nothing.
        /// </summary>
        /// <param name="user">No one cares.</param>
        public List<Guid> GetUnviewedIds(string user) => new List<Guid>();
        /// <summary>
        /// Gets nothing.
        /// </summary>
        /// <param name="user">No one cares.</param>
        public Task<List<Guid>> GetUnviewedIdsAsync(string user) => Task.FromResult(new List<Guid>());
    }
}
