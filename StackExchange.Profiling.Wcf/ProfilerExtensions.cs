using System.Diagnostics;
using System.Linq;

namespace StackExchange.Profiling.Wcf
{
    
    /// <summary>
    /// The profiler extensions.
    /// </summary>
    internal static class ProfilerExtensions
    {
        /// <summary>
        /// We don't actually know the start milliseconds, but lets 
        /// take it as zero being the start of the current head
        /// </summary>
        public static void UpdateStartMillisecondTimingsToAbsolute(this Timing timing, decimal newStartMilliseconds)
        {
            if (timing == null)
                return;

            UpdateStartMillisecondTimingsByDelta(timing, newStartMilliseconds - timing.StartMilliseconds);
        }

        /// <summary>
        /// Delta is added to the existing StartMillisecondsValue
        /// </summary>
        public static void UpdateStartMillisecondTimingsByDelta(this Timing timing, decimal deltaMilliseconds)
        {
            if (timing == null)
                return;

            timing.StartMilliseconds += deltaMilliseconds;
            if (timing.Children != null)
            {
                foreach (var child in timing.Children)
                {
                    UpdateStartMillisecondTimingsByDelta(child, deltaMilliseconds);
                }
            }

            if (timing.CustomTimings != null)
            {
                foreach (var customTiming in timing.CustomTimings.SelectMany(pair => pair.Value))
                {
                    customTiming.StartMilliseconds += deltaMilliseconds;                    
                }
            }
        }

        /// <summary>
        /// Removes trivial items from the current profiler results
        /// </summary>
        public static void RemoveTrivialTimings(this Timing timing)
        {
            if (timing.Children != null)
            {
                // This assumes that trivial items do not have any non-trivial children
                timing.Children.RemoveAll(child => child.IsTrivial);
            }

            Debug.Assert(timing.Children != null, "timing.Children != null");
            if (timing.Children != null) timing.Children.ForEach(child => child.RemoveTrivialTimings());
        }
    }
}
