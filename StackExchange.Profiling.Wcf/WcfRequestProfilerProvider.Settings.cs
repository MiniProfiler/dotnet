namespace StackExchange.Profiling.Wcf
{
    using StackExchange.Profiling.Wcf.Storage;

    /// <summary>
    /// The WCF request profiler provider.
    /// </summary>
    public partial class WcfRequestProfilerProvider
    {
        /// <summary>
        /// The settings.
        /// </summary>
        public static class Settings
        {
            /// <summary>
            /// Gets or sets the user provider.
            /// </summary>
            public static IWcfUserProvider UserProvider { get; set; }

            /// <summary>
            /// ensure the storage strategy.
            /// </summary>
            internal static void EnsureStorageStrategy()
            {
                if (MiniProfiler.Settings.Storage == null)
                    MiniProfiler.Settings.Storage = new WcfRequestInstanceStorage();
            }
        }
    }
}
