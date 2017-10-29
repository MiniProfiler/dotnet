using StackExchange.Profiling.Data;
using StackExchange.Profiling.Internal;
using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Extension methods for the MiniProfiler.EntityFrameworkCore.
    /// </summary>
    public static class MiniProfilerBaseOptionsExtensions
    {
        /// <summary>
        /// Adds Entity Framework Core profiling for MiniProfiler via DiagnosticListener.
        /// </summary>
        /// <typeparam name="T">The specific options type to chain with.</typeparam>
        /// <param name="options">The <see cref="MiniProfilerBaseOptions" /> to register on (just for chaining).</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static T AddEntityFramework<T>(this T options) where T : MiniProfilerBaseOptions
        {
            var initializer = new DiagnosticInitializer(new[] { new RelationalDiagnosticListener() });
            initializer.Start();

            return options;
        }
    }
}
