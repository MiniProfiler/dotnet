namespace StackExchange.Profiling.MongoDB.Utils
{
    static class ProfilerUtils
    {
        public const string ExecuteTypeCommand = "command";

        public static void AddMongoTiming(MongoTiming timing)
        {
            if (MiniProfiler.Current == null || MiniProfiler.Current.Head == null)
                return;

            MiniProfiler.Current.Head.AddCustomTiming(MongoMiniProfiler.CategoryName, timing);
        }

        public static void AddMongoTiming(string commandString, long durationMilliseconds, ExecuteType executeType)
        {
            AddMongoTiming(
                new MongoTiming(MiniProfiler.Current, commandString)
                {
                    DurationMilliseconds = durationMilliseconds,
                    FirstFetchDurationMilliseconds = durationMilliseconds,
                    ExecuteType = executeType.ToString().ToLower()
                });
        }
    }
}
