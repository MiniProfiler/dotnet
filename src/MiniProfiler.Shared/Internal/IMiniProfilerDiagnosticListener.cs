#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
#endif

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal MiniProfiler interface for registering DiagnosticListeners, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// </summary>
    public interface IMiniProfilerDiagnosticListener
#if NETSTANDARD2_0
        : IObserver<KeyValuePair<string, object>>
#endif
    {
        /// <summary>
        /// Gets a value indicating which listener this instance should be subscribed to
        /// </summary>
        string ListenerName { get; }
    }
}
