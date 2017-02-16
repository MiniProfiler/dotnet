using System;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Web;

using Subtext.TestLibrary;
using Xunit;
using StackExchange.Profiling;

namespace Tests
{
    internal class MiniProfilerHandlerTests
    {
        [Theory]
        [InlineData("BRILLANT", 404)]
        [InlineData("underscore.js", 404)]
        [InlineData("results-list", 200)]
        [InlineData("includes.js", 200)]
        [InlineData("includes.css", 200)]
        public void GivenContext_WhenAResourceIsRequested_ThenTheCorrectHttpStatusCodeIsReturned(string resourceName, int expectedHttpStatus)
        {
            // Arrange
            var sut = new MiniProfilerHandler();

            // Act
            var res = GetRequestResponseHttpStatus(sut, resourceName);

            // Assert
            Assert.Equal(expectedHttpStatus, res);
        }

        [Theory]
        [InlineData(true, 200)]
        [InlineData(false, 401)]
        public void GivenContext_WhenIndexIsRequested_ThenTheCorrectHttpStatusCodeIsReturned(bool isRequestAuthorized, int expectedHttpStatus)
        {
            // Arrange
            var sut = new MiniProfilerHandler();
            MiniProfilerWebSettings.ResultsListAuthorize = request => isRequestAuthorized;

            // Act
            var res = GetRequestResponseHttpStatus(sut, "/results-index");

            // Assert
            Assert.Equal(expectedHttpStatus, res);
        }

		[Theory]
		[InlineData("gzip", typeof(GZipStream))]
		[InlineData("deflate", typeof(DeflateStream))]
		[InlineData("unknown", null)]
		[InlineData("", null)]
		public void GivenContext_WhenIndexIsRequested_ThenTheCorrectHttpStatusCodeIsReturned(string acceptEncoding, Type expectedEncodingFilterType)
		{
			// Arrange
			var sut = new MiniProfilerHandler();

			// Act
			var res = GetRequestResponseEncoding(sut, "includes.js", acceptEncoding);

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

        private static Type GetRequestResponseEncoding(MiniProfilerHandler handler, string resourceName, string encoding)
        {
	        var headers = new NameValueCollection();
	        headers.Add("Accept-encoding", encoding);

	        using (new HttpSimulator("/mini-profiler-resources/", @"c:\").SimulateRequest(new Uri("http://localhost/mini-profiler-resources"+resourceName), HttpVerb.GET, headers))
            {
                handler.ProcessRequest(HttpContext.Current);

				////return HttpContext.Current.Response.Headers["Content-encoding"];
	            return HttpContext.Current.Response.Filter.GetType();
            }
        }
    }
}
