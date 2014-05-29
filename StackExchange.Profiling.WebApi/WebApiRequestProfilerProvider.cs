namespace StackExchange.Profiling.WebApi
{
    public sealed class WebApiRequestProfilerProvider : BaseProfilerProvider
    {
        private const string CacheKey = ":mini-profiler:";

        private MiniProfiler CurrentProfiler
        {
            get
            {
                MiniProfiler currentProfiler = null;

                var context = WebApiContext.Current;

                if (context != null && context.Request != null)
                {
                    object cached;

                    if (context.Request.Properties.TryGetValue(CacheKey, out cached))
                    {
                        currentProfiler = cached as MiniProfiler;
                    }
                }

                return currentProfiler;
            }
            set
            {
                var context = WebApiContext.Current;

                if (context != null && context.Request != null)
                {
                    context.Request.Properties[CacheKey] = value;
                }
            }
        }

        public override MiniProfiler GetCurrentProfiler()
        {
            return CurrentProfiler;
        }

        public override MiniProfiler Start(ProfileLevel level, string sessionName = null)
        {
            var context = WebApiContext.Current;

            if (context.Request == null)
            {
                return null;
            }

            var url = context.Request.RequestUri;

            // TODO: Check against MiniProfiler.Settings.IgnoredPaths

            var profiler = new MiniProfiler(sessionName ?? url.OriginalString, level);
            SetProfilerActive(profiler);

            // TODO: Set profiler.User

            CurrentProfiler = profiler;

            return profiler;
        }

        public override MiniProfiler Start(string sessionName = null)
        {
            return Start(ProfileLevel.Info, sessionName);
        }

        public override void Stop(bool discardResults)
        {
            var context = WebApiContext.Current;

            if (context.Request == null)
            {
                return;
            }

            var profiler = CurrentProfiler;

            if (profiler == null)
            {
                return;
            }

            if (!StopProfiler(profiler))
            {
                return;
            }

            if (discardResults)
            {
                CurrentProfiler = null;
                return;
            }

            // TODO: EnsureName

            SaveProfiler(profiler);

            // TODO: Set header?
        }
    }
}