using System;
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
            MiniProfiler.Settings.Results_List_Authorize = request => isRequestAuthorized;

            // Act
            var res = GetRequestResponseHttpStatus(sut, "/results-index");
            
            // Assert
            Assert.AreEqual(expectedHttpStatus, res);
        }

        private static int GetRequestResponseHttpStatus(MiniProfilerHandler handler, string resourceName)
        {
            using (new HttpSimulator("/mini-profiler-resources/", @"c:\").SimulateRequest(new Uri("http://localhost/mini-profiler-resources"+resourceName)))
            {
                handler.ProcessRequest(HttpContext.Current);
                return HttpContext.Current.Response.StatusCode;
            }
        }
    }
}
