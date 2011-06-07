using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Web;

namespace MvcMiniProfiler
{
    partial class MiniProfiler
    {
        /// <summary>
        /// Various configuration properties.
        /// </summary>
        public static class Settings
        {
            static Settings()
            {
                var props = from p in typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Static)
                            let t = typeof(DefaultValueAttribute)
                            where p.IsDefined(t, inherit: false)
                            let a = p.GetCustomAttributes(t, inherit: false).Single() as DefaultValueAttribute
                            select new { PropertyInfo = p, DefaultValue = a };

                foreach (var pair in props)
                {
                    pair.PropertyInfo.SetValue(null, pair.DefaultValue.Value, null);
                }
            }

            /// <summary>
            /// When true, link and script tags will be written to the response stream when MiniProfiler.Stop is called.
            /// </summary>
            [DefaultValue(false)]
            public static bool WriteScriptsToResponseOnStop { get; set; }

            /// <summary>
            /// Any Timing step with a duration less than or equal to this will be hidden by default in the UI; defaults to 2.0 ms.
            /// </summary>
            [DefaultValue(2.0)]
            public static double TrivialDurationThresholdMilliseconds { get; set; }

            /// <summary>
            /// Dictates on which side of the page the profiler popup button is displayed; defaults to false (i.e. renders on left side).
            /// </summary>
            [DefaultValue(false)]
            public static bool RenderPopupButtonOnRight { get; set; }

            /// <summary>
            /// When <see cref="MiniProfiler.Start"/> is called, if the current request url starts with this property,
            /// no profiler will be instantiated and no results will be displayed.  
            /// Default value is { "/mini-profiler-includes.js", "/mini-profiler-includes.less", "/mini-profiler-results", "/content/", "/scripts/" }.
            /// </summary>
            [DefaultValue(new string[] { "/mini-profiler-includes.js", "/mini-profiler-includes.less", "/mini-profiler-results", "/content/", "/scripts/" })]
            public static string[] IgnoredRootPaths { get; set; }

            /// <summary>
            /// A method that will return a MiniProfiler when given a Guid.  Meant for caching individual page profilings for a 
            /// very limited time.
            /// </summary>
            /// <remarks>
            /// By default, MiniProfilers will be cached for 5 minutes in the HttpRuntime.Cache.  This can be extended when the cache is shared
            /// from its top link.
            /// </remarks>
            public static Func<Guid, MiniProfiler> ShortTermCacheGetter { get; set; }

            /// <summary>
            /// A method that will save a MiniProfiler into a short-duration cache, so results can fetched down to the client after page load.
            /// It is important that you cache the MiniProfiler under its Id, a Guid - this Id will be passed to the ShortTermCacheGetter.
            /// </summary>
            /// <remarks>
            /// By default, MiniProfilers will be cached for 5 minutes in the HttpRuntime.Cache.
            /// </remarks>
            public static Action<MiniProfiler> ShortTermCacheSetter { get; set; }

            /// <summary>
            /// A method that will return a MiniProfiler when given a Guid.  Meant for caching profilings for an extended period of time, so
            /// they may be shared with others.
            /// </summary>
            /// <remarks>
            /// This is used by the full page results view, which is linked in the popup's header.
            /// </remarks>
            public static Func<Guid, MiniProfiler> LongTermCacheGetter { get; set; }

            /// <summary>
            /// A method that will save a MiniProfiler, identified by its Guid Id, into long-term storage.  Allows results to be shared with others.
            /// It is important that you cache the MiniProfiler under its Id, a Guid - this Id will be passed to the LongTermCacheGetter.
            /// </summary>
            /// <remarks>
            /// This is activated EVERY TIME the top left header link is clicked in the popup UI and the full page results 
            /// view is displayed.  When overriding the default, your code will need to handle setting the same profiler 
            /// back into your chosen storage medium (e.g. no-op when it already exists).
            /// </remarks>
            public static Action<MiniProfiler> LongTermCacheSetter { get; set; }

            /// <summary>
            /// When setters are null, creates default getters and setters that operate on the HttpRuntime.Cache.
            /// </summary>
            /// <remarks>
            /// Our MiniProfiler must have these to run.
            /// </remarks>
            internal static void EnsureCacheMethods()
            {
                if (ShortTermCacheGetter == null || ShortTermCacheSetter == null)
                {
                    ShortTermCacheSetter = (prof) => SetProfilerIntoRuntimeCache(CacheKey + prof.Id.ToString(), prof, DateTime.Now.AddMinutes(10));
                    ShortTermCacheGetter = (guid) => { return HttpRuntime.Cache[CacheKey + guid.ToString()] as MiniProfiler; };
                }

                if (LongTermCacheGetter == null || LongTermCacheSetter == null)
                {
                    LongTermCacheSetter = (prof) => SetProfilerIntoRuntimeCache(CacheKey + "longterm:" + prof.Id.ToString(), prof, DateTime.Now.AddDays(1));
                    LongTermCacheGetter = (guid) => { return HttpRuntime.Cache[CacheKey + "longterm:" + guid.ToString()] as MiniProfiler; };
                }
            }

            private static void SetProfilerIntoRuntimeCache(string key, MiniProfiler prof, DateTime expires)
            {
                HttpRuntime.Cache.Add(
                    key: key,
                    value: prof,
                    dependencies: null,
                    absoluteExpiration: expires,
                    slidingExpiration: System.Web.Caching.Cache.NoSlidingExpiration,
                    priority: System.Web.Caching.CacheItemPriority.Low,
                    onRemoveCallback: null);
            }

        }
    }
}
