using System;
using System.Collections.Generic;

namespace StackExchange.Profiling.Tests
{
    public static class Skip
    {
        public static void Inconclusive(string message) => throw new SkipTestException(message);

        public static void IfNoConfig(string prop, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new SkipTestException($"Config.{prop} is not set, skipping test.");
            }
        }

        public static void IfNoConfig(string prop, List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                throw new SkipTestException($"Config.{prop} is not set, skipping test.");
            }
        }
    }

#pragma warning disable RCS1194 // Implement exception constructors.
    public class SkipTestException : Exception
    {
        public string MissingFeatures { get; set; }

        public SkipTestException(string reason) : base(reason) { }
    }
#pragma warning restore RCS1194 // Implement exception constructors.
}
