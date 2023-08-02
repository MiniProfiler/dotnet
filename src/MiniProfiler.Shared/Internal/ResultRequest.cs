using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// JSON format sent on the request to /results.
    /// </summary>
    public class ResultRequest
    {
        /// <summary>
        /// The ID of the MiniProfiler both being requested, and that these client results are for.
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// The window.performace timings from the client.
        /// </summary>
        public List<ClientTiming>? Performance { get; set; }

        /// <summary>
        /// The JavaScript client probes, if any.
        /// </summary>
        public List<ClientTiming>? Probes { get; set; }

        /// <summary>
        /// The amount of redirects made before the pageload.
        /// </summary>
        public int? RedirectCount { get; set; }

        /// <summary>
        /// The total count of timings on this request.
        /// </summary>
        public int TimingCount => (Performance?.Count ?? 0) + (Probes?.Count ?? 0);

#if NEWTONSOFT
        private static readonly Newtonsoft.Json.JsonSerializer _serializer = new();
#endif

        /// <summary>
        /// Returns a deserialize object from an input stream, like an HTTP request body.
        /// </summary>
        /// <param name="stream">The stream to deserialize.</param>
        /// <param name="result">The resulting <see cref="ResultRequest"/>, if successful.</param>
        /// <returns>A <see cref="ResultRequest"/> object.</returns>
        public static bool TryParse(Stream stream, [NotNullWhen(true)] out ResultRequest? result)
        {
            try
            {
#if NEWTONSOFT
                using var sr = new StreamReader(stream);
                using (var jsonTextReader = new Newtonsoft.Json.JsonTextReader(sr))
                {
                    var tmp = _serializer.Deserialize<ResultRequest>(jsonTextReader);
                    if (tmp?.Id.HasValue == true)
                    {
                        result = tmp;
                        return true;
                    }
                }
#elif STJSON
                var tmp = System.Text.Json.JsonSerializer.Deserialize<ResultRequest>(stream);
                if (tmp?.Id.HasValue == true)
                {
                    result = tmp;
                    return true;
                }
#endif
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing: " + e);
            }
            result = null;
            return false;
        }
    }
}
