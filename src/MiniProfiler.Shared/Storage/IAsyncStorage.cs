﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// How lists should be sorted.
    /// </summary>
    public enum ListResultsOrder
    {
        /// <summary>
        /// Ascending Order
        /// </summary>
        Ascending = 0,

        /// <summary>
        /// Descending Order
        /// </summary>
        Descending = 1
    }

    /// <summary>
    /// Provides saving and loading <see cref="MiniProfiler"/>s to a storage medium.
    /// </summary>
    public interface IAsyncStorage
    {
        /// <summary>
        /// List the latest profiling results.
        /// </summary>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="start">(Optional) The start of the date range to fetch.</param>
        /// <param name="finish">(Optional) The end of the date range to fetch.</param>
        /// <param name="orderBy">(Optional) The order to fetch profiler IDs in.</param>
        IEnumerable<Guid> List(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending);

        /// <summary>
        /// Stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being unviewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        void Save(MiniProfiler profiler);

        /// <summary>
        /// Returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>,
        /// which should map to <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its
        /// profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        MiniProfiler Load(Guid id);

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        void SetUnviewed(string user, Guid id);

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        void SetViewed(string user, Guid id);

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfilerOptions.UserProvider</c></param>
        List<Guid> GetUnviewedIds(string user);

        /// <summary>
        /// Asynchronously list the latest profiling results.
        /// </summary>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="start">(Optional) The start of the date range to fetch.</param>
        /// <param name="finish">(Optional) The end of the date range to fetch.</param>
        /// <param name="orderBy">(Optional) The order to fetch profiler IDs in.</param>
        Task<IEnumerable<Guid>> ListAsync(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending);

        /// <summary>
        /// Asynchronously stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being unviewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        Task SaveAsync(MiniProfiler profiler);

        /// <summary>
        /// Asynchronously returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>,
        /// which should map to <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its
        /// profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        Task<MiniProfiler> LoadAsync(Guid id);

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "unviewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        Task SetUnviewedAsync(string user, Guid id);

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        Task SetViewedAsync(string user, Guid id);

        /// <summary>
        /// Asynchronously returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfilerOptions.UserProvider</c></param>
        Task<List<Guid>> GetUnviewedIdsAsync(string user);
    }

    /// <summary>
    /// Extension methods for <see cref="IAsyncStorage"/>.
    /// </summary>
    public static class AsyncStorageExtensions
    {
        /// <summary>
        /// Sets a specific <see cref="MiniProfiler"/> to "unviewed".
        /// </summary>
        /// <param name="storage">The <see cref="IAsyncStorage"/> provider.</param>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to set to "unviewed".</param>
        public static void SetUnviewed(this IAsyncStorage storage, MiniProfiler profiler) => storage.SetUnviewed(profiler.User, profiler.Id);

        /// <summary>
        /// Asynchronously sets a specific <see cref="MiniProfiler"/> to "unviewed".
        /// </summary>
        /// <param name="storage">The <see cref="IAsyncStorage"/> provider.</param>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to set to "unviewed".</param>
        public static Task SetUnviewedAsync(this IAsyncStorage storage, MiniProfiler profiler) => storage.SetUnviewedAsync(profiler.User, profiler.Id);

        /// <summary>
        /// Sets a specific <see cref="MiniProfiler"/> to "viewed".
        /// </summary>
        /// <param name="storage">The <see cref="IAsyncStorage"/> provider.</param>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to set to "viewed".</param>
        public static void SetViewed(this IAsyncStorage storage, MiniProfiler profiler) => storage.SetViewed(profiler.User, profiler.Id);

        /// <summary>
        /// Asynchronously sets a specific <see cref="MiniProfiler"/> to "viewed".
        /// </summary>
        /// <param name="storage">The <see cref="IAsyncStorage"/> provider.</param>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to set to "viewed".</param>
        public static Task SetViewedAsync(this IAsyncStorage storage, MiniProfiler profiler) => storage.SetViewedAsync(profiler.User, profiler.Id);
    }
}
