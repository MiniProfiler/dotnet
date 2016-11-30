using System.Text.RegularExpressions;
#if NET45
using System.Web.Script.Serialization;
#else
using Newtonsoft.Json;
#endif

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Common extension methods to use only in this project
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Answers true if this String is either null or empty.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        public static bool HasValue(this string value) => !string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// Chops off a string at the specified length and accounts for smaller length
        /// </summary>
        public static string Truncate(this string s, int maxLength)
        {
            return s?.Length > maxLength ? s.Substring(0, maxLength) : s;
        }

        /// <summary>
        /// Removes trailing / characters from a path and leaves just one
        /// </summary>
        public static string EnsureTrailingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "/+$", string.Empty) + "/";
        }

        /// <summary>
        /// Serializes <paramref name="o"/> to a JSON string.
        /// </summary>
        /// <param name="o">the instance to serialise</param>
        /// <returns>the resulting JSON object as a string</returns>
        public static string ToJson(this object o)
        {
            return o != null
#if NET45
                ? new JavaScriptSerializer() { MaxJsonLength = int.MaxValue }.Serialize(o) : null;
#else
                ? JsonConvert.SerializeObject(o) : null;
#endif
        }

        /// <summary>
        /// Deserializes <paramref name="s"/> to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="s">The string to deserialize</param>
        /// <returns>The object resulting from the given string</returns>
        public static T FromJson<T>(this string s) where T : class
        {
            return s.HasValue()
#if NET45
                ? new JavaScriptSerializer().Deserialize<T>(s) : null;
#else
                ? JsonConvert.DeserializeObject<T>(s) : null;
#endif
        }
    }
}
