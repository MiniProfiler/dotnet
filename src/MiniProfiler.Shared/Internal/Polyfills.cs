using System.Threading.Tasks;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal MiniProfiler extensions, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// </summary>
    public static class Polyfills
    {
        /// <summary>
        /// Task.Completed task, or Task.FromResult(false), cached for reuse.
        /// </summary>
#if NET45
        public static Task CompletedTask = Task.FromResult(false);
#else
        public static Task CompletedTask => Task.CompletedTask;
#endif
    }
}
