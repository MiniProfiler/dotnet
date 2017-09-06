using System;
using System.Collections.Generic;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Identifies a MiniProfiler result and only contains the needed info for sorting a list of profiling sessions.
    /// </summary>
    /// <remarks>SortedList on uses the comparer for both key lookups and insertion</remarks>
    public class ProfilerSortedKey : IComparable<ProfilerSortedKey>
    {
        /// <summary>
        /// Profiler Id
        /// </summary>
        public Guid Id { get; }
        /// <summary>
        /// Profiler start date
        /// </summary>
        public DateTime Started { get; }

        /// <summary>
        /// Creates a key to use in a <see cref="SortedList{ProfilerSortedKey, T}"/>.
        /// </summary>
        /// <param name="profiler"></param>
        public ProfilerSortedKey(MiniProfiler profiler)
        {
            Id = profiler.Id;
            Started = profiler.Started;
        }

        /// <summary>
        /// Compares this <see cref="ProfilerSortedKey"/> to another.
        /// </summary>
        /// <param name="other">The <see cref="ProfilerSortedKey"/> to compare</param>
        /// <returns></returns>
        public int CompareTo(ProfilerSortedKey other)
        {
            var comp = Started.CompareTo(other.Started);
            if (comp == 0) comp = Id.CompareTo(other.Id);
            return comp;
        }
    }

    /// <summary>
    /// Helpers for <see cref="ProfilerSortedKey"/>
    /// </summary>
    public static class ProfilerSortedKeyExtensions
    {
        /// <summary>
        /// Perform a binary search of the given SortedList.
        /// </summary>
        /// <typeparam name="T">SortedList value type.</typeparam>
        /// <param name="list">List to search.</param>
        /// <param name="date">The date to find the index of.</param>
        /// <returns>The index of the nearest occurrence.</returns>
        public static int BinaryClosestSearch<T>(this SortedList<ProfilerSortedKey, T> list, DateTime date)
        {
            int lower = 0;
            int upper = list.Count - 1;

            while (lower <= upper)
            {
                int adjustedIndex = lower + ((upper - lower) >> 1);
                int comparison = list.Keys[adjustedIndex].Started.CompareTo(date);
                if (comparison == 0)
                {
                    return adjustedIndex;
                }
                if (comparison < 0)
                {
                    lower = adjustedIndex + 1;
                }
                else
                {
                    upper = adjustedIndex - 1;
                }
            }
            return lower;
        }
    }
}
