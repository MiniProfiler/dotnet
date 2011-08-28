using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcMiniProfiler.Wcf.Storage;

namespace MvcMiniProfiler.Wcf
{
    partial class WcfRequestProfilerProvider
    {
        public static class Settings
        {
            public static IWcfUserProvider UserProvider { get; set; }

            internal static void EnsureStorageStrategy()
            {
                if (MvcMiniProfiler.MiniProfiler.Settings.Storage == null)
                {
                    MvcMiniProfiler.MiniProfiler.Settings.Storage = new WcfRequestInstanceStorage();
                }
            }
        }
    }
}
