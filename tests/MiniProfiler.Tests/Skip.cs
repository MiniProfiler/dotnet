using System;
using System.Runtime.InteropServices;
using Xunit;

namespace StackExchange.Profiling.Tests
{
    public static class Skip
    {
        public static void Inconclusive(string message) => Assert.Skip(message);

        public static void IfNoConfig(string prop, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Assert.Skip($"Config.{prop} is not set, skipping test.");
            }
        }

        public static void IfNotWindows()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Skip("Only runnable on Windows");
            }
        }

        public static void IfNotLongRunning()
        {
            if (!TestConfig.Current.RunLongRunning)
            {
                Assert.Skip("Config.RunLongRunning is false - skipping long test.");
            }
        }
    }
}
