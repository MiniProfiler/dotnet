using System;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Web;

using NUnit.Framework;
using Subtext.TestLibrary;

namespace StackExchange.Profiling.Tests
{
    [TestFixture]
    class MiniProfilerHandlerTests
    {
        [Test]
        [TestCase("BRILLANT", 404)]
        [TestCase("underscore.js", 404)]
        [TestCase("jquery.1.7.1.js", 404)]
        [TestCase("jquery.tmpl.js", 404)]
        [TestCase("jquery.tmpl.js", 404)]
        [TestCase("results-list", 200)]
        [TestCase("includes.js", 200)]
        [TestCase("includes.css", 200)]
        public void GivenContext_WhenAResourceIsRequested_ThenTheCorrectHttpStatusCodeIsReturned(string resourceName, int expectedHttpStatus)
        {
            // Arrange
            var sut = new MiniProfilerHandler();

            // Act
            var res = GetRequestResponseHttpStatus(sut, resourceName);

            // Assert
            Assert.AreEqual(expectedHttpStatus, res);
        }

        [Test]
        [TestCase(true, 200)]
        [TestCase(false, 401)]
        public void GivenContext_WhenIndexIsRequested_ThenTheCorrectHttpStatusCodeIsReturned(bool isRequestAuthorized, int expectedHttpStatus)
        {
            // Arrange
            var sut = new MiniProfilerHandler();
            MiniProfilerWebSettings.Results_List_Authorize = request => isRequestAuthorized;

            // Act
            var res = GetRequestResponseHttpStatus(sut, "/results-index");
            
            // Assert
            Assert.AreEqual(expectedHttpStatus, res);
        }

		[Test]
		[TestCase("gzip", typeof(GZipStream))]
		[TestCase("deflate", typeof(DeflateStream))]
		[TestCase("unknown", null)]
		[TestCase("", null)]
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
				Assert.AreNotEqual(typeof(GZipStream), res);
				Assert.AreNotEqual(typeof(DeflateStream), res);
			}
			else
			{
				Assert.AreEqual(expectedEncodingFilterType, res);
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
