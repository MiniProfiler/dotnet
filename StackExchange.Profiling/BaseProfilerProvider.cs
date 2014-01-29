using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// BaseProfilerProvider.  This providers some helper methods which provide access to
    /// internals not otherwise available.
    /// To use, override the <see cref="Start(string)"/>, <see cref="Stop"/> and <see cref="GetCurrentProfiler"/>
    /// methods.
    /// </summary>
    public abstract class BaseProfilerProvider : IProfilerProvider
    {
        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="GetCurrentProfiler"/> should return the new MiniProfiler.
        /// </summary>
        [Obsolete("ProfileLevel is going away")]
        public abstract MiniProfiler Start(ProfileLevel level, string sessionName = null);

        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="GetCurrentProfiler"/> should return the new MiniProfiler.
        /// </summary>
        public abstract MiniProfiler Start(string sessionName = null);

        /// <summary>
        /// Stops the current MiniProfiler (if any is currently running).
        /// <see cref="SaveProfiler"/> should be called if <paramref name="discardResults"/> is false
        /// </summary>
        /// <param name="discardResults">If true, any current results will be thrown away and nothing saved</param>
        public abstract void Stop(bool discardResults);

        /// <summary>
        /// Returns the current MiniProfiler.  This is used by <see cref="MiniProfiler.Current"/>.
        /// </summary>
        public abstract MiniProfiler GetCurrentProfiler();

        /// <summary>
        /// Sets <paramref name="profiler"/> to be active (read to start profiling)
        /// This should be called once a new MiniProfiler has been created.
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="profiler"/> is null</exception>
        protected static void SetProfilerActive(MiniProfiler profiler)
        {
            if (profiler == null)
                throw new ArgumentNullException("profiler");

            profiler.IsActive = true;
        }

        /// <summary>
        /// Stops the profiler and marks it as inactive.
        /// </summary>
        /// <returns>True if successful, false if Stop had previously been called on this profiler</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="profiler"/> is null</exception>
        protected static bool StopProfiler(MiniProfiler profiler)
        {
            if (profiler == null)
                throw new ArgumentNullException("profiler");

            if (!profiler.StopImpl())
                return false;

            profiler.IsActive = false;
            return true;
        }

        /// <summary>
        /// Calls <see cref="MiniProfiler.Settings.EnsureStorageStrategy"/> to save the current
        /// profiler using the current storage settings. 
        /// If <see cref="MiniProfiler.Storage"/> is set, this will be used.
        /// </summary>
        protected static void SaveProfiler(MiniProfiler current)
        {
            // because we fetch profiler results after the page loads, we have to put them somewhere in the meantime
            // If the current MiniProfiler object has a custom IStorage set in the Storage property, use it. Else use the Global Storage.
            var storage = current.Storage;
            if (storage == null)
            {
                MiniProfiler.Settings.EnsureStorageStrategy();
                storage = MiniProfiler.Settings.Storage;
            }
            storage.Save(current);
            if (current.HasUserViewed == false)
            {
                storage.SetUnviewed(current.User, current.Id);
            }
        }
    }
}
