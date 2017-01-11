using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Empty storage provider, used if absolutely nothing is configured.
    /// </summary>
    internal class NullStorage : IAsyncStorage
    {
        private static readonly Task _completed = Task.FromResult(false);

        public IEnumerable<Guid> List(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending) => Enumerable.Empty<Guid>();
        public Task<IEnumerable<Guid>> ListAsync(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending) => Task.FromResult(Enumerable.Empty<Guid>());
        public void Save(MiniProfiler profiler) { /* no-op */ }
        public Task SaveAsync(MiniProfiler profiler) => _completed;
        public MiniProfiler Load(Guid id) => null;
        public Task<MiniProfiler> LoadAsync(Guid id) => Task.FromResult((MiniProfiler)null);
        public void SetUnviewed(string user, Guid id) { /* no-op */ }
        public Task SetUnviewedAsync(string user, Guid id) => _completed;
        public void SetViewed(string user, Guid id) { /* no-op */ }
        public Task SetViewedAsync(string user, Guid id) => _completed;
        public List<Guid> GetUnviewedIds(string user) => new List<Guid>();
        public Task<List<Guid>> GetUnviewedIdsAsync(string user) => Task.FromResult(new List<Guid>());
    }
}
