using System;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    public partial class MiniProfiler
    {
        /// <summary>
        /// Settings for context-less settings access.
        /// For example, every <see cref="MiniProfiler"/> deserialized from a store would have these settings.
        /// </summary>
        public static MiniProfilerBaseOptions DefaultOptions { get; private set; }

        /// <summary>
        /// Saves the given <paramref name="options"/> as the global <see cref="DefaultOptions"/> available for use globally.
        /// These are intended to be used by global/background operations where normal context access isn't available.
        /// </summary>
        /// <typeparam name="T">The specific type of <see cref="MiniProfilerBaseOptions"/> to use.</typeparam>
        /// <param name="options">The options object 44to set for background access.</param>
        public static T Configure<T>(T options) where T : MiniProfilerBaseOptions
        {
            DefaultOptions = options ?? throw new ArgumentNullException(nameof(options));
            options.Configure(); // Event handler of sorts
            return options;
        }

        /// <summary>
        /// Starts a new <see cref="MiniProfiler"/> based on <see cref="DefaultOptions"/>.
        /// Shortcut for DefaultOptions.ProfilerProvider.Start.
        /// </summary>
        /// <param name="profilerName">
        /// Allows explicit naming of the new profiling session; when null, an appropriate default will be used, e.g. for
        /// a web request, the URL will be used for the overall session name.
        /// </param>
        public static MiniProfiler StartNew(string profilerName = null) => DefaultOptions?.StartProfiler(profilerName);
    }
}
