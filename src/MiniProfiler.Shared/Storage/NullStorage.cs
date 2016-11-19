using System;
using System.Linq;
using System.Collections.Generic;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Empty storage provider, used if absolutely nothing is configured.
    /// </summary>
    internal class NullStorage : IStorage
    {
        public NullStorage() { }
        public IEnumerable<Guid> List(
            int maxResults, 
            DateTime? start = null, 
            DateTime? finish = null, 
            ListResultsOrder orderBy = ListResultsOrder.Descending) => Enumerable.Empty<Guid>();
        public void Save(MiniProfiler profiler) { }
        public MiniProfiler Load(Guid id) => null;
        public void SetUnviewed(string user, Guid id) { }
        public void SetViewed(string user, Guid id) { }
        public List<Guid> GetUnviewedIds(string user) => new List<Guid>();
    }
}
