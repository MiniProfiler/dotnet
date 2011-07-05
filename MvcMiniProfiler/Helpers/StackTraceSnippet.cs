using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MvcMiniProfiler.Helpers
{
	/// <summary>
	/// Gets part of a stack trace containing only methods we care about.
	/// </summary>
	public class StackTraceSnippet
	{
		private const string AspNetEntryPointMethodName = "System.Web.HttpApplication.IExecutionStep.Execute";

		/// <summary>
		/// Gets the current formatted and filted stack trace.
		/// </summary>
		/// <returns>Space separated list of methods</returns>
		public static string Get()
		{
			var frames = new StackTrace(true).GetFrames();
			if (frames == null)
			{
				return "";
			}

			var methods = new Stack<string>();

			foreach (StackFrame t in frames)
			{
				var method = t.GetMethod();

				// no need to continue up the chain
				if (method.Name == AspNetEntryPointMethodName)
					break;

				var assembly = method.Module.Assembly.GetName().Name;
				if (!MiniProfiler.Settings.AssembliesToExclude.Contains(assembly) &&
					!ShouldExcludeType(method) &&
					!MiniProfiler.Settings.MethodsToExclude.Contains(method.Name))
				{
					methods.Push(method.Name);
				}
			}

			var result = string.Join(" ", methods);

			if (result.Length > MiniProfiler.Settings.StackMaxLength)
				result = result.Substring(result.Length - MiniProfiler.Settings.StackMaxLength);

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
