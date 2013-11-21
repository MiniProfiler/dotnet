using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Profiling.RavenDb
{
    internal static class ExtensionMethods
    {
        internal static string Truncate(this string s, int maxLength)
        {
            return s != null && s.Length > maxLength ? s.Substring(0, maxLength) : s;
        }
    }
}
