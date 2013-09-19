using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// The <c>haack</c> formatter.
    /// <c>http://haacked.com/archive/2009/01/04/fun-with-named-formats-string-parsing-and-edge-cases.aspx</c>.
    /// </summary>
    internal static class HaackFormatter
    {
        /// <summary>
        /// format the supplied string.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="source">The source.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string Format(this string format, object source)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            var formattedStrings = (from expression in SplitFormat(format)
                                    select expression.Eval(source)).ToArray();
            return string.Join(string.Empty, formattedStrings);
        }

        /// <summary>
        /// split and format the supplied string.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>the set of text expressions</returns>
        private static IEnumerable<ITextExpression> SplitFormat(string format)
        {
            int exprEndIndex = -1;
            int expStartIndex;

            do
            {
                expStartIndex = format.IndexOfExpressionStart(exprEndIndex + 1);
                if (expStartIndex < 0)
                {
                    // everything after last end brace index.
                    if (exprEndIndex + 1 < format.Length)
                    {
                        yield return new LiteralFormat(format.Substring(exprEndIndex + 1));
                    }

                    break;
                }

                if (expStartIndex - exprEndIndex - 1 > 0)
                {
                    // everything up to next start brace index
                    yield return 
                        new LiteralFormat(format.Substring(exprEndIndex + 1, expStartIndex - exprEndIndex - 1));
                }

                int endBraceIndex = format.IndexOfExpressionEnd(expStartIndex + 1);
                if (endBraceIndex < 0)
                {
                    // rest of string, no end brace (could be invalid expression)
                    yield return new FormatExpression(format.Substring(expStartIndex));
                }
                else
                {
                    exprEndIndex = endBraceIndex;

                    // everything from start to end brace.
                    yield return
                        new FormatExpression(format.Substring(expStartIndex, endBraceIndex - expStartIndex + 1));
                }
            }
            while (expStartIndex > -1);
        }

        /// <summary>
        /// index of the expression start.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>the start index</returns>
        private static int IndexOfExpressionStart(this string format, int startIndex)
        {
            int index = format.IndexOf('{', startIndex);
            if (index == -1)
            {
                return index;
            }

            // peek ahead.
            if (index + 1 < format.Length)
            {
                char nextChar = format[index + 1];
                if (nextChar == '{')
                {
                    return IndexOfExpressionStart(format, index + 2);
                }
            }

            return index;
        }

        /// <summary>
        /// index of the expression end.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>the expression end index.</returns>
        private static int IndexOfExpressionEnd(this string format, int startIndex)
        {
            int endBraceIndex = format.IndexOf('}', startIndex);
            if (endBraceIndex == -1)
            {
                return endBraceIndex;
            }

            // start peeking ahead until there are no more braces...
            // }}}}
            int braceCount = 0;
            for (int i = endBraceIndex + 1; i < format.Length; i++)
            {
                if (format[i] == '}')
                {
                    braceCount++;
                }
                else
                {
                    break;
                }
            }

            if (braceCount % 2 == 1)
            {
                return IndexOfExpressionEnd(format, endBraceIndex + braceCount + 1);
            }

            return endBraceIndex;
        }

        /// <summary>
        /// The format expression.
        /// </summary>
        public class FormatExpression : ITextExpression
        {
            /// <summary>
            /// The _invalid expression.
            /// </summary>
            private readonly bool _invalidExpression = false;

            /// <summary>
            /// Initialises a new instance of the <see cref="FormatExpression"/> class.
            /// </summary>
            /// <param name="expression">
            /// The expression.
            /// </param>
            public FormatExpression(string expression)
            {
                if (!expression.StartsWith("{") || !expression.EndsWith("}"))
                {
                    _invalidExpression = true;
                    Expression = expression;
                    return;
                }

                var expressionWithoutBraces = expression.Substring(1, expression.Length - 2);
                int colonIndex = expressionWithoutBraces.IndexOf(':');
                if (colonIndex < 0)
                {
                    Expression = expressionWithoutBraces;
                }
                else
                {
                    Expression = expressionWithoutBraces.Substring(0, colonIndex);
                    Format = expressionWithoutBraces.Substring(colonIndex + 1);
                }
            }

            /// <summary>
            /// Gets the expression.
            /// </summary>
            public string Expression
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the format.
            /// </summary>
            // ReSharper disable MemberHidesStaticFromOuterClass
            public string Format
            // ReSharper restore MemberHidesStaticFromOuterClass
            {
                get;
                private set;
            }

            /// <summary>
            /// evaluate the expression
            /// </summary>
            /// <param name="o">The o.</param>
            /// <returns>The <see cref="string"/>.</returns>
            public string Eval(object o)
            {
                if (_invalidExpression)
                {
                    throw new FormatException("Invalid expression");
                }

                try
                {
                    if (string.IsNullOrEmpty(Format))
                    {
                        return (DataBinder.Eval(o, Expression) ?? string.Empty).ToString();
                    }

                    return (DataBinder.Eval(o, Expression, "{0:" + Format + "}") ?? string.Empty).ToString();
                }
                catch (ArgumentException)
                {
                    throw new FormatException();
                }
                catch (HttpException)
                {
                    throw new FormatException();
                }
            }
        }

        /// <summary>
        /// The literal format.
        /// </summary>
        public class LiteralFormat : ITextExpression
        {
            /// <summary>
            /// Initialises a new instance of the <see cref="LiteralFormat"/> class.
            /// </summary>
            /// <param name="literalText">
            /// The literal text.
            /// </param>
            public LiteralFormat(string literalText)
            {
                LiteralText = literalText;
            }

            /// <summary>
            /// Gets the literal text.
            /// </summary>
            public string LiteralText
            {
                get;
                private set;
            }

            /// <summary>
            /// evaluate the object
            /// </summary>
            /// <param name="o">The object.</param>
            /// <returns>The <see cref="string"/>.</returns>
            public string Eval(object o)
            {
                string literalText = LiteralText
                    .Replace("{{", "{")
                    .Replace("}}", "}");
                return literalText;
            }
        }

        /// <summary>
        /// The TextExpression interface.
        /// </summary>
        public interface ITextExpression
        {
            /// <summary>
            /// evaluate the supplied object.
            /// </summary>
            /// <param name="o">The o.</param>
            /// <returns>a string containing the substituted text.</returns>
            string Eval(object o);
        }
    }
}