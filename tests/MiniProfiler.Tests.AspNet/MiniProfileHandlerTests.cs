using System;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Reflection;
using System.Web;

using Subtext.TestLibrary;
using Xunit;

namespace StackExchange.Profiling.Tests
{
    public class MiniProfilerHandlerTests
    {
        [Theory(WindowsOnly = true)]
        [InlineData("BRILLANT", 404)]
        [InlineData("underscore.js", 404)]
        [InlineData("results-list", 200)]
        [InlineData("includes.min.js", 200)]
        [InlineData("includes.min.css", 200)]
        public void GivenContext_WhenAResourceIsRequested_ThenTheCorrectHttpStatusCodeIsReturned(string resourceName, int expectedHttpStatus)
        {
            var sut = new MiniProfilerHandler(new MiniProfilerOptions()
            {
                ResultsListAuthorize = null
            });

            var res = GetRequestResponseHttpStatus(sut, resourceName);
            Assert.Equal(expectedHttpStatus, res);
        }

        private static readonly FieldInfo _cacheability = typeof(HttpCachePolicy).GetField("_cacheability", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _maxAge = typeof(HttpCachePolicy).GetField("_maxAge", BindingFlags.Instance | BindingFlags.NonPublic);

        [Theory(WindowsOnly = true)]
        [InlineData("BRILLANT", (HttpCacheability)6, null)]
        [InlineData("underscore.js", (HttpCacheability)6, null)]
        [InlineData("results-list", (HttpCacheability)6, null)]
        [InlineData("includes.min.js", HttpCacheability.Public, 2592000)]
        [InlineData("includes.min.css", HttpCacheability.Public, 2592000)]
        public void GivenContext_WhenAResourceIsRequested_ThenTheCorrectHttpCacheControlIsReturned(string resourceName, HttpCacheability expectedCacheability, int? expectedMaxAgeSeconds)
        {
            var sut = new MiniProfilerHandler(new MiniProfilerOptions()
            {
                ResultsListAuthorize = null
            });

            var res = GetRequestResponseCacheControl(sut, resourceName);
            Assert.Equal(expectedCacheability, (HttpCacheability)_cacheability.GetValue(res));
            if (expectedMaxAgeSeconds.HasValue)
            {
                Assert.Equal(TimeSpan.FromSeconds(expectedMaxAgeSeconds.Value), (TimeSpan)_maxAge.GetValue(res));
            }
        }

        [Theory(WindowsOnly = true)]
        [InlineData(true, 200)]
        [InlineData(false, 401)]
        public void GivenContext_WhenIndexIsRequested_ThenTheCorrectHttpStatusCodeIsReturned(bool isRequestAuthorized, int expectedHttpStatus)
        {
            var sut = new MiniProfilerHandler(new MiniProfilerOptions()
            {
                ResultsListAuthorize = _ => isRequestAuthorized
            });

            var res = GetRequestResponseHttpStatus(sut, "/results-index");
            Assert.Equal(expectedHttpStatus, res);
        }

        [Theory(WindowsOnly = true)]
		[InlineData("gzip", typeof(GZipStream))]
		[InlineData("deflate", typeof(DeflateStream))]
		[InlineData("unknown", null)]
		[InlineData("", null)]
		public void GivenContext_WhenIndexIsRequested_ThenTheCorrectHttpStatusCodeIsReturnedType(string acceptEncoding, Type expectedEncodingFilterType)
		{
			// Arrange
			var sut = new MiniProfilerHandler(new MiniProfilerOptions());

			// Act
			var res = GetRequestResponseEncoding(sut, "includes.min.js", acceptEncoding);

			// Assert
			// due the limitations of the HttpSimulator, we can't access the header values because it needs iis integrated pipeline mode.
			// instead we return the type of the response filter

			if (expectedEncodingFilterType == null)
			{
				Assert.NotEqual(typeof(GZipStream), res);
				Assert.NotEqual(typeof(DeflateStream), res);
			}
			else
			{
				Assert.Equal(expectedEncodingFilterType, res);
			}
		}

        private static int GetRequestResponseHttpStatus(MiniProfilerHandler handler, string resourceName)
        {
            using (new HttpSimulator("/mini-profiler-resources/", @"c:\").SimulateRequest(new Uri("http://localhost/mini-profiler-resources"+resourceName)))
            {
                handler.ProcessRequest(HttpContext.Current);
                return HttpContext.Current.Response.StatusCode;
            }
        }

        private static HttpCachePolicy GetRequestResponseCacheControl(MiniProfilerHandler handler, string resourceName)
        {
            using (new HttpSimulator("/mini-profiler-resources/", @"c:\").SimulateRequest(new Uri("http://localhost/mini-profiler-resources" + resourceName)))
            {
                handler.ProcessRequest(HttpContext.Current);
                return HttpContext.Current.Response.Cache;
            }
        }

        private static Type GetRequestResponseEncoding(MiniProfilerHandler handler, string resourceName, string encoding)
        {
            var headers = new NameValueCollection
            {
                ["Accept-encoding"] = encoding
            };
            using (new HttpSimulator("/mini-profiler-resources/", @"c:\").SimulateRequest(new Uri("http://localhost/mini-profiler-resources"+resourceName), headers: headers))
            {
                handler.ProcessRequest(HttpContext.Current);

				////return HttpContext.Current.Response.Headers["Content-encoding"];
	            return HttpContext.Current.Response.Filter.GetType();
            }
        }
    }
}
