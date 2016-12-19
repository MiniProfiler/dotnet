﻿using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Mostly for unit testing and single-threaded apps, only allows one 
    /// instance of a <see cref="MiniProfiler"/> to be the <see cref="MiniProfiler.Current"/> one.
    /// </summary>
    public class SingletonProfilerProvider : IProfilerProvider
    {
        private MiniProfiler _profiler;

        /// <summary>
        /// The name says it all
        /// </summary>
        /// <returns></returns>
        public MiniProfiler GetCurrentProfiler()
        {
            return _profiler;
        }

        public Timing GetHead()
        {
            return _head;
        }

        public void SetHead(Timing t)
        {
            _head = t;
        }

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        public MiniProfiler Start(string sessionName = null)
        {
            _profiler = new MiniProfiler(sessionName ?? AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
            return _profiler;
        }

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        [Obsolete("Please use the Start(string sessionName) overload instead of this one. ProfileLevel is going away.")]
        public MiniProfiler Start(ProfileLevel level, string sessionName = null) 
        {
            return Start(sessionName);
        }

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        public void Stop(bool discardResults)
        {
            if (_profiler != null) _profiler.StopImpl();
        }

        private Timing _head; 
    }
}
