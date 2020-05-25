using System.Linq;
using Raven.Client.Documents.Indexes;

namespace StackExchange.Profiling
{
    /// <summary>
    /// The MiniProfiler index
    /// </summary>
    internal class MiniProfilerWrapper_ByProfileId : AbstractIndexCreationTask<MiniProfilerWrapper>
    {
        /// <summary>
        /// 
        /// </summary>
        public MiniProfilerWrapper_ByProfileId()
        {
            Map = docs => from profiler in docs
                select new 
                {
                    profiler.ProfileId
                };
        }
    }
}
