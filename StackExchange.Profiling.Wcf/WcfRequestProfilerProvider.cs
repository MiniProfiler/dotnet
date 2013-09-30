namespace StackExchange.Profiling.Wcf
{
    using System.ServiceModel;

    using StackExchange.Profiling.Wcf.Helpers;
    using StackExchange.Profiling.Wcf.Storage;

    /// <summary>
    /// wCF implementation of a profile provider.  This provider uses <see cref="StackExchange.Profiling.Wcf.Helpers.WcfInstanceContext"/>
    /// to keep one MiniProfiler for each request
    /// </summary>
    public partial class WcfRequestProfilerProvider : BaseProfilerProvider
    {
        /// <summary>
        /// The WCF cache key.
        /// </summary>
        private const string WcfCacheKey = ":mini-profiler:";

        /// <summary>
        /// Initialises a new instance of the <see cref="WcfRequestProfilerProvider"/> class.
        /// </summary>
        public WcfRequestProfilerProvider()
        {
            // By default use a per request storage model only
            MiniProfiler.Settings.Storage =
                MiniProfiler.Settings.Storage ?? new WcfRequestInstanceStorage();
        }

        /// <summary>
        /// start the profiler.
        /// </summary>
        /// <param name="level">The profile level.</param>
        /// <returns>the mini profiler.</returns>
        public override MiniProfiler Start(ProfileLevel level, string sessionName = null)
        {
            var context = WcfInstanceContext.Current;
            if (context == null) return null;

            var operationContext = OperationContext.Current;
            if (operationContext == null) return null;

            var instanceContext = operationContext.InstanceContext;
            if (instanceContext == null) return null;

            // TODO: Include the action name here as well, and null protection
            string serviceName = instanceContext.Host.Description.Name;
            
            // BaseAddresses.FirstOrDefault();
            // TODO: Ignored paths - currently solely based on servicename

            // var url = context.Request.Url;
            // var path = context.Request.AppRelativeCurrentExecutionFilePath.Substring(1);

            // don't profile /content or /scripts, either - happens in web.dev
            foreach (var ignored in MiniProfiler.Settings.IgnoredPaths ?? new string[0])
            {
                if (serviceName.ToUpperInvariant().Contains((ignored ?? string.Empty).ToUpperInvariant()))
                    return null;
            }

            var result = new MiniProfiler(sessionName ?? GetProfilerName(operationContext, instanceContext), level);

            SetCurrentProfiler(result);

            // don't really want to pass in the context to MiniProfler's constructor or access it statically in there, either
            result.User = (Settings.UserProvider ?? new EmptyUserProvider()).GetUser(/*context.Request*/);

            SetProfilerActive(result);

            return result;
        }

        /// <summary>
        /// stop the profiler.
        /// </summary>
        /// <param name="discardResults">The discard results.</param>
        public override void Stop(bool discardResults)
        {
            var current = GetCurrentProfiler();

            if (current == null)
                return;

            // stop our timings - when this is false, we've already called .Stop before on this session
            if (!StopProfiler(current))
                return;

            if (discardResults)
            {
                SetCurrentProfiler(null);
                return;
            }

            // set the profiler name to Controller/Action or /url
            EnsureServiceName(current);

            // save the profiler
            SaveProfiler(current);
        }

        /// <summary>
        /// get the current profiler.
        /// </summary>
        /// <returns>the mini profiler.</returns>
        public override MiniProfiler GetCurrentProfiler()
        {
            var context = WcfInstanceContext.GetCurrentWithoutInstantiating();
            if (context == null) return null;

            return context.Items[WcfCacheKey] as MiniProfiler;
        }

        /// <summary>
        /// get the profiler name.
        /// </summary>
        /// <param name="operationContext">The operation context.</param>
        /// <param name="instanceContext">The instance context.</param>
        /// <returns>a string containing the profiler name.</returns>
        private static string GetProfilerName(OperationContext operationContext, InstanceContext instanceContext)
        {
            // TODO: Include the action name here as well, and null protection
            var action = operationContext.IncomingMessageHeaders.Action;

            var serviceName = string.Format("{0} [{1}]", instanceContext.Host.Description.Name, action);

            return serviceName;
        }

        /// <summary>
        /// Makes sure 'profiler' has a Name, pulling it from route data or url.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        private static void EnsureServiceName(MiniProfiler profiler/*, HttpRequest request*/)
        {
            // also set the profiler name to Controller/Action or /url
            if (string.IsNullOrWhiteSpace(profiler.Name))
            {
                profiler.Name = "Unknown";

                var operationContext = OperationContext.Current;
                if (operationContext == null) return;

                var instanceContext = operationContext.InstanceContext;
                if (instanceContext == null) return;

                profiler.Name = GetProfilerName(operationContext, instanceContext);
            }
        }

        /// <summary>
        /// set the current profiler.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        private void SetCurrentProfiler(MiniProfiler profiler)
        {
            var context = WcfInstanceContext.Current;
            if (context == null) return;

            context.Items[WcfCacheKey] = profiler;
        }
    }
}
