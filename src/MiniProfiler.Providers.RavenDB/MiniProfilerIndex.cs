using System.Linq;
using Raven.Client.Documents.Indexes;

namespace StackExchange.Profiling
{
    /// <summary>
    /// The MiniProfiler index
    /// </summary>
    public class MiniProfilerIndex : AbstractIndexCreationTask<MiniProfilerWrapper>
    {
        /// <summary>
        /// 
        /// </summary>
        public MiniProfilerIndex()
        {
            this.Indexes.Add(x => x.User, FieldIndexing.Search);
            this.Indexes.Add(x => x.HasUserViewed, FieldIndexing.Search);
            this.Indexes.Add(x => x.Started, FieldIndexing.Search);

            Map = docs => from profiler in docs
                select new MiniProfiler
                {
                    Id = profiler.ProfileId,
                    Name =  profiler.Name,
                    Started = profiler.Started,
                    DurationMilliseconds = profiler.DurationMilliseconds,
                    MachineName = profiler.MachineName,
                    CustomLinks = profiler.CustomLinks,
                    CustomLinksJson = profiler.CustomLinksJson,
                    Root = profiler.Root,
                    ClientTimings = profiler.ClientTimings,
                    User = profiler.User,
                    HasUserViewed = profiler.HasUserViewed
                };
        }
    }
}
