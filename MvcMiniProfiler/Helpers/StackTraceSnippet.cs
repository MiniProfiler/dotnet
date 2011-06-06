using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StackExchange.MvcMiniProfiler.Data;
using System.Web.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace StackExchange.MvcMiniProfiler.Helpers
{
    /// <summary>
    /// Gets part of a stack trace containing only methods we care about.
    /// </summary>
    internal class StackTraceSnippet
    {
        public static readonly HashSet<string> AssembliesToExclude = new HashSet<string>
        {
            // our assembly
            "MiniProfiler",

            // reflection emit
            "Anonymously Hosted DynamicMethods Assembly",

            // the man
            "System.Core",
            "System.Data",
            "System.Data.Linq",
            "System.Web",
            "System.Web.Mvc",
        };

        /// <summary>
        /// Contains the default list of full type names we don't want in any stack trace snippets.
        /// </summary>
        public static readonly HashSet<string> TypesToExclude = new HashSet<string>
        {
            // while we like our friend, we don't want to see him all the time
            "Dapper.SqlMapper",
        };

        public static readonly HashSet<string> MethodsToExclude = new HashSet<string>
        {
            "lambda_method",
            ".ctor",
        };

        public static string Get()
        {
            var frames = new StackTrace(true).GetFrames();
            var methods = new Stack<string>();

            for (int i = 0; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                var assembly = method.Module.Assembly.FullName;

                // remove version info.. we just want the .dll name
                assembly = assembly.Remove(assembly.IndexOf(","));

                // no need to continue up the chain
                if (method.Name == "System.Web.HttpApplication.IExecutionStep.Execute")
                    break;

                if (!AssembliesToExclude.Contains(assembly) &&
                    !ShouldExcludeType(method) &&
                    !MethodsToExclude.Contains(method.Name))
                {
                    methods.Push(method.Name);
                }
            }

            var result = string.Join(" ", methods);

            const int maxlen = 120;
            if (result.Length > maxlen)
                result = result.Substring(result.Length - maxlen);

            return result;
        }

        private static bool ShouldExcludeType(MethodBase method)
        {
            var t = method.DeclaringType;

            while (t != null)
            {
                if (TypesToExclude.Contains(t.FullName))
                    return true;

                t = t.DeclaringType;
            }
            return false;
        }
    }
}
