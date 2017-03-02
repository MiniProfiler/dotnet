namespace StackExchange.Profiling
{
    public partial class MiniProfiler
    {
        /// <summary>
        /// Used downstream to store request state on this MiniProfiler
        /// We want to keep this here rather than switching the ref for more items in AsyncLocal{T}, I think.
        /// Let's be honest, this sucks. But doing work twice on requests is even worse.
        /// </summary>
        internal object RequestState { get; set; }
    }
}
