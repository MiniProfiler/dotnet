using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Common extension methods to use only in this project
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Performs the specified action on each element of the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <param name="list">the list to perform the operations for</param>
        /// <param name="action">The <see cref="T:System.Action`1"/> delegate to perform on each element of the
        ///  <see cref="T:System.Collections.Generic.List`1"/>.</param><exception cref="T:System.ArgumentNullException">
        /// <paramref name="action"/> is null.</exception><exception cref="T:System.InvalidOperationException">An element in the collection has been modified. CautionThis exception is thrown starting with the .NET Framework 4.5. </exception>
        public static void ForEach<T>(this IList<T> list, Action<T> action)
        {
            foreach(var item in list)
                action(item);
        }

        public static void RemoveAll<T>(this IList<T> list, Predicate<T> match)
        {
            var lockObj = (list as SynchronizedCollection<T>)?.SyncRoot;
            if (lockObj != null)
                Monitor.Enter(lockObj);
            try
            {
                var indexesToRemove = new List<int>();
                for(int i = 0; i < list.Count; i++)
                {
                    if (match(list[i]))
                        indexesToRemove.Add(i);
                }
                indexesToRemove.Reverse();
                foreach (var idx in indexesToRemove)
                {
                    list.RemoveAt(idx);
                }
            }
            finally
            {
                if (lockObj != null)
                    Monitor.Exit(lockObj);
            }
        }

        /// <summary>
        /// Answers true if this String is either null or empty.
        /// </summary>
        internal static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        internal static bool HasValue(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Chops off a string at the specified length and accounts for smaller length
        /// </summary>
        /// <param name="s"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string Truncate(this string s, int maxLength)
        {
            return s != null && s.Length > maxLength ? s.Substring(0, maxLength) : s;
        }

        /// <summary>
        /// Removes trailing / characters from a path and leaves just one
        /// </summary>
        internal static string EnsureTrailingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "/+$", string.Empty) + "/";
        }

        /// <summary>
        /// Removes any leading / characters from a path
        /// </summary>
        internal static string RemoveLeadingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "^/+", string.Empty);
        }

        /// <summary>
        /// Removes any leading / characters from a path
        /// </summary>
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
        /// Deserializes <paramref name="s"/> to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="s">The string to deserialize</param>
        /// <returns>The object resulting from the given string</returns>
        internal static T FromJson<T>(this string s) where T : class 
        {
            return s.HasValue() ? new JavaScriptSerializer().Deserialize<T>(s) : null;
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
