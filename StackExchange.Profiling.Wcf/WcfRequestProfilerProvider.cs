using System;
using System.Diagnostics;
using System.ServiceModel;
using StackExchange.Profiling.Wcf.Helpers;
using StackExchange.Profiling.Wcf.Storage;

namespace StackExchange.Profiling.Wcf
{
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
        /// <returns>the mini profiler.</returns>
        public override MiniProfiler Start(string sessionName = null)
        {
            var context = WcfInstanceContext.Current;
            if (context == null) return null;

            var operationContext = OperationContext.Current;
            if (operationContext == null) return null;

            var instanceContext = operationContext.InstanceContext;
            if (instanceContext == null) return null;

            string serviceName = GetProfilerName(operationContext, instanceContext);

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

            var result = new MiniProfiler(sessionName ?? serviceName);

            SetCurrentProfiler(result);

            // don't really want to pass in the context to MiniProfler's constructor or access it statically in there, either
            result.User = (Settings.UserProvider ?? new EmptyUserProvider()).GetUser(/*context.Request*/);

            SetProfilerActive(result);

            return result;
        }

        /// <summary>
        /// start the profiler.
        /// </summary>
        /// <param name="level">The profile level.</param>
        /// <param name="sessionName">The session name</param>
        /// <returns>the mini profiler.</returns>
        [Obsolete("Please use the Start(string sessionName) overload instead of this one. ProfileLevel is going away.")]
        public override MiniProfiler Start(ProfileLevel level, string sessionName = null)
        {
            var context = WcfInstanceContext.Current;
            if (context == null) return null;

            var operationContext = OperationContext.Current;
            if (operationContext == null) return null;

            var instanceContext = operationContext.InstanceContext;
            if (instanceContext == null) return null;

            // TODO: Include the action name here as well, and null protection
            string serviceName = GetProfilerName(operationContext, instanceContext);
            
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

            var result = new MiniProfiler(sessionName ?? serviceName, level);

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
            var action = operationContext.IncomingMessageHeaders.Action;
            if (string.IsNullOrEmpty(action))
            {
                action = operationContext.IncomingMessageProperties["HttpOperationName"] as string;
            }

            if (!string.IsNullOrEmpty(action))
            {
                // For Client->Server calls the host is available here.
                string contractName;
                if (instanceContext.Host != null)
                {
                    contractName = instanceContext.Host.Description.Name;
                    action = GetActionFromUri(action);
                    return $"{contractName} [{action}]";
                }

                // For Server->Client calls (callbacks) the host is not available, 
                // maybe the action (in case of SOAP) contains the contract name 
                // in the action http://www.tempuri.org/{Contract}/{Action}/
                // Unfortunately the {Contract} is then rather the Callback-Interface 
                // instead of the actual contract but it's better than nothing 
                // and faster than trying to resolve the action otherwise. 

                action = GetActionAndContractFromUri(action);
                var slash = action.IndexOf('/');
                if (slash != -1)
                {
                    contractName = action.Substring(0, slash);
                    action = action.Substring(slash + 1);
                    return $"{contractName} [{action}]";
                }
                return action;
            }

            if (operationContext.IncomingMessageHeaders.To != null)
            {
                return operationContext.IncomingMessageHeaders.To.LocalPath;
            }

            Debug.Fail("What method is being called?");
            return "Unknown Member";
        }

        /// <summary>
        /// Tries to load the action name only form the given action determined via WCF.
        /// </summary>
        /// <param name="actionUri">The action identifier received via WCF</param>
        /// <returns>
        /// In case the action contains an URI (e.g. http://www.tempuri.org/{Contract}/{Action}) only the last part of the path is returned, 
        /// otherwise the whole actionUri is returned.
        /// </returns>
        private static string GetActionFromUri(string actionUri)
        {
            var actionAndContract = GetActionAndContractFromUri(actionUri);

            var lastSlash = actionAndContract.LastIndexOf('/');
            if (lastSlash != -1)
            {
                return actionAndContract.Substring(lastSlash + 1);
            }

            return actionAndContract;
        }

        /// <summary>
        /// Tries to load the action name only form the given action determined via WCF.
        /// </summary>
        /// <param name="actionUri">The action identifier received via WCF</param>
        /// <returns>
        /// In case the action contains an URI (e.g. http://www.tempuri.org/{Contract}/{Action}) only the local path without domain and protocol
        /// is returned, otherwise the whole actionUri is returned.
        /// </returns>
        private static string GetActionAndContractFromUri(string actionUri)
        {
            Uri parsedUri;
            if (Uri.TryCreate(actionUri, UriKind.RelativeOrAbsolute, out parsedUri))
            {
                var path = parsedUri.LocalPath;
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }
            return actionUri;
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
