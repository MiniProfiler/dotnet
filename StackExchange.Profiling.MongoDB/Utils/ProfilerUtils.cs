namespace StackExchange.Profiling.MongoDB.Utils
{
    static class ProfilerUtils
    {
        public const string ExecuteTypeCommand = "command";

        public static void AddMongoTiming(string commandString, long durationMilliseconds, ExecuteType executeType)
        {
            if (MiniProfiler.Current == null || MiniProfiler.Current.Head == null)
                return;

            MiniProfiler.Current.Head.AddCustomTiming(MongoMiniProfiler.CategoryName,
                new CustomTiming(MiniProfiler.Current, commandString)
                {
                    DurationMilliseconds = durationMilliseconds,
                    FirstFetchDurationMilliseconds = durationMilliseconds,
                    ExecuteType = executeType.ToString().ToLower()
                });
        }
    }
}
