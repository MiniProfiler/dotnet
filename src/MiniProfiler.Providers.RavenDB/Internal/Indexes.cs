using System.Linq;
using Raven.Client.Documents.Indexes;

namespace StackExchange.Profiling.Storage.Internal
{
    /// <summary>
    /// The MiniProfiler index by user for has viewed
    /// </summary>
    internal class Index_ByHasUserViewedAndUser : AbstractIndexCreationTask<MiniProfilerDoc>
    {
        public Index_ByHasUserViewedAndUser() =>
            Map = docs => from profiler in docs select new { profiler.HasUserViewed, profiler.User };
    }

    /// <summary>
    /// The MiniProfiler index by profiler Id
    /// </summary>
    internal class Index_ByProfilerId : AbstractIndexCreationTask<MiniProfilerDoc>
    {
        public Index_ByProfilerId() =>
            Map = docs => from profiler in docs select new { profiler.ProfilerId };
    }

    /// <summary>
    /// The MiniProfiler index by start time
    /// </summary>
    internal class Index_ByStarted : AbstractIndexCreationTask<MiniProfilerDoc>
    {
        public Index_ByStarted() =>
            Map = docs => from profiler in docs select new { profiler.Started };
    }
}
