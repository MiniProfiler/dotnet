using System.Linq;
using Raven.Client.Documents.Indexes;

namespace StackExchange.Profiling
{
    /// <summary>
    /// The MiniProfiler index
    /// </summary>
    public class MiniProfilerIdIndex : AbstractIndexCreationTask<MiniProfilerWrapper>
    {
        /// <summary>
        /// 
        /// </summary>
        public MiniProfilerIdIndex()
        {
            this.Indexes.Add(x => x.User, FieldIndexing.Search);
            this.Indexes.Add(x => x.HasUserViewed, FieldIndexing.Search);
            this.Indexes.Add(x => x.Started, FieldIndexing.Search);

            Map = docs => from profiler in docs
                select new 
                {
                    profiler.ProfileId,
                };
        }
    }
}
