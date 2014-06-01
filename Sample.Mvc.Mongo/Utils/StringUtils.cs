using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SampleWeb.Utils
{
    public static class StringUtils
    {
        public static string Truncate(this string source, int maxChars)
        {
            return source.Substring(0, Math.Min(source.Length, maxChars));
        }
    }
}