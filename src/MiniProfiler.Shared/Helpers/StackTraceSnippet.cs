using StackExchange.Profiling.Internal;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Gets part of a stack trace containing only methods we care about.
    /// </summary>
    public static class StackTraceSnippet
    {
        /// <summary>
        /// Gets the current formatted and filtered stack trace.
        /// </summary>
        /// <returns>Space separated list of methods</returns>
        public static string Get()
        {
#if !NETSTANDARD1_5
            var frames = new StackTrace().GetFrames();
#else // The above works in netstandard2.0 via https://github.com/dotnet/corefx/pull/12527
            StackFrame[] frames = null;
#endif
            if (frames == null || MiniProfiler.Settings.StackMaxLength <= 0)
            {
                return string.Empty;
            }

            var sb = StringBuilderCache.Get();
            int stackLength = 0,
                startFrame = frames.Length - 1;

            for (int i = 0; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                if (stackLength >= MiniProfiler.Settings.StackMaxLength
                    // ASP.NET: no need to continue up the chain
                    || method.Name == "System.Web.HttpApplication.IExecutionStep.Execute"
                    || (method.Module.Name == "Microsoft.AspNetCore.Mvc.Core.dll" && method.DeclaringType.Name == "ObjectMethodExecutor"))
                {
                    frames[i] = null;
                    startFrame = i < 0 ? 0 : i - 1;
                    break;
                }
                else if (ShouldExcludeType(method)
                    || MiniProfiler.Settings.AssembliesToExclude.Contains(method.Module.Assembly.GetName().Name)
                    || MiniProfiler.Settings.MethodsToExclude.Contains(method.Name))
                {
                    frames[i] = null;
                }
                else
                {
                    stackLength += (stackLength > 0 ? 3 : 0) + method.Name.Length;
                }
            }

            for (var i = startFrame; i >= 0; i--)
            {
                var f = frames[i];
                if (f != null)
                {
                    var method = f.GetMethod();
                    if (sb.Length > 0)
                    {
                        sb.Append(" > ");
                    }
                    sb.Append(method.Name);
                }
            }

            return sb.ToStringRecycle();
        }

        private static bool ShouldExcludeType(MethodBase method)
        {
            var t = method.DeclaringType;

            while (t != null)
            {
                if (MiniProfiler.Settings.TypesToExclude.Contains(t.Name))
                    return true;

                t = t.DeclaringType;
            }

            return false;
        }
    }
}