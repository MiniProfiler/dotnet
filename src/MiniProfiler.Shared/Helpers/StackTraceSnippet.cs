using StackExchange.Profiling.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Gets part of a stack trace containing only methods we care about.
    /// </summary>
    public static class StackTraceSnippet
    {
        private static readonly ConcurrentDictionary<Assembly, string> AssemblyNames = new();
        private static readonly ConcurrentDictionary<Module, string> ModuleNames = new();

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
                    || (ModuleNames.GetOrAdd(method.Module, m => m.Name) == "Microsoft.AspNetCore.Mvc.Core.dll" && method.DeclaringType.Name == "ObjectMethodExecutor"))
                {
                    frames[i] = null;
                    startFrame = i < 0 ? 0 : i - 1;
                    break;
                }
                else if (ShouldExcludeType(method)
                    || options.ExcludedAssemblies.Contains(AssemblyNames.GetOrAdd(method.Module.Assembly, a => a.GetName().Name))
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

    /// <summary>
    /// StackTrace utilities, from Exceptional
    /// </summary>
    /// <remarks>
    /// ...need to make this a source package...
    /// </remarks>
    internal static class StackTraceUtils
    {
        // Inspired by StackTraceParser by Atif Aziz, project home: https://github.com/atifaziz/StackTraceParser
        internal const string Space = @"[\x20\t]",
                              NoSpace = @"[^\x20\t]";
        private static class Groups
        {
            public const string LeadIn = nameof(LeadIn);
            public const string Frame = nameof(Frame);
            public const string Type = nameof(Type);
            public const string AsyncMethod = nameof(AsyncMethod);
            public const string Method = nameof(Method);
            public const string Params = nameof(Params);
            public const string ParamType = nameof(ParamType);
            public const string ParamName = nameof(ParamName);
            public const string SourceInfo = nameof(SourceInfo);
            public const string Path = nameof(Path);
            public const string LinePrefix = nameof(LinePrefix);
            public const string Line = nameof(Line);
        }

        private static readonly char[] NewLine_CarriageReturn = { '\n', '\r' };

        private const string EndStack = "--- End of stack trace from previous location where exception was thrown ---";

        // TODO: Patterns, or a bunch of these...
        private static readonly HashSet<string> _asyncFrames = new()
            {
                // 3.1 Stacks
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(Exception exception)",
                "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1.SetException(Exception exception)",
                "System.Threading.Tasks.AwaitTaskContinuation.RunOrScheduleAction(IAsyncStateMachineBox box, Boolean allowInlining)",
                "System.Threading.Tasks.Task.FinishSlow(Boolean userDelegateExecute)",
                "System.Threading.Tasks.Task.TrySetException(Object exceptionObject)",

                // 3.0 Stacks
                "System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)",
                "System.Threading.Tasks.Task.RunContinuations(Object continuationObject)",
                "System.Threading.Tasks.Task`1.TrySetResult(TResult result)",
                "System.Threading.Tasks.AwaitTaskContinuation.RunOrScheduleAction(Action action, Boolean allowInlining)",
                "System.Threading.Tasks.Task.CancellationCleanupLogic()",
                "System.Threading.Tasks.Task.TrySetCanceled(CancellationToken tokenToRecord, Object cancellationException)",
                "System.Threading.Tasks.Task.FinishContinuations()",

                "System.Runtime.CompilerServices.AsyncMethodBuilderCore.ContinuationWrapper.Invoke()",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.SetExistingTaskResult(TResult result)",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.AsyncStateMachineBox`1.ExecutionContextCallback(Object s)",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.AsyncStateMachineBox`1.MoveNext(Thread threadPoolThread)",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.AsyncStateMachineBox`1.MoveNext()",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.SetException(Exception exception)",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.SetResult(TResult result)",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()",
                "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder`1.SetResult(TResult result)",
                "System.Runtime.CompilerServices.TaskAwaiter.<>c.<OutputWaitEtwEvents>b__12_0(Action innerContinuation, Task innerTask)",

                // < .NET Core 3.0 stacks
                "System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()",
                "System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)",
                "System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)",
                "System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task)",
                "System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()",
                "System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter.GetResult()",
                "System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1.ConfiguredTaskAwaiter.GetResult()",
                "System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)",
                "System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()",
                "Microsoft.Extensions.Internal.ObjectMethodExecutorAwaitable.Awaiter.GetResult()",
                EndStack
            };

        // TODO: Adjust for URLs instead of files
        private static readonly Regex _regex = new Regex($@"
            ^(?<{Groups.LeadIn}>{Space}*\w+{Space}+)
             (?<{Groups.Frame}>
                (?<{Groups.Type}>({NoSpace}+(<(?<{Groups.AsyncMethod}>\w+)>d__[0-9]+))|{NoSpace}+)\.
                (?<{Groups.Method}>{NoSpace}+?){Space}*
                (?<{Groups.Params}>\(({Space}*\)
                                    |(?<{Groups.ParamType}>.+?){Space}+(?<{Groups.ParamName}>.+?)
                                     (,{Space}*(?<{Groups.ParamType}>.+?){Space}+(?<{Groups.ParamName}>.+?))*\))
             )
             ({Space}+
                (\w+{Space}+
					(?<{Groups.SourceInfo}>
		                (?<{Groups.Path}>([a-z]\:.+?|(\b(https?|ftp|file)://)?[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]))
		                (?<{Groups.LinePrefix}>\:\w+{Space}+)
		                (?<{Groups.Line}>[0-9]+)\p{{P}}?
		                |\[0x[0-9a-f]+\]{Space}+\w+{Space}+<(?<{Groups.Path}>[^>]+)>(?<{Groups.LinePrefix}>:)(?<{Groups.Line}>[0-9]+))
					)
             )?
            )\s*$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline
            | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace,
            TimeSpan.FromSeconds(2));

        /// <summary>
        /// Converts a stack trace to formatted HTML with styling and linkifiation.
        /// </summary>
        /// <param name="stackTrace">The stack trace to HTMLify.</param>
        /// <param name="commonStart">The frame index to start marking as common (e.g. to grey out beneath).</param>
        /// <returns>An HTML-pretty version of the stack trace.</returns>
        internal static string HtmlPrettify(string stackTrace, int? commonStart = null)
        {
            string GetBetween(Capture prev, Capture next) =>
                stackTrace.Substring(prev.Index + prev.Length, next.Index - (prev.Index + prev.Length));

            int pos = 0;
            var sb = StringBuilderCache.Get();
            var matches = _regex.Matches(stackTrace);
            for (var mi = 0; mi < matches.Count; mi++)
            {
                Match m = matches[mi];
                Group leadIn = m.Groups[Groups.LeadIn],
                      frame = m.Groups[Groups.Frame],
                      type = m.Groups[Groups.Type],
                      asyncMethod = m.Groups[Groups.AsyncMethod],
                      method = m.Groups[Groups.Method],
                      allParams = m.Groups[Groups.Params],
                      sourceInfo = m.Groups[Groups.SourceInfo],
                      path = m.Groups[Groups.Path],
                      linePrefix = m.Groups[Groups.LinePrefix],
                      line = m.Groups[Groups.Line];
                CaptureCollection paramTypes = m.Groups[Groups.ParamType].Captures,
                                  paramNames = m.Groups[Groups.ParamName].Captures;
                bool nextIsAsync = false;
                if (mi < matches.Count - 1)
                {
                    Group nextFrame = matches[mi + 1].Groups[Groups.Frame];
                    nextIsAsync = _asyncFrames.Contains(nextFrame.Value);
                }

                var isAsync = _asyncFrames.Contains(frame.Value);

                // The initial message may be above an async frame
                if (sb.Length == 0 && isAsync && leadIn.Index > pos)
                {
                    sb.Append("<span class=\"stack stack-row\">")
                      .Append("<span class=\"stack misc\">")
                      .AppendHtmlEncode(stackTrace.Substring(pos, leadIn.Index - pos).Trim(NewLine_CarriageReturn))
                      .Append("</span>")
                      .Append("</span>");
                    pos += sb.Length;
                }

                sb.Append("<span class=\"stack stack-row");
                if (isAsync)
                {
                    sb.Append(" async");
                }
                if (commonStart.HasValue && mi >= commonStart)
                {
                    sb.Append(" common");
                }
                sb.Append("\">");

                if (leadIn.Index > pos)
                {
                    var miscContent = stackTrace.Substring(pos, leadIn.Index - pos);
                    if (miscContent.Contains(EndStack))
                    {
                        // Handle end-of-stack removals and redundant multilines remaining
                        miscContent = miscContent.Replace(EndStack, "")
                                                 .Replace("\r\n\r\n", "\r\n")
                                                 .Replace("\n\n", "\n\n");
                    }

                    sb.Append("<span class=\"stack misc\">")
                      .AppendHtmlEncode(miscContent)
                      .Append("</span>");
                }
                sb.Append("<span class=\"stack leadin\">")
                  .AppendHtmlEncode(leadIn.Value)
                  .Append("</span>");

                // Check if the next line is the end of an async hand-off
                var nextEndStack = stackTrace.IndexOf(EndStack, m.Index + m.Length);
                if ((nextEndStack > -1 && nextEndStack < m.Index + m.Length + 3) || (!isAsync && nextIsAsync))
                {
                    sb.Append("<span class=\"stack async-tag\">async</span> ");
                }

                if (asyncMethod.Success)
                {
                    sb.Append("<span class=\"stack type\">")
                      .AppendGenericsHtml(GetBetween(leadIn, asyncMethod))
                      .Append("</span>")
                      .Append("<span class=\"stack method\">")
                      .AppendHtmlEncode(asyncMethod.Value)
                      .Append("</span>")
                      .Append("<span class=\"stack type\">")
                      .AppendGenericsHtml(GetBetween(asyncMethod, method));
                    sb.Append("</span>");
                }
                else
                {
                    sb.Append("<span class=\"stack type\">")
                      .AppendGenericsHtml(type.Value)
                      .Append("<span class=\"stack dot\">")
                      .AppendHtmlEncode(GetBetween(type, method)) // "."
                      .Append("</span>")
                      .Append("</span>");
                }
                sb.Append("<span class=\"stack method-section\">")
                  .Append("<span class=\"stack method\">")
                  .AppendHtmlEncode(NormalizeMethodName(method.Value))
                  .Append("</span>");

                if (paramTypes.Count > 0)
                {
                    sb.Append("<span class=\"stack parens\">")
                      .Append(GetBetween(method, paramTypes[0]))
                      .Append("</span>");
                    for (var i = 0; i < paramTypes.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append("<span class=\"stack misc\">")
                              .AppendHtmlEncode(GetBetween(paramNames[i - 1], paramTypes[i])) // ", "
                              .Append("</span>");
                        }
                        sb.Append("<span class=\"stack paramType\">")
                          .AppendGenericsHtml(paramTypes[i].Value)
                          .Append("</span>")
                          .AppendHtmlEncode(GetBetween(paramTypes[i], paramNames[i])) // " "
                          .Append("<span class=\"stack paramName\">")
                          .AppendHtmlEncode(paramNames[i].Value)
                          .Append("</span>");
                    }
                    var last = paramNames[paramTypes.Count - 1];
                    sb.Append("<span class=\"stack parens\">")
                      .AppendHtmlEncode(allParams.Value.Substring(last.Index + last.Length - allParams.Index))
                      .Append("</span>");
                }
                else
                {
                    sb.Append("<span class=\"stack parens\">")
                      .AppendHtmlEncode(allParams.Value) // "()"
                      .Append("</span>");
                }
                sb.Append("</span>"); // method-section for table layout

                if (sourceInfo.Value.HasValue())
                {
                    sb.Append("<span class=\"stack source-section\">");

                    var curPath = sourceInfo.Value;
                    if (curPath != sourceInfo.Value)
                    {
                        sb.Append("<span class=\"stack misc\">")
                          .AppendHtmlEncode(GetBetween(allParams, sourceInfo))
                          .Append("</span>")
                          .Append(curPath);
                    }
                    else if (path.Value.HasValue())
                    {
                        var subPath = GetSubPath(path.Value, type.Value);

                        sb.Append("<span class=\"stack misc\">")
                          .AppendHtmlEncode(GetBetween(allParams, path))
                          .Append("</span>")
                          .Append("<span class=\"stack path\">")
                          .AppendHtmlEncode(subPath)
                          .Append("</span>")
                          .AppendHtmlEncode(GetBetween(path, linePrefix))
                          .Append("<span class=\"stack line-prefix\">")
                          .AppendHtmlEncode(linePrefix.Value)
                          .Append("</span>")
                          .Append("<span class=\"stack line\">")
                          .AppendHtmlEncode(line.Value)
                          .Append("</span>");
                    }
                    sb.Append("</span>");
                }

                sb.Append("</span>");

                pos = frame.Index + frame.Length;
            }

            // append anything left
            sb.Append("<span class=\"stack misc\">");
            var tailLength = stackTrace.Length - pos;
            if (tailLength > 0)
            {
                sb.AppendHtmlEncode(stackTrace.Substring(pos, tailLength));
            }
            sb.Append("</span>");

            return sb.ToStringRecycle();
        }

        private static char[] Backslash { get; } = new[] { '\\' };

        private static string GetSubPath(string sourcePath, string type)
        {
            //C:\git\NickCraver\StackExchange.Exceptional\src\StackExchange.Exceptional.Shared\Utils.Test.cs
            int pathPos = 0;
            foreach (var path in sourcePath.Split(Backslash))
            {
                pathPos += (path.Length + 1);
                if (type.StartsWith(path))
                {
                    return sourcePath.Substring(pathPos);
                }
            }
            return sourcePath;
        }

        /// <summary>
        /// .NET Core changes methods so generics render as as Method[T], this normalizes it.
        /// </summary>
        private static string NormalizeMethodName(string method)
        {
            return method?.Replace("[", "<").Replace("]", ">");
        }
    }

    internal static class StackTraceExtensions
    {
        private static readonly char[] _dot = new char[] { '.' };
        private static readonly Regex _genericTypeRegex = new Regex($@"(?<BaseClass>{StackTraceUtils.NoSpace}+)`(?<ArgCount>\d+)");
        private static readonly string[] _singleT = new[] { "T" };

        private static readonly Dictionary<string, string[]> _commonGenerics = new()
        {
            ["Microsoft.CodeAnalysis.SymbolVisitor`1"] = new[] { "TResult" },
            ["Microsoft.CodeAnalysis.Diagnostics.CodeBlockStartAnalysisContext`1"] = new[] { "TLanguageKindEnum" },
            ["Microsoft.CodeAnalysis.Diagnostics.SourceTextValueProvider`1"] = new[] { "TValue" },
            ["Microsoft.CodeAnalysis.Diagnostics.SyntaxTreeValueProvider`1"] = new[] { "TValue" },
            ["Microsoft.CodeAnalysis.Semantics.OperationVisitor`2"] = new[] { "TArgument", "TResult" },
            ["System.Converter`2"] = new[] { "TInput", "TOutput" },
            ["System.EventHandler`1"] = new[] { "TEventArgs" },
            ["System.Func`1"] = new[] { "TResult" },
            ["System.Func`2"] = new[] { "T", "TResult" },
            ["System.Func`3"] = new[] { "T1", "T2", "TResult" },
            ["System.Func`4"] = new[] { "T1", "T2", "T3", "TResult" },
            ["System.Func`5"] = new[] { "T1", "T2", "T3", "T4", "TResult" },
            ["System.Func`6"] = new[] { "T1", "T2", "T3", "T4", "T5", "TResult" },
            ["System.Func`7"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "TResult" },
            ["System.Func`8"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "TResult" },
            ["System.Func`9"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "TResult" },
            ["System.Func`10"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "TResult" },
            ["System.Func`11"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "TResult" },
            ["System.Func`12"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "TResult" },
            ["System.Func`13"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12", "TResult" },
            ["System.Func`14"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12", "T13", "TResult" },
            ["System.Func`15"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12", "T13", "T14", "TResult" },
            ["System.Func`16"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12", "T13", "T14", "T15", "TResult" },
            ["System.Func`17"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12", "T13", "T14", "T15", "T16", "TWhatTheHellAreYouDoing" },
            ["System.Tuple`8"] = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "TRest" },
            ["System.Collections.Concurrent.ConcurrentDictionary`2"] = new[] { "TKey", "TValue" },
            ["System.Collections.Concurrent.OrderablePartitioner`1"] = new[] { "TSource" },
            ["System.Collections.Concurrent.Partitioner`1"] = new[] { "TSource" },
            ["System.Collections.Generic.Dictionary`2"] = new[] { "TKey", "TValue" },
            ["System.Collections.Generic.SortedDictionary`2"] = new[] { "TKey", "TValue" },
            ["System.Collections.Generic.SortedList`2"] = new[] { "TKey", "TValue" },
            ["System.Collections.Immutable.ImmutableDictionary`2"] = new[] { "TKey", "TValue" },
            ["System.Collections.Immutable.ImmutableSortedDictionary`2"] = new[] { "TKey", "TValue" },
            ["System.Collections.ObjectModel.KeyedCollection`2"] = new[] { "TKey", "TItem" },
            ["System.Collections.ObjectModel.ReadOnlyDictionary`2"] = new[] { "TKey", "TValue" },
            ["System.Data.Common.CommandTrees.DbExpressionVisitor`1"] = new[] { "TResultType" },
            ["System.Data.Linq.EntitySet`1"] = new[] { "TEntity" },
            ["System.Data.Linq.Table`1"] = new[] { "TEntity" },
            ["System.Data.Linq.Mapping.MetaAccessor`2"] = new[] { "TEntity", "TMember" },
            ["System.Data.Linq.SqlClient.Implementation.ObjectMaterializer`1"] = new[] { "TDataReader" },
            ["System.Data.Objects.ObjectSet`1"] = new[] { "TEntity" },
            ["System.Data.Objects.DataClasses.EntityCollection`1"] = new[] { "TEntity" },
            ["System.Data.Objects.DataClasses.EntityReference`1"] = new[] { "TEntity" },
            ["System.Linq.Lookup`2"] = new[] { "TKey", "TElement" },
            ["System.Linq.OrderedParallelQuery`1"] = new[] { "TSource" },
            ["System.Linq.ParallelQuery`1"] = new[] { "TSource" },
            ["System.Linq.Expressions.Expression`1"] = new[] { "TDelegate" },
            ["System.Runtime.CompilerServices.ConditionalWeakTable`2"] = new[] { "TKey", "TValue" },
            ["System.Threading.Tasks.Task`1"] = new[] { "TResult" },
            ["System.Threading.Tasks.TaskCompletionSource`1"] = new[] { "TResult" },
            ["System.Threading.Tasks.TaskFactory`1"] = new[] { "TResult" },
            ["System.Web.ModelBinding.ArrayModelBinder`1"] = new[] { "TElement" },
            ["System.Web.ModelBinding.CollectionModelBinder`1"] = new[] { "TElement" },
            ["System.Web.ModelBinding.DataAnnotationsModelValidator`1"] = new[] { "TAttribute" },
            ["System.Web.ModelBinding.DictionaryModelBinder`2"] = new[] { "TKey", "TValue" },
            ["System.Web.ModelBinding.DictionaryValueProvider`1"] = new[] { "TValue" },
            ["System.Web.ModelBinding.KeyValuePairModelBinder`2"] = new[] { "TKey", "TValue" },
            ["System.Windows.WeakEventManager`2"] = new[] { "TEventSource", "TEventArgs" },
            ["System.Windows.Documents.TextElementCollection`1"] = new[] { "TextElementType" },
            ["System.Windows.Threading.DispatcherOperation`1"] = new[] { "TResult" },
            ["System.Xaml.Schema.XamlValueConverter`1"] = new[] { "TConverterBase" },
        };

        internal static StringBuilder AppendHtmlEncode(this StringBuilder sb, string s) => sb.Append(WebUtility.HtmlEncode(s));

        internal static StringBuilder AppendGenericsHtml(this StringBuilder sb, string typeOrMethod)
        {
            const string _dotSpan = "<span class=\"stack dot\">.</span>";
            // Check the common framework list above
            _commonGenerics.TryGetValue(typeOrMethod, out string[] args);

            // Break each type down by namespace and class (remember, we *could* have nested generic classes)
            var classes = typeOrMethod.Split(_dot);
            // Loop through each dot component of the type, e.g. "System", "Collections", "Generics"
            for (var i = 0; i < classes.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(_dotSpan);
                }
                var match = _genericTypeRegex.Match(classes[i]);
                if (match.Success)
                {
                    // If arguments aren't known, get the defaults
                    if (args == null && int.TryParse(match.Groups["ArgCount"].Value, out int count))
                    {
                        if (count == 1)
                        {
                            args = _singleT;
                        }
                        else
                        {
                            args = new string[count];
                            for (var j = 0; j < count; j++)
                            {
                                args[j] = "T" + (j + 1).ToString(); // <T>, or <T1, T2, T3>
                            }
                        }
                    }
                    // In the known case, BaseClass is "System.Collections.Generic.Dictionary"
                    // In the unknown case, we're hitting here at "Class" only
                    sb.AppendHtmlEncode(match.Groups["BaseClass"].Value);
                    AppendArgs(args);
                }
                else
                {
                    sb.AppendHtmlEncode(classes[i]);
                }
            }
            return sb;

            void AppendArgs(string[] tArgs)
            {
                sb.Append("&lt;");
                // Don't put crazy amounts of arguments in here
                if (tArgs.Length > 5)
                {
                    sb.Append("<span class=\"stack generic-type\">").Append(tArgs[0]).Append("</span>")
                      .Append(",")
                      .Append("<span class=\"stack generic-type\">").Append(tArgs[1]).Append("</span>")
                      .Append(",")
                      .Append("<span class=\"stack generic-type\">").Append(tArgs[2]).Append("</span>")
                      .Append("…")
                      .Append("<span class=\"stack generic-type\">").Append(tArgs[tArgs.Length - 1]).Append("</span>");
                }
                else
                {
                    for (int i = 0; i < tArgs.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(",");
                        }
                        sb.Append("<span class=\"stack generic-type\">");
                        sb.Append(tArgs[i])
                            .Append("</span>");
                    }
                }
                sb.Append("&gt;");
            }
        }
    }
}
