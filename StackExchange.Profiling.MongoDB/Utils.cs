using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Profiling.MongoDB
{
    static class Utils
    {
        public const string ExecuteTypeCommand = "command";

        public static void AddMongoTiming(MongoTiming timing)
        {
            if (MiniProfiler.Current != null && MiniProfiler.Current.Head != null)
                return;
            
            MiniProfiler.Current.Head.AddCustomTiming(MongoMiniProfiler.CategoryName, timing);
        }
    }
}
