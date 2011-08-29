using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcMiniProfiler.Wcf.Helpers;
using MvcMiniProfiler.Wcf.Storage;
using System.ServiceModel;

namespace MvcMiniProfiler.Wcf
{
    /// <summary>
    /// Wcf implementation of a profile provider.  This provider uses <see cref="MvcMiniProfiler.Wcf.Helpers.WcfInstanceContext"/>
    /// to keep one MiniProfiler for each request
    /// </summary>
    public partial class WcfRequestProfilerProvider : BaseProfilerProvider
    {
        public WcfRequestProfilerProvider()
        {
            // By default use a per request storage model only
            MiniProfiler.Settings.Storage =
                MiniProfiler.Settings.Storage ?? new WcfRequestInstanceStorage();
        }

        public override MiniProfiler Start(ProfileLevel level)
        {
            var context = WcfInstanceContext.Current;
            if (context == null) return null;

            var operationContext = OperationContext.Current;
            if (operationContext == null) return null;

            var instanceContext = operationContext.InstanceContext;
            if (instanceContext == null) return null;

            // TODO: Include the action name here as well, and null protection
            string serviceName = instanceContext.Host.Description.Name;// .BaseAddresses.FirstOrDefault();

            // TODO: Ignored paths - currently solely based on servicename

            //var url = context.Request.Url;
            //var path = context.Request.AppRelativeCurrentExecutionFilePath.Substring(1);

            // don't profile /content or /scripts, either - happens in web.dev
            foreach (var ignored in MiniProfiler.Settings.IgnoredPaths ?? new string[0])
            {
                if (serviceName.ToUpperInvariant().Contains((ignored ?? "").ToUpperInvariant()))
                    return null;
            }

            var result = new MiniProfiler(GetProfilerName(operationContext, instanceContext), level);

            SetCurrentProfiler(result);

            // don't really want to pass in the context to MiniProfler's constructor or access it statically in there, either
            result.User = (Settings.UserProvider ?? new EmptyUserProvider()).GetUser(/*context.Request*/);

            SetProfilerActive(result);

            return result;
        }

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



        public override MiniProfiler GetCurrentProfiler()
        {
            var context = WcfInstanceContext.GetCurrentWithoutInstantiating();
            if (context == null) return null;

            return context.Items[WcfCacheKey] as MiniProfiler;
        }

        private const string WcfCacheKey = ":mini-profiler:";

        private void SetCurrentProfiler(MiniProfiler profiler)
        {
            var context = WcfInstanceContext.Current;
            if (context == null) return;

            context.Items[WcfCacheKey] = profiler;
        }


        /// <summary>
        /// Makes sure 'profiler' has a Name, pulling it from route data or url.
        /// </summary>
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

        private static string GetProfilerName(OperationContext operationContext, InstanceContext instanceContext)
        {
            // TODO: Include the action name here as well, and null protection
            var action = operationContext.IncomingMessageHeaders.Action;

            string serviceName = string.Format("{0} [{1}]",
                instanceContext.Host.Description.Name,
                action);

            return serviceName;
        }
    }
}
