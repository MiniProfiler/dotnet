using System.ComponentModel;

namespace StackExchange.Profiling.MongoDB
{
    public  partial class MongoMiniProfiler
    {
        /// <summary>
        /// Various configuration properties.
        /// </summary>
        public static class Settings
        {
            static Settings()
            {
                MaxServerCount = 100;
            }

            /// <summary>
            /// Maximum number of ProfiledeMongoServer instance to create, defaults to 100.
            /// </summary>
            [DefaultValue(100)]
            public static int MaxServerCount { get; set; }
        }
    }
}
