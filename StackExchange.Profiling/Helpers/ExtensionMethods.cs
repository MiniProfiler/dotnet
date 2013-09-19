using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Common extension methods to use only in this project
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Answers true if this String is either null or empty.
        /// </summary>
        /// <param name="value">
        /// The string value.
        /// </param>
        /// <returns>true if the string is null or white space</returns>
        internal static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        internal static bool HasValue(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// truncate the string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="maxLength">The max length.</param>
        /// <returns>The <see cref="string"/>.</returns>
        internal static string Truncate(this string s, int maxLength)
        {
            return s != null && s.Length > maxLength ? s.Substring(0, maxLength) : s;
        }

        /// <summary>
        /// Removes trailing / characters from a path and leaves just one
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>the input string with a trailing slash</returns>
        internal static string EnsureTrailingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "/+$", string.Empty) + "/";
        }

        /// <summary>
        /// Removes any leading / characters from a path
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>the input string without a leading slash</returns>
        internal static string RemoveLeadingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "^/+", string.Empty);
        }

        /// <summary>
        /// Removes any leading / characters from a path
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>the input string without a trailing slash</returns>
        internal static string RemoveTrailingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "/+$", string.Empty);
        }

        /// <summary>
        /// Serializes <paramref name="o"/> to a JSON string.
        /// </summary>
        /// <param name="o">the instance to serialise</param>
        /// <returns>the resulting JSON object as a string</returns>
        internal static string ToJson(this object o)
        {
            return o == null ? null : new JavaScriptSerializer().Serialize(o);
        }

        /// <summary>
        /// Returns a lowercase string of <paramref name="b"/> suitable for use in javascript.
        /// </summary>
        internal static string ToJs(this bool b)
        {
            return b ? "true" : "false";
        }

    }
}
