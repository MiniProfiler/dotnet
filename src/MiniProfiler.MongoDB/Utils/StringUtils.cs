using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling.MongoDB.Utils
{
    static class StringUtils
    {
        public static string Truncate(string text, int maxChars)
        {
            if (text.Length <= maxChars)
                return text;

            return text.Substring(0, maxChars - 3) + "...";
        }
    }
}
