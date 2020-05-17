using System.Linq;
using Raven.Client.Documents.Indexes;

namespace StackExchange.Profiling
{
    /// <summary>
    /// The MiniProfiler index
    /// </summary>
    public class MiniProfilerWrapper_ByStarted : AbstractIndexCreationTask<MiniProfilerWrapper>
    {
        /// <summary>
        /// 
        /// </summary>
        public MiniProfilerWrapper_ByStarted()
        {
            Map = docs => from profiler in docs
                select new 
                {
                    profiler.Started
                };
        }
    }
}
