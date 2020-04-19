using StackExchange.Profiling.Internal;
using System.Diagnostics;
using System.Reflection;

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
        /// <param name="options">The options to use for this StackTrace fetch.</param>
        /// <returns>Space separated list of methods</returns>
        public static string Get(MiniProfilerBaseOptions options)
        {
            if (options.StackMaxLength <= 0)
            {
                return string.Empty;
            }

            bool ShouldExcludeType(MethodBase method)
            {
                var t = method.DeclaringType;
                while (t != null)
                {
                    if (options.ExcludedTypes.Contains(t.Name))
                        return true;

                    t = t.DeclaringType;
                }
                return false;
            }

            var frames = new StackTrace().GetFrames();

            if (frames == null)
            {
                return string.Empty;
            }

            var sb = StringBuilderCache.Get();
            int stackLength = 0,
                startFrame = frames.Length - 1;

            for (int i = 0; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                if (stackLength >= options.StackMaxLength
                    // ASP.NET: no need to continue up the chain
                    || method.Name == "System.Web.HttpApplication.IExecutionStep.Execute"
                    || (method.Module.Name == "Microsoft.AspNetCore.Mvc.Core.dll" && method.DeclaringType.Name == "ObjectMethodExecutor"))
                {
                    frames[i] = null;
                    startFrame = i < 0 ? 0 : i - 1;
                    break;
                }
                else if (ShouldExcludeType(method)
                    || options.ExcludedAssemblies.Contains(method.Module.Assembly.GetName().Name)
                    || options.ExcludedMethods.Contains(method.Name))
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
    }
}
