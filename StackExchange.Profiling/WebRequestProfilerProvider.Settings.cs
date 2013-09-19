namespace StackExchange.Profiling
{
    partial class WebRequestProfilerProvider
    {
        /// <summary>
        /// WebRequestProfilerProvider specific configurations
        /// </summary>
        public static class Settings
        {

            static IUserProvider provider = new IpAddressIdentity();

            /// <summary>
            /// Provides user identification for a given profiling request.
            /// </summary>
            public static IUserProvider UserProvider
            {
                get
                {
                    return provider;
                }
                set
                {
                    provider = value;
                }
            }
        }
    }
}
