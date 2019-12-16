using StackExchange.Profiling.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Default profile provider, gracefully handles async transitions.
    /// To use, override the <see cref="Start(string, MiniProfilerBaseOptions)"/>, <see cref="Stopped(MiniProfiler, bool)"/> and <see cref="CurrentProfiler"/>
    /// methods.
    /// </summary>
    public class DefaultProfilerProvider : IAsyncProfilerProvider
    {
        private static readonly AsyncLocal<MiniProfiler> _profiler = new AsyncLocal<MiniProfiler>();

        /// <summary>
        /// The current profiler instance, statically resolved and backed by AsyncLocal{T}.
        /// </summary>
        public virtual MiniProfiler CurrentProfiler
        {
            get => _profiler.Value;
            protected set => _profiler.Value = value;
        }

        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="CurrentProfiler"/> should return the new MiniProfiler.
        /// </summary>
        /// <param name="profilerName">
        /// Allows explicit naming of the new profiling session; when null, an appropriate default will be used, e.g. for
        /// a web request, the URL will be used for the overall session name.
        /// </param>
        /// <param name="options">The options to start the MiniProfiler with. Likely a more-specific type underneath.</param>
        public virtual MiniProfiler Start(string profilerName, MiniProfilerBaseOptions options) =>
            CurrentProfiler = new MiniProfiler(profilerName ?? nameof(MiniProfiler), options);

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public virtual void Stopped(MiniProfiler profiler, bool discardResults)
        {
            if (profiler == null) return;
            if (discardResults)
            {
                if (CurrentProfiler == profiler)
                {
                    CurrentProfiler = null;
                }
                return;
            }
            Save(profiler);
        }

        /// <summary>
        /// Asynchronously stops the current MiniProfiler (if any is currently running).
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public virtual async Task StoppedAsync(MiniProfiler profiler, bool discardResults)
        {
            if (profiler == null) return;
            if (discardResults)
            {
                if (CurrentProfiler == profiler)
                {
                    CurrentProfiler = null;
                }
                return;
            }
            await SaveAsync(profiler).ConfigureAwait(false);
        }

        /// <summary>
        /// Calls <see cref="Storage.IAsyncStorage.Save(MiniProfiler)"/> to save the current
        /// profiler using the current storage settings. 
        /// If <see cref="MiniProfiler.Storage"/> is set, this will be used.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        protected void Save(MiniProfiler profiler)
        {
            // because we fetch profiler results after the page loads, we have to put them somewhere in the meantime
            // If the current MiniProfiler object has a custom IAsyncStorage set in the Storage property, use it. Else use the Global Storage.
            var storage = profiler.Storage;
            if (storage == null)
            {
                return;
            }
            storage.Save(profiler);
        }

        /// <summary>
        /// Asynchronously calls <see cref="Storage.IAsyncStorage.SaveAsync(MiniProfiler)"/> to save the current
        /// profiler using the current storage settings. 
        /// If <see cref="MiniProfiler.Storage"/> is set, this will be used.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        protected async Task SaveAsync(MiniProfiler profiler)
        {
            var storage = profiler.Storage;
            if (storage == null)
            {
                return;
            }
            await storage.SaveAsync(profiler).ConfigureAwait(false);
        }
    }
}
