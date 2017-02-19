using StackExchange.Profiling.Storage;
using System;
using System.Threading.Tasks;

namespace StackExchange.Profiling
{
    /// <summary>
    /// BaseProfilerProvider.  This providers some helper methods which provide access to
    /// internals not otherwise available.
    /// To use, override the <see cref="Start(string)"/>, <see cref="Stop"/> and <see cref="GetCurrentProfiler"/>
    /// methods.
    /// </summary>
    public abstract class BaseProfilerProvider : IAsyncProfilerProvider
    {
        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="GetCurrentProfiler"/> should return the new MiniProfiler.
        /// </summary>
        /// <param name="profilerName">The name for the <see cref="MiniProfiler"/>.</param>
        public abstract MiniProfiler Start(string profilerName = null);

        /// <summary>
        /// Stops the current MiniProfiler (if any is currently running).
        /// <see cref="SaveProfiler"/> should be called if <paramref name="discardResults"/> is false
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public abstract void Stop(bool discardResults);

        /// <summary>
        /// Asynchronously stops the current MiniProfiler (if any is currently running).
        /// <see cref="SaveProfiler"/> should be called if <paramref name="discardResults"/> is false
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public abstract Task StopAsync(bool discardResults);

        /// <summary>
        /// Returns the current MiniProfiler.  This is used by <see cref="MiniProfiler.Current"/>.
        /// </summary>
        public abstract MiniProfiler GetCurrentProfiler();

        /// <summary>
        /// Sets <paramref name="profiler"/> to be active (read to start profiling)
        /// This should be called once a new MiniProfiler has been created.
        /// </summary>
        /// <param name="profiler">Sets a <see cref="MiniProfiler"/> to active/enabled.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="profiler"/> is <c>null</c>.</exception>
        protected static void SetProfilerActive(MiniProfiler profiler)
        {
            if (profiler == null)
                throw new ArgumentNullException(nameof(profiler));

            profiler.IsActive = true;
        }

        /// <summary>
        /// Stops the profiler and marks it as inactive.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <returns>True if successful, false if Stop had previously been called on this profiler</returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="profiler"/> is <c>null</c>.</exception>
        protected static bool StopProfiler(MiniProfiler profiler)
        {
            if (profiler == null)
                throw new ArgumentNullException(nameof(profiler));

            if (!profiler.StopImpl())
                return false;

            profiler.IsActive = false;
            return true;
        }

        /// <summary>
        /// Calls <see cref="IAsyncStorage.Save(MiniProfiler)"/> to save the current
        /// profiler using the current storage settings. 
        /// If <see cref="MiniProfiler.Storage"/> is set, this will be used.
        /// </summary>
        /// <param name="current">The <see cref="MiniProfiler"/> to save.</param>
        protected static void SaveProfiler(MiniProfiler current)
        {
            // because we fetch profiler results after the page loads, we have to put them somewhere in the meantime
            // If the current MiniProfiler object has a custom IAsyncStorage set in the Storage property, use it. Else use the Global Storage.
            var storage = current.Storage ?? MiniProfiler.Settings.Storage;
            storage.Save(current);
            if (storage.SetUnviewedAfterSave && !current.HasUserViewed)
            {
                storage.SetUnviewed(current.User, current.Id);
            }
        }

        /// <summary>
        /// Asynchronously calls <see cref="IAsyncStorage.SaveAsync(MiniProfiler)"/> to save the current
        /// profiler using the current storage settings. 
        /// If <see cref="MiniProfiler.Storage"/> is set, this will be used.
        /// </summary>
        /// <param name="current">The <see cref="MiniProfiler"/> to save.</param>
        protected static async Task SaveProfilerAsync(MiniProfiler current)
        {
            var storage = current.Storage ?? MiniProfiler.Settings.Storage;
            await storage.SaveAsync(current).ConfigureAwait(false);
            if (storage.SetUnviewedAfterSave && !current.HasUserViewed)
            {
                await storage.SetUnviewedAsync(current.User, current.Id).ConfigureAwait(false);
            }
        }
    }
}
