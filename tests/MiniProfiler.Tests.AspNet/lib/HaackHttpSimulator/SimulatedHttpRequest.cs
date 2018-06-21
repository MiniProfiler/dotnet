﻿#region Disclaimer/Info
///////////////////////////////////////////////////////////////////////////////////////////////////
// Subtext WebLog
// 
// Subtext is an open source weblog system that is a fork of the .TEXT
// weblog system.
//
// For updated news and information please visit http://subtextproject.com/
// Subtext is hosted at SourceForge at http://sourceforge.net/projects/subtext
// The development mailing list is at subtext-devs@lists.sourceforge.net 
//
// This project is licensed under the BSD license.  See the License.txt file for more information.
///////////////////////////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web.Hosting;

namespace Subtext.TestLibrary
{
    /// <summary>
    /// Used to simulate an HttpRequest.
    /// </summary>
    public class SimulatedHttpRequest : SimpleWorkerRequest
    {
        private Uri _referer;
        private readonly string _host;
        private readonly string _verb;
        private readonly int _port;
        private readonly string _physicalFilePath;

        /// <summary>
        /// Creates a new <see cref="SimulatedHttpRequest"/> instance.
        /// </summary>
        /// <param name="applicationPath">App virtual dir.</param>
        /// <param name="physicalAppPath">Physical Path to the app.</param>
        /// <param name="physicalFilePath">Physical Path to the file.</param>
        /// <param name="page">The Part of the URL after the application.</param>
        /// <param name="query">Query.</param>
        /// <param name="output">Output.</param>
        /// <param name="host">Host.</param>
        /// <param name="port">Port to request.</param>
        /// <param name="verb">The HTTP Verb to use.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="host"/> or <paramref name="applicationPath"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Throws when <paramref name="host"/> is empty.</exception>
	    public SimulatedHttpRequest(string applicationPath, string physicalAppPath, string physicalFilePath, string page, string query, TextWriter output, string host, int port, string verb) : base(applicationPath, physicalAppPath, page, query, output)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host), "Host cannot be null.");

            if (host.Length == 0)
                throw new ArgumentException("Host cannot be empty.", nameof(host));

            if (applicationPath == null)
                throw new ArgumentNullException(nameof(applicationPath), "Can't create a request with a null application path. Try empty string.");

            _verb = verb;
            _port = port;
            _physicalFilePath = physicalFilePath;
        }

        internal void SetReferer(Uri referer)
        {
            _referer = referer;
        }

        /// <summary>
        /// Returns the specified member of the request header.
        /// </summary>
        /// <returns>
        /// The HTTP verb returned in the request header.
        /// </returns>
        public override string GetHttpVerbName() => _verb;

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        public override string GetServerName() => _host;

        public override int GetLocalPort() => _port;

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>The headers.</value>
        public NameValueCollection Headers => Headers1;

        /// <summary>
        /// Gets the format exception.
        /// </summary>
        /// <value>The format exception.</value>
        public NameValueCollection Form { get; } = new NameValueCollection();

        public NameValueCollection Headers1 { get; } = new NameValueCollection();

        /// <summary>
        /// Get all nonstandard HTTP header name-value pairs.
        /// </summary>
        /// <returns>An array of header name-value pairs.</returns>
        public override string[][] GetUnknownRequestHeaders()
        {
            if (Headers1 == null || Headers1.Count == 0)
            {
                return null;
            }
            var headersArray = new string[Headers1.Count][];
            for (int i = 0; i < Headers1.Count; i++)
            {
                headersArray[i] = new string[2] { Headers1.Keys[i], Headers1[i] };
            }
            return headersArray;
        }

        public override string GetKnownRequestHeader(int index)
        {
            if (index == 0x24)
                return _referer == null ? string.Empty : _referer.ToString();

            if (index == 12 && _verb == "POST")
                return "application/x-www-form-urlencoded";

            return base.GetKnownRequestHeader(index);
        }

        public override string GetFilePathTranslated() => _physicalFilePath;

        /// <summary>
        /// Reads request data from the client (when not preloaded).
        /// </summary>
        /// <returns>The number of bytes read.</returns>
        public override byte[] GetPreloadedEntityBody()
        {
            var sb = new StringBuilder();

            foreach (string key in Form.Keys)
            {
                sb.Append(key).Append('=').Append(Form[key]).Append('&');
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// Returns a value indicating whether all request data
        /// is available and no further reads from the client are required.
        /// </summary>
        /// <returns><see langword="true"/> if all request data is available; otherwise, <see langword="false"/>.</returns>
        public override bool IsEntireEntityBodyIsPreloaded() => true;
    }
}
