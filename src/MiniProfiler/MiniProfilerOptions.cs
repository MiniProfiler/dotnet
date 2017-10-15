using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.Internal;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Contains the settings specific to web applications (not in MiniProfiler.Standard)
    /// </summary>
    public class MiniProfilerOptions : MiniProfilerBaseOptions
    {
        /// <summary>
        /// Creates a new <see cref="MiniProfilerOptions"/> with <see cref="AspNetRequestProvider"/> as the provider.
        /// </summary>
        public MiniProfilerOptions()
        {
            // The default profiler for old ASP.NET (non-Core) is the AspNetRequestProvider
            // Only set this provider by default if we're in a web application
            if (HttpRuntime.AppDomainAppId != null)
            {
                ProfilerProvider = new AspNetRequestProvider();
            }
            // Default storage is 30 minutes in-memory
            Storage = new MemoryCacheStorage(TimeSpan.FromMinutes(30));
        }

        /// <summary>
        /// The path under which ALL routes are registered in, defaults to the application root.  For example, "~/myDirectory/" would yield
        /// "/myDirectory/includes.js" rather than just "/mini-profiler-resources/includes.js"
        /// Any setting here should be in APP RELATIVE FORM, e.g. "~/myDirectory/"
        /// </summary>
        public string RouteBasePath { get; set; } = "~/mini-profiler-resources";

        /// <summary>
        /// A function that determines who can access the MiniProfiler results URL and list URL.  It should return true when
        /// the request client has access to results, false for a 401 to be returned. HttpRequest parameter is the current request and
        /// </summary>
        /// <remarks>
        /// The HttpRequest parameter that will be passed into this function should never be null.
        /// </remarks>
        public Func<HttpRequest, bool> ResultsAuthorize { get; set; }

        /// <summary>
        /// Special authorization function that is called for the list results (listing all the profiling sessions), 
        /// we also test for results authorize always. This must be set and return true, to enable the listing feature.
        /// </summary>
        public Func<HttpRequest, bool> ResultsListAuthorize { get; set; }

        /// <summary>
        /// Function to provide the unique user ID based on the request, to store MiniProfiler IDs user
        /// </summary>
        public Func<HttpRequest, string> UserIdProvider { get; set; } = IpAddressIdentity.GetUser;

        /// <summary>
        /// By default, the output of the MiniProfilerHandler is compressed, if the request supports that.
        /// If this setting is false, the output won't be compressed. (Only do this when you take care of compression yourself)
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// The path where custom UI elements are stored.
        /// If the custom file doesn't exist, the standard resource is used.
        /// This setting should be in APP RELATIVE FORM, e.g. "~/App_Data/MiniProfilerUI"
        /// </summary>
        /// <remarks>A web server restart is required to reload new files.</remarks>
        public string CustomUITemplates { get; set; } = "~/App_Data/MiniProfilerUI";

        private string _versionHash;
        /// <summary>
        /// The hash to use for file cache breaking, this is automatically calculated.
        /// </summary>
        public override string VersionHash => _versionHash ?? (_versionHash = GetVersionHash());

        /// <summary>
        /// On first call, set the version hash for all cache breakers.
        /// </summary>
        private string GetVersionHash()
        {
            try
            {
                if (HttpContext.Current == null) return base.VersionHash;
                var files = new List<string>();

                var customUITemplatesPath = HttpContext.Current.Server.MapPath(CustomUITemplates);
                if (Directory.Exists(customUITemplatesPath))
                {
                    files.AddRange(Directory.EnumerateFiles(customUITemplatesPath));
                }

                if (files.Count == 0) return base.VersionHash;

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
                    return Convert.ToBase64String(hash);
                }
            }
            catch (Exception e)
            {
                //VersionHash is pre-populated
                Debug.WriteLine($"Error calculating folder hash: {e}\n{e.StackTrace}");
                return base.VersionHash;
            }
        }

        /// <summary>
        /// Configures the <see cref="MiniProfilerHandler"/>.
        /// </summary>
        protected override void OnConfigure()
        {
            MiniProfilerHandler.Configure(this);
        }
    }
}
