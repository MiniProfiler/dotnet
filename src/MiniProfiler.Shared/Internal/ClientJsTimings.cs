using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

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
        public List<ClientTiming> Performance { get; set; }

        /// <summary>
        /// The JavaScript client probes, if any.
        /// </summary>
        public List<ClientTiming> Probes { get; set; }

        /// <summary>
        /// The amount of redirects made before the pageload.
        /// </summary>
        public int? RedirectCount { get; set; }

        /// <summary>
        /// The total count of timings on this request.
        /// </summary>
        public int TimingCount => (Performance?.Count ?? 0) + (Probes?.Count ?? 0);

        private static readonly JsonSerializer _serializer = new JsonSerializer();

        /// <summary>
        /// Returns a deserialize object from an input stream, like an HTTP request body.
        /// </summary>
        /// <param name="stream">The stream to deserialize.</param>
        /// <param name="result">The resulting <see cref="ResultRequest"/>, if successful.</param>
        /// <returns>A <see cref="ResultRequest"/> object.</returns>
        public static bool TryParse(Stream stream, out ResultRequest result)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    var tmp = _serializer.Deserialize<ResultRequest>(jsonTextReader);
                    if (tmp.Id.HasValue)
                    {
                        result = tmp;
                        return true;
                    }
                }
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
