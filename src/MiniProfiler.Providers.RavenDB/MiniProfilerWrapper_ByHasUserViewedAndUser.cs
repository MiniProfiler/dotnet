using System.Linq;
using Raven.Client.Documents.Indexes;

namespace StackExchange.Profiling
{
    /// <summary>
    /// The MiniProfiler index
    /// </summary>
    internal class MiniProfilerWrapper_ByHasUserViewedAndUser : AbstractIndexCreationTask<MiniProfilerWrapper>
    {
        /// <summary>
        /// 
        /// </summary>
        public MiniProfilerWrapper_ByHasUserViewedAndUser()
        {
            Map = docs => from profiler in docs
                select new 
                {
                    profiler.HasUserViewed,
                    profiler.User
                };
        }
    }
}
