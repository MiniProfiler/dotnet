using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Profiling.MongoDB
{
    /// <summary>
    /// Profiles a MongoDB command
    /// </summary>
    public class MongoTiming : CustomTiming
    {
        public MongoTiming(MiniProfiler profiler, string commandString)
            : base(profiler, null)
        {
            if (profiler == null) throw new ArgumentNullException("profiler");

            CommandString = commandString;
        }
    }
}
