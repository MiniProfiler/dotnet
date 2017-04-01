namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// An interface for configuring MiniProfiler services.
    /// </summary>
    public interface IMiniProfilerBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where MiniProfiler services are configured.
        /// </summary>
        IServiceCollection Services { get; }
    }
}