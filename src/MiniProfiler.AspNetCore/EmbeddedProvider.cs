﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StackExchange.Profiling.Internal;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace StackExchange.Profiling
{
    internal class EmbeddedProvider
    {
        /// <summary>
        /// Embedded resource contents keyed by filename.
        /// </summary>
        private ConcurrentDictionary<string, string> _resourceCache { get; } = new ConcurrentDictionary<string, string>();
        private readonly IHostingEnvironment _env;
        private readonly IOptions<MiniProfilerOptions> _options;

        public EmbeddedProvider(IOptions<MiniProfilerOptions> options, IHostingEnvironment env)
        {
            _options = options;
            _env = env;
        }

        public string GetFile(HttpContext context, PathString file)
        {
            var response = context.Response;
            var path = file.Value;
            switch (Path.GetExtension(path))
            {
                case ".js":
                    response.ContentType = "application/javascript";
                    break;
                case ".css":
                    response.ContentType = "text/css";
                    break;
                case ".tmpl":
                    response.ContentType = "text/x-jquery-tmpl";
                    break;
                default:
                    return null;
            }

            if (TryGetResource(Path.GetFileName(path), out string resource))
            {
                return resource;
            }

            return null;
        }

        public bool TryGetResource(string filename, out string resource)
        {
            filename = filename.ToLower();
            if (_resourceCache.TryGetValue(filename, out resource))
            {
                return true;
            }

            // Check the on-disk location first, if configured
            if (_options.Value.UITemplatesPath.HasValue())
            {
                var customFile = Path.Combine(_options.Value.UITemplatesPath, filename);
                var fileInfo = _env.ContentRootFileProvider.GetFileInfo(customFile);
                if (fileInfo.Exists)
                {
                    using (var stream = fileInfo.CreateReadStream())
                    using (var reader = new StreamReader(stream))
                    {
                        resource = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                // Fall back to embedded
                using (var stream = typeof(MiniProfiler).GetTypeInfo().Assembly.GetManifestResourceStream("StackExchange.Profiling.ui." + filename))
                {
                    if (stream == null)
                    {
                        return false;
                    }
                    using (var reader = new StreamReader(stream))
                    {
                        resource = reader.ReadToEnd();
                    }
                }
            }

            _resourceCache[filename] = resource;

            return true;
        }
    }
}
