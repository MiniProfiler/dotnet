using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Web;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Contains the settings specific to web applications (not in MiniProfiler.Standard)
    /// </summary>
    public static class MiniProfilerWebSettings
    {
        /// <summary>
        /// Provides user identification for a given profiling request.
        /// </summary>
        public static IUserProvider UserProvider { get; set; } = new IpAddressIdentity();

        /// <summary>
        /// A function that determines who can access the MiniProfiler results url and list url.  It should return true when
        /// the request client has access to results, false for a 401 to be returned. HttpRequest parameter is the current request and
        /// </summary>
        /// <remarks>
        /// The HttpRequest parameter that will be passed into this function should never be null.
        /// </remarks>
        public static Func<HttpRequest, bool> ResultsAuthorize { get; set; }

        /// <summary>
        /// Special authorization function that is called for the list results (listing all the profiling sessions), 
        /// we also test for results authorize always. This must be set and return true, to enable the listing feature.
        /// </summary>
        public static Func<HttpRequest, bool> ResultsListAuthorize { get; set; }

        /// <summary>
        /// By default, the output of the MiniProfilerHandler is compressed, if the request supports that.
        /// If this setting is false, the output won't be compressed. (Only do this when you take care of compression yourself)
        /// </summary>
        public static bool EnableCompression { get; set; } = true;

        /// <summary>
        /// When <see cref="MiniProfiler.Start(string)"/> is called, if the current request url contains any items in this property,
        /// no profiler will be instantiated and no results will be displayed.
        /// Default value is { "/content/", "/scripts/", "/favicon.ico" }.
        /// </summary>
        public static string[] IgnoredPaths { get; set; } = new string[] { "/content/", "/scripts/", "/favicon.ico" };

        /// <summary>
        /// The path where custom ui elements are stored.
        /// If the custom file doesn't exist, the standard resource is used.
        /// This setting should be in APP RELATIVE FORM, e.g. "~/App_Data/MiniProfilerUI"
        /// </summary>
        /// <remarks>A web server restart is required to reload new files.</remarks>
        public static string CustomUITemplates { get; set; } = "~/App_Data/MiniProfilerUI";

        /// <summary>
        /// On first call, set the version hash for all cache breakers
        /// </summary>
        static MiniProfilerWebSettings()
        {
            try
            {
                if (HttpContext.Current == null) return;
                var files = new List<string>();

                var customUITemplatesPath = HttpContext.Current.Server.MapPath(CustomUITemplates);
                if (Directory.Exists(customUITemplatesPath))
                {
                    files.AddRange(Directory.EnumerateFiles(customUITemplatesPath));
                }

                if (files.Count == 0) return;

                using (var sha256 = new SHA256CryptoServiceProvider())
                {
                    var hash = new byte[sha256.HashSize / 8];
                    foreach (string file in files)
                    {
                        // sha256 can throw a FIPS exception, but SHA256CryptoServiceProvider is FIPS BABY - FIPS 
                        byte[] contents = File.ReadAllBytes(file);
                        byte[] hashfile = sha256.ComputeHash(contents);
                        for (int i = 0; i < (sha256.HashSize / 8); i++)
                        {
                            hash[i] = (byte)(hashfile[i] ^ hash[i]);
                        }
                    }
                    MiniProfiler.Settings.VersionHash = Convert.ToBase64String(hash);
                }
            }
            catch (Exception e)
            {
                //VersionHash is pre-opopulated
                Debug.WriteLine($"Error calculating folder hash: {e.ToString()}\n{e.StackTrace}");
            }
        }
    }
}
