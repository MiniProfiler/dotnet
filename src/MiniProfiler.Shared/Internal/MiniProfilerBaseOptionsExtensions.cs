using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal extension methods for <see cref="MiniProfilerBaseOptions"/> and inheritors.
    /// </summary>
    public static class MiniProfilerBaseOptionsExtensions
    {
        /// <summary>
        /// Synchronously gets unviewed profiles for the user, 
        /// expiring any above the <see cref="MiniProfilerBaseOptions.MaxUnviewedProfiles"/> count.
        /// </summary>
        /// <param name="options">The options to operate against on.</param>
        /// <param name="user">The user to get profiler IDs for.</param>
        /// <returns>The list of IDs</returns>
        public static List<Guid> ExpireAndGetUnviewed(this MiniProfilerBaseOptions options, string user)
        {
            var ids = options.Storage?.GetUnviewedIds(user);
            if (ids?.Count > options.MaxUnviewedProfiles)
            {
                for (var i = 0; i < ids.Count - options.MaxUnviewedProfiles; i++)
                {
                    options.Storage.SetViewedAsync(user, ids[i]);
                }
            }
            return ids;
        }

        /// <summary>
        /// Asynchronously gets unviewed profiles for the user, 
        /// expiring any above the <see cref="MiniProfilerBaseOptions.MaxUnviewedProfiles"/> count.
        /// </summary>
        /// <param name="options">The options to operate against on.</param>
        /// <param name="user">The user to get profiler IDs for.</param>
        /// <returns>The list of IDs</returns>
        public static async Task<List<Guid>> ExpireAndGetUnviewedAsync(this MiniProfilerBaseOptions options, string user)
        {
            if (options.Storage == null)
            {
                return null;
            }
            var ids = await options.Storage.GetUnviewedIdsAsync(user).ConfigureAwait(false);
            if (ids?.Count > options.MaxUnviewedProfiles)
            {
                for (var i = 0; i < ids.Count - options.MaxUnviewedProfiles; i++)
                {
                    await options.Storage.SetViewedAsync(user, ids[i]).ConfigureAwait(false);
                }
            }
            return ids;
        }
    }
}
