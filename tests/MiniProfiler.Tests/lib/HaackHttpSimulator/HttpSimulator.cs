using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.SessionState;

namespace Subtext.TestLibrary
{
    public enum HttpVerb
    {
        GET = 0,
        HEAD = 1,
        POST = 2,
        PUT = 3,
        DELETE = 4,
    }

    /// <summary>
    /// Useful class for simulating the HttpContext. This does not actually 
    /// make an HttpRequest, it merely simulates the state that your code 
    /// would be in "as if" handling a request. Thus the HttpContext.Current 
    /// property is populated.
    /// </summary>
    public class HttpSimulator : IDisposable
    {
        private const string defaultPhysicalAppPath = @"c:\InetPub\wwwRoot\";
        private StringBuilder builder;
        private Uri _referer;
        private readonly NameValueCollection _formVars = new NameValueCollection();
        private readonly NameValueCollection _headers = new NameValueCollection();

        public HttpSimulator() : this("/", defaultPhysicalAppPath)
        {
        }

        public HttpSimulator(string applicationPath) : this(applicationPath, defaultPhysicalAppPath)
        {
        }

        public HttpSimulator(string applicationPath, string physicalApplicationPath)
        {
            ApplicationPath = applicationPath;
            PhysicalApplicationPath = physicalApplicationPath;
        }

        /// <summary>
        /// Sets up the HttpContext objects to simulate a GET request.
        /// </summary>
        /// <remarks>
        /// Simulates a request to http://localhost/
        /// </remarks>
        public HttpSimulator SimulateRequest() => SimulateRequest(new Uri("http://localhost/"));

        /// <summary>
        /// Sets up the HttpContext objects to simulate a request.
        /// </summary>
        /// <param name="url">The Uri to hit (via POST).</param>
        /// <param name="httpVerb">The HTTP method to use.</param>
        /// <param name="formVariables">The form variables to send.</param>
        /// <param name="headers">The headers to send.</param>
        public virtual HttpSimulator SimulateRequest(
            Uri url,
            HttpVerb httpVerb = HttpVerb.GET,
            NameValueCollection formVariables = null,
            NameValueCollection headers = null)
        {
            HttpContext.Current = null;

            ParseRequestUrl(url);

            if (ResponseWriter == null)
            {
                builder = new StringBuilder();
                ResponseWriter = new StringWriter(builder);
            }

            SetHttpRuntimeInternals();

            string query = ExtractQueryStringPart(url);

            if (formVariables != null)
                _formVars.Add(formVariables);

            if (_formVars.Count > 0)
                httpVerb = HttpVerb.POST; //Need to enforce 

            if (headers != null)
                _headers.Add(headers);

            workerRequest = new SimulatedHttpRequest(ApplicationPath, PhysicalApplicationPath, PhysicalPath, Page, query, ResponseWriter, host, port, httpVerb.ToString());

            workerRequest.Form.Add(_formVars);
            workerRequest.Headers.Add(_headers);

            if (_referer != null)
                workerRequest.SetReferer(_referer);

            InitializeSession();

            InitializeApplication();

            #region Console Debug INfo

            Console.WriteLine("host: " + host);
            Console.WriteLine("virtualDir: " + applicationPath);
            Console.WriteLine("page: " + localPath);
            Console.WriteLine("pathPartAfterApplicationPart: " + _page);
            Console.WriteLine("appPhysicalDir: " + physicalApplicationPath);
            Console.WriteLine("Request.Url.LocalPath: " + HttpContext.Current.Request.Url.LocalPath);
            Console.WriteLine("Request.Url.Host: " + HttpContext.Current.Request.Url.Host);
            Console.WriteLine("Request.FilePath: " + HttpContext.Current.Request.FilePath);
            Console.WriteLine("Request.Path: " + HttpContext.Current.Request.Path);
            Console.WriteLine("Request.RawUrl: " + HttpContext.Current.Request.RawUrl);
            Console.WriteLine("Request.Url: " + HttpContext.Current.Request.Url);
            Console.WriteLine("Request.Url.Port: " + HttpContext.Current.Request.Url.Port);
            Console.WriteLine("Request.ApplicationPath: " + HttpContext.Current.Request.ApplicationPath);
            Console.WriteLine("Request.PhysicalPath: " + HttpContext.Current.Request.PhysicalPath);
            Console.WriteLine("HttpRuntime.AppDomainAppPath: " + HttpRuntime.AppDomainAppPath);
            Console.WriteLine("HttpRuntime.AppDomainAppVirtualPath: " + HttpRuntime.AppDomainAppVirtualPath);
            Console.WriteLine("HostingEnvironment.ApplicationPhysicalPath: " + HostingEnvironment.ApplicationPhysicalPath);
            Console.WriteLine("HostingEnvironment.ApplicationVirtualPath: " + HostingEnvironment.ApplicationVirtualPath);

            #endregion

            return this;
        }

        private static void InitializeApplication()
        {
            Type appFactoryType = Type.GetType("System.Web.HttpApplicationFactory, System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            object appFactory = ReflectionHelper.GetStaticFieldValue<object>("_theApplicationFactory", appFactoryType);
            ReflectionHelper.SetPrivateInstanceFieldValue("_state", appFactory, HttpContext.Current.Application);
        }

        private void InitializeSession()
        {
            HttpContext.Current = new HttpContext(workerRequest);
            HttpContext.Current.Items.Clear();
            var session = (HttpSessionState)ReflectionHelper.Instantiate(typeof(HttpSessionState), new Type[] { typeof(IHttpSessionState) }, new FakeHttpSessionState());

            HttpContext.Current.Items.Add("AspSession", session);
        }

        public class FakeHttpSessionState : NameObjectCollectionBase, IHttpSessionState
        {
            /// <summary>
            /// Ends the current session.
            /// </summary>
            public void Abandon() => BaseClear();

            /// <summary>
            /// Adds a new item to the session-state collection.
            /// </summary>
            /// <param name="name">The name of the item to add to the session-state collection. </param>
            /// <param name="value">The value of the item to add to the session-state collection. </param>
            public void Add(string name, object value) => BaseAdd(name, value);

            /// <summary>
            /// Deletes an item from the session-state item collection.
            /// </summary>
            /// <param name="name">The name of the item to delete from the session-state item collection. </param>
            public void Remove(string name) => BaseRemove(name);

            /// <summary>
            /// Deletes an item at a specified index from the session-state item collection.
            /// </summary>
            /// <param name="index">The index of the item to remove from the session-state collection. </param>
            public void RemoveAt(int index) => BaseRemoveAt(index);

            /// <summary>
            /// Clears all values from the session-state item collection.
            /// </summary>
            public void Clear() => BaseClear();

            /// <summary>
            /// Clears all values from the session-state item collection.
            /// </summary>
            public void RemoveAll() => BaseClear();

            /// <summary>
            /// Copies the collection of session-state item values to a one-dimensional array, starting at the specified index in the array.
            /// </summary>
            /// <param name="array">The <see cref="T:System.Array"></see> that receives the session values. </param>
            /// <param name="index">The index in array where copying starts. </param>
            ///  <exception cref="NotImplementedException"></exception>
            public void CopyTo(Array array, int index)
            {
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
                throw new NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
            }

            /// <summary>
            /// Gets the unique session identifier for the session.
            /// </summary>
            /// <returns>The session ID.</returns>
            public string SessionID { get; } = Guid.NewGuid().ToString();

            /// <summary>
            /// Gets and sets the time-out period (in minutes) allowed between requests before the session-state provider terminates the session.
            /// </summary>
            /// <returns>The time-out period, in minutes.</returns>
            public int Timeout { get; set; } = 30;

            /// <summary>
            /// Gets a value indicating whether the session was created with the current request.
            /// </summary>
            /// <returns>true if the session was created with the current request; otherwise, false.</returns>
            public bool IsNewSession { get; } = true;

            /// <summary>
            /// Gets the current session-state mode.
            /// </summary>
            /// <returns>One of the <see cref="T:System.Web.SessionState.SessionStateMode"></see> values.</returns>
            public SessionStateMode Mode => SessionStateMode.InProc;

            /// <summary>
            /// Gets a value indicating whether the session ID is embedded in the URL or stored in an HTTP cookie.
            /// </summary>
            /// <returns>true if the session is embedded in the URL; otherwise, false.</returns>
            public bool IsCookieless => false;

            /// <summary>
            /// Gets a value that indicates whether the application is configured for cookieless sessions.
            /// </summary>
            /// <returns>
            /// One of the <see cref="T:System.Web.HttpCookieMode"></see> values that indicate whether the application is configured for cookieless sessions. 
            /// The default is <see cref="F:System.Web.HttpCookieMode.UseCookies"></see>.
            /// </returns>
            public HttpCookieMode CookieMode => HttpCookieMode.UseCookies;

            /// <summary>
            /// Gets or sets the locale identifier (LCID) of the current session.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Globalization.CultureInfo"></see> instance that specifies the culture of the current session.
            /// </returns>
            public int LCID { get; set; }

            /// <summary>
            /// Gets or sets the code-page identifier for the current session.
            /// </summary>
            /// <returns>The code-page identifier for the current session.</returns>
            public int CodePage { get; set; }

            /// <summary>
            /// Gets a collection of objects declared by &lt;object Runat="Server" Scope="Session"/&gt; tags within the ASP.NET application file Global.asax.
            /// </summary>
            /// <returns>An <see cref="T:System.Web.HttpStaticObjectsCollection"></see> containing objects declared in the Global.asax file.</returns>
            public HttpStaticObjectsCollection StaticObjects { get; } = new HttpStaticObjectsCollection();

            /// <summary>
            /// Gets or sets a session-state item value by name.
            /// </summary>
            /// <param name="name">The key name of the session-state item value. </param>
            /// <returns>The session-state item value specified in the name parameter.</returns>
            public object this[string name]
            {
                get => BaseGet(name);
                set => BaseSet(name, value);
            }

            /// <summary>
            /// Gets or sets a session-state item value by numerical index.
            /// </summary>
            /// <param name="index">The numerical index of the session-state item value. </param>
            /// <returns>The session-state item value specified in the index parameter.</returns>
            public object this[int index]
            {
                get => BaseGet(index);
                set => BaseSet(index, value);
            }

            /// <summary>
            /// Gets an object that can be used to synchronize access to the collection of session-state values.
            /// </summary>
            /// <returns>An object that can be used to synchronize access to the collection.</returns>
            public object SyncRoot { get; } = new Object();

            /// <summary>
            /// Gets a value indicating whether access to the collection of session-state values is synchronized (thread safe).
            /// </summary>
            /// <returns>true if access to the collection is synchronized (thread safe); otherwise, false.</returns>
            public bool IsSynchronized => true;

            /// <summary>
            /// Gets a value indicating whether the session is read-only.
            /// </summary>
            /// <returns>true if the session is read-only; otherwise, false.</returns>
            bool IHttpSessionState.IsReadOnly => true;
        }

        /// <summary>
        /// Sets the referer for the request. Uses a fluent interface.
        /// </summary>
        /// <param name="referer">The referer to set.</param>
        public HttpSimulator SetReferer(Uri referer)
        {
            workerRequest?.SetReferer(referer);
            _referer = referer;
            return this;
        }

        /// <summary>
        /// Sets a form variable.
        /// </summary>
        /// <param name="name">The form variable name.</param>
        /// <param name="value">The form variable value.</param>
        /// <exception cref="InvalidOperationException">Throws when called after <see cref="Simulate()"/>.</exception>
        public HttpSimulator SetFormVariable(string name, string value)
        {
            //TODO: Change this ordering requirement.
            if (workerRequest != null)
                throw new InvalidOperationException("Cannot set form variables after calling Simulate().");

            _formVars.Add(name, value);

            return this;
        }

        /// <summary>
        /// Sets a header value.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        /// <exception cref="InvalidOperationException">Throws when called after <cref="Simulate()"/>.</exception>
        public HttpSimulator SetHeader(string name, string value)
        {
            //TODO: Change this ordering requirement.
            if (workerRequest != null)
                throw new InvalidOperationException("Cannot set headers after calling Simulate().");

            _headers.Add(name, value);

            return this;
        }

        private void ParseRequestUrl(Uri url)
        {
            if (url == null)
                return;
            host = url.Host;
            port = url.Port;
            localPath = url.LocalPath;
            _page = StripPrecedingSlashes(RightAfter(url.LocalPath, ApplicationPath));
            physicalPath = Path.Combine(physicalApplicationPath, _page.Replace("/", @"\"));
        }

        private static string RightAfter(string original, string search)
        {
            if (search.Length > original.Length || search.Length == 0)
                return original;

            int searchIndex = original.IndexOf(search, 0, StringComparison.InvariantCultureIgnoreCase);

            if (searchIndex < 0)
                return original;

            return original.Substring(original.IndexOf(search, 0, StringComparison.Ordinal) + search.Length);
        }

        public string Host => host;
        private string host;

        public string LocalPath => localPath;
        private string localPath;

        public int Port => port;
        private int port;

        /// <summary>
        /// Portion of the URL after the application.
        /// </summary>
        public string Page => _page;
        private string _page;

        /// <summary>
        /// The same thing as the IIS Virtual directory. It's 
        /// what gets returned by Request.ApplicationPath.
        /// </summary>
        public string ApplicationPath
        {
            get => applicationPath;
            set
            {
                applicationPath = value ?? "/";
                applicationPath = NormalizeSlashes(applicationPath);
            }
        }

        private string applicationPath = "/";

        /// <summary>
        /// Physical path to the application (used for simulation purposes).
        /// </summary>
        public string PhysicalApplicationPath
        {
            get => physicalApplicationPath;
            set
            {
                physicalApplicationPath = value ?? defaultPhysicalAppPath;
                //strip trailing backslashes.
                physicalApplicationPath = StripTrailingBackSlashes(physicalApplicationPath) + @"\";
            }
        }

        private string physicalApplicationPath = defaultPhysicalAppPath;

        /// <summary>
        /// Physical path to the requested file (used for simulation purposes).
        /// </summary>
        public string PhysicalPath => physicalPath;
        private string physicalPath = defaultPhysicalAppPath;

        public TextWriter ResponseWriter { get; set; }

        /// <summary>
        /// Returns the text from the response to the simulated request.
        /// </summary>
        public string ResponseText => (builder ?? new StringBuilder()).ToString();

        public SimulatedHttpRequest WorkerRequest => workerRequest;
        private SimulatedHttpRequest workerRequest;

        private static string ExtractQueryStringPart(Uri url)
        {
            string query = url.Query ?? string.Empty;
            if (query.StartsWith("?", StringComparison.Ordinal))
                return query.Substring(1);
            return query;
        }

        private void SetHttpRuntimeInternals()
        {
            //We cheat by using reflection.

            // get singleton property value
            HttpRuntime runtime = ReflectionHelper.GetStaticFieldValue<HttpRuntime>("_theRuntime", typeof(HttpRuntime));

            // set app path property value
            ReflectionHelper.SetPrivateInstanceFieldValue("_appDomainAppPath", runtime, PhysicalApplicationPath);
            // set app virtual path property value
            const string vpathTypeName = "System.Web.VirtualPath, System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            object virtualPath = ReflectionHelper.Instantiate(vpathTypeName, new Type[] { typeof(string) }, new object[] { ApplicationPath });
            ReflectionHelper.SetPrivateInstanceFieldValue("_appDomainAppVPath", runtime, virtualPath);

            // set codegen dir property value
            ReflectionHelper.SetPrivateInstanceFieldValue("_codegenDir", runtime, PhysicalApplicationPath);

            HostingEnvironment environment = GetHostingEnvironment();
            ReflectionHelper.SetPrivateInstanceFieldValue("_appPhysicalPath", environment, PhysicalApplicationPath);
            ReflectionHelper.SetPrivateInstanceFieldValue("_appVirtualPath", environment, virtualPath);
            ReflectionHelper.SetPrivateInstanceFieldValue("_configMapPath", environment, new ConfigMapPath(this));
        }

        protected static HostingEnvironment GetHostingEnvironment()
        {
            HostingEnvironment environment;
            try
            {
                environment = new HostingEnvironment();
            }
            catch (InvalidOperationException)
            {
                //Shoot, we need to grab it via reflection.
                environment = ReflectionHelper.GetStaticFieldValue<HostingEnvironment>("_theHostingEnvironment", typeof(HostingEnvironment));
            }
            return environment;
        }

        #region --- Text Manipulation Methods for slashes ---
        protected static string NormalizeSlashes(string s)
        {
            if (string.IsNullOrEmpty(s) || s == "/")
                return "/";

            s = s.Replace(@"\", "/");

            //Reduce multiple slashes in row to single.
            string normalized = Regex.Replace(s, "(/)/+", "$1");
            //Strip left.
            normalized = StripPrecedingSlashes(normalized);
            //Strip right.
            normalized = StripTrailingSlashes(normalized);
            return "/" + normalized;
        }

        protected static string StripPrecedingSlashes(string s) =>
            Regex.Replace(s, "^/*(.*)", "$1");

        protected static string StripTrailingSlashes(string s) =>
            Regex.Replace(s, "(.*)/*$", "$1", RegexOptions.RightToLeft);

        protected static string StripTrailingBackSlashes(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return Regex.Replace(s, @"(.*)\\*$", "$1", RegexOptions.RightToLeft);
        }
        #endregion

        internal class ConfigMapPath : IConfigMapPath
        {
            private readonly HttpSimulator _requestSimulation;
            public ConfigMapPath(HttpSimulator simulation)
            {
                _requestSimulation = simulation;
            }

#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            public string GetMachineConfigFilename()
            {
                throw new NotImplementedException();
            }

            public string GetRootWebConfigFilename()
            {
                throw new NotImplementedException();
            }

            public void GetPathConfigFilename(string siteID, string path, out string directory, out string baseName)
            {
                throw new NotImplementedException();
            }

            public void GetDefaultSiteNameAndID(out string siteName, out string siteID)
            {
                throw new NotImplementedException();
            }

            public void ResolveSiteArgument(string siteArgument, out string siteName, out string siteID)
            {
                throw new NotImplementedException();
            }
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.

            public string MapPath(string siteID, string path)
            {
                string page = StripPrecedingSlashes(RightAfter(path, _requestSimulation.ApplicationPath));
                return Path.Combine(_requestSimulation.PhysicalApplicationPath, page.Replace("/", @"\"));
            }

            public string GetAppPathForPath(string siteID, string path) => _requestSimulation.ApplicationPath;
        }

        ///<summary>
        ///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (HttpContext.Current != null)
            {
                OnBeforeDispose?.Invoke();

                HttpContext.Current = null;
            }
        }

        /// <summary>
        /// Called during <see cref="Dispose"/>, right before nulling <see cref="HttpContext.Current"/>.
        /// </summary>
        public event Action OnBeforeDispose;
    }
}
