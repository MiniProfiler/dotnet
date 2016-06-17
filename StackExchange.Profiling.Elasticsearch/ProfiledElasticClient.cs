using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Elasticsearch.Net;
using Nest;

namespace StackExchange.Profiling.Elasticsearch
{
    /// <summary>
    /// Profiled version of <see cref="ElasticClient"/>. Handles responses and pushes data to current <see cref="MiniProfiler"/>'s session.
    /// </summary>
	public class ProfiledElasticClient : ElasticClient
    {
        private readonly MiniProfiler _profiler = null;
        private readonly Timing _headTiming = null;

        #region static
        static ProfiledElasticClient()
        {
            ExcludeElasticsearchAssemblies();
        }

        /// <summary>
        /// Enabling configuration settings prior to receive internal API call timings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
		internal static void ApplyConfigurationSettings<T>(ConnectionConfiguration<T> configuration) where T : ConnectionConfiguration<T>
        {
            //configuration.EnableMetrics();
            //configuration.ExposeRawResponse();
        }

        /// <summary>
        /// Exclude assemblies, so they won't be included into <see cref="MiniProfiler"/> timings' call-stack.
        /// </summary>
		internal static void ExcludeElasticsearchAssemblies()
        {
            MiniProfiler.Settings.ExcludeAssembly("Elasticsearch.Net");
            MiniProfiler.Settings.ExcludeAssembly("Nest");
            MiniProfiler.Settings.ExcludeAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        }
        #endregion

        /// <summary>
        /// Provides base <see cref="ElasticClient"/> with profiling features to current <see cref="MiniProfiler"/> session.
        /// </summary>
        /// <param name="configuration">Instance of <see cref="ConnectionSettings"/>. Its responses will be handled and pushed to <see cref="MiniProfiler"/></param>
        public ProfiledElasticClient(ConnectionSettings configuration)
            : base(configuration)
        {
            _profiler = MiniProfiler.Current;
            if (_profiler != null)
                _headTiming = _profiler.Head;

            if (_headTiming != null)
            {
                ApplyConfigurationSettings(configuration);

                configuration.OnRequestCompleted(response => HandleResponse(response, _profiler, _headTiming));
            }
        }

        #region MyRegion
        /// <summary>
        /// Handles <see cref="IApiCallDetails"/> and pushes <see cref="CustomTiming"/> to current <see cref="MiniProfiler"/> session.
        /// </summary>
        /// <param name="callDetails"><see cref="IApiCallDetails"/> to be handled.</param>
        /// <param name="profiler">Current <see cref="MiniProfiler"/> session instance.</param>
        /// <param name="headTiming">Current <see cref="Timing"/> session instance.</param>
		internal static void HandleResponse(IApiCallDetails callDetails, MiniProfiler profiler, Timing headTiming)
        {
            if (callDetails == null)
                return;

            headTiming.AddCustomTiming("elasticsearch", new CustomTiming(profiler, BuildCommandString(callDetails))
            {
                Id = Guid.NewGuid(),
                DurationMilliseconds = callDetails.AuditTrail == null ? null : (decimal?)callDetails.AuditTrail.Sum(a => (a.Ended - a.Started).TotalMilliseconds),
                ExecuteType = callDetails.HttpMethod.ToString()
            });
        }

        /// <summary>
        /// Processes <see cref="IApiCallDetails"/> and builds command string for <see cref="CustomTiming"/> instance.
        /// </summary>
        /// <param name="callDetails"><see cref="IApiCallDetails"/> to be processed.</param>
        /// <returns></returns>
		private static string BuildCommandString(IApiCallDetails callDetails)
        {
            var commandTextBuilder = new StringBuilder();

            commandTextBuilder.AppendFormat("{0} {2} {1}", callDetails.HttpMethod, callDetails.HttpStatusCode, callDetails.Uri);

            if (callDetails.HttpStatusCode != (int)HttpStatusCode.NotFound && !callDetails.Success && callDetails.OriginalException != null)
            {
                commandTextBuilder.AppendLine();

                commandTextBuilder.Append(callDetails.OriginalException);
            }

            return commandTextBuilder.ToString();
        }
        #endregion
    }
}