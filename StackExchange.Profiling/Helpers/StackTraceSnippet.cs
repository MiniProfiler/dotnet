using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
	/// Gets part of a stack trace containing only methods we care about.
	/// </summary>
	public class StackTraceSnippet
	{
        private const string AspNetEntryPointMethodName = "System.Web.HttpApplication.IExecutionStep.Execute";

		/// <summary>
		/// Gets the current formatted and filtered stack trace.
		/// </summary>
		/// <returns>Space separated list of methods</returns>
		public static string Get()
		{
			var frames = new StackTrace().GetFrames();
			if (frames == null)
			{
				return "";
			}

			var methods = new List<string>();

            // TODO: short circuit here by keeping a sum of method name chars and checking against Settings.StackMaxLength
			foreach (StackFrame t in frames)
			{
				var method = t.GetMethod();

				// no need to continue up the chain
				if (method.Name == AspNetEntryPointMethodName)
					break;

				var assembly = method.Module.Assembly.GetName().Name;
				if (!ShouldExcludeType(method) 
                    && !MiniProfiler.Settings.AssembliesToExclude.Contains(assembly) 
                    && !MiniProfiler.Settings.MethodsToExclude.Contains(method.Name))
				{
					methods.Add(method.Name);
				}
			}

			var result = string.Join(" ", methods);

            if (result.Length > MiniProfiler.Settings.StackMaxLength)
            {
                var index = result.IndexOf(" ", MiniProfiler.Settings.StackMaxLength, StringComparison.Ordinal);		
	            if (index >= MiniProfiler.Settings.StackMaxLength)		
	            {		
	                result = result.Substring(0, index);		
	            }		
	        }		

			return result;
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
