using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage;

/// <summary>
/// Provides saving and loading <see cref="MiniProfiler"/>s to a storage medium with some advanced operations.
/// </summary>
public interface IAdvancedAsyncStorage : IAsyncStorage
{
    /// <summary>
    /// Asynchronously sets the provided profiler sessions to "viewed"
    /// </summary>
    /// <param name="user">The user to set this profiler ID as viewed for.</param>
    /// <param name="ids">The profiler IDs to set viewed.</param>
    Task SetViewedAsync(string user, IEnumerable<Guid> ids);
}
