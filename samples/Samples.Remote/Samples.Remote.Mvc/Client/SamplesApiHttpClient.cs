using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;

namespace Samples.Remote.Mvc.Client
{
    public class SamplesApiHttpClient
    {
        private readonly HttpClient _client;

        public SamplesApiHttpClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<JArray> GetAllAsync()
        {
            using (var response = await _client.GetAsync("api/samples"))
            {
                response.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync());
            }
        }

        public async Task<JArray> GetOddAsync()
        {
            using (var response = await _client.GetAsync("api/samples/odd"))
            {
                response.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync());
            }
        }
    }

    public class SamplesApiHttpClientHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (MiniProfiler.Current.Step("Http-Call"))
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (response.Headers.TryGetValues("MiniProfiler-Remote-Id", out var s))
                {
                    // Add a temporary step with some information, like the remote host and the remote session id.
                    // these information are reused later by the RemoteAsyncStorage to make an http call and get 
                    // the remote profile information, then we will replace this temporary step with the remote one.
                    // Please note that this is the only additional work required during the profiled call, 
                    // the http call issued to the remote service in order to get the remote profiling information 
                    // is executed by miniprofiler so it won't affect your request performance, we just need this simple
                    // delegating handler to record this temporary step.
                    MiniProfiler.Current.Step($"RemoteStep:{request.RequestUri.GetLeftPart(UriPartial.Authority)}:{s.Single()}");
                }

                return response;
            }
        }
    }

    public class RemoteAsyncStorage : IAsyncStorage
    {
        private IAsyncStorage Wrapped { get; }

        /// <summary>
        /// We're just wrapping the base storage here, so that we can intercept the .Load(Async)() methods
        /// and append a remote profiler if needed.
        /// </summary>
        public RemoteAsyncStorage(IAsyncStorage storage) => Wrapped = storage ?? throw new ArgumentNullException(nameof(storage));

        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending) =>
            Wrapped.List(maxResults, start, finish, orderBy);
        public void Save(MiniProfiler profiler) => Wrapped.Save(profiler);
        public void SetUnviewed(string user, Guid id) => Wrapped.SetUnviewed(user, id);
        public void SetViewed(string user, Guid id) => Wrapped.SetViewed(user, id);
        public List<Guid> GetUnviewedIds(string user) => Wrapped.GetUnviewedIds(user);

        public Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending) =>
            Wrapped.ListAsync(maxResults, start, finish, orderBy);
        public Task SaveAsync(MiniProfiler profiler) => Wrapped.SaveAsync(profiler);
        public Task SetUnviewedAsync(string user, Guid id) => Wrapped.SetUnviewedAsync(user, id);
        public Task SetViewedAsync(string user, Guid id) => Wrapped.SetViewedAsync(user, id);
        public Task<List<Guid>> GetUnviewedIdsAsync(string user) => Wrapped.GetUnviewedIdsAsync(user);

        /// <summary>
        /// This is a timing name prefix we check to see if we should even be trying to load a remote profiler
        /// ...but this signal could be anything you want.
        /// </summary>
        public const string RemotePrefix = "RemoteStep:";

        /// <summary>
        /// Loads a profiler and appends any remote ones found.
        /// </summary>
        public MiniProfiler Load(Guid id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is just an async version of above.
        /// </summary>
        public async Task<MiniProfiler> LoadAsync(Guid id)
        {
            var result = await Wrapped.LoadAsync(id).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            // TODO: You may want to filter here to only run this search for routes you expect to have a child profiler

            // Gets the timing hierarchy of the whole tree in a flat list form for iteration.
            foreach (var t in result.GetTimingHierarchy())
            {
                // We should only expect it once. Hop out as soon as we find it.
                // Check if this looks like a remote profile, and if so, attempt to load it:
                if (t.Name?.StartsWith(RemotePrefix) == true)
                {
                    // Note: if you wanted to recursively do this, call Load() instead of Wrapped.Load() here.
                    var remote = await LoadRemoteProfilingSessionAsync(t).ConfigureAwait(false);
                    if (remote != null)
                    {
                        // Found it!
                        // Let's pretty up the result and indicate which server we hit.
                        remote.Root.Name = $"Remote: {remote.Name} ({remote.MachineName})";
                        // In case you're using in-memory caching, let's protect against a double append on dupe runs.
                        if (!t.Children?.Any(c => c.Id == remote.Root.Id) ?? true)
                        {
                            var parent = t.ParentTiming;
                            // Remove the temporary step, we don't need it
                            parent.Children.Remove(t);
                            // Add the remote profiler!
                            parent.AddChild(remote.Root);
                        }
                        break;
                    }
                }
            }
            return result;
        }

        private async Task<MiniProfiler> LoadRemoteProfilingSessionAsync(Timing timing)
        {
            string stepName = timing.Name;
            int firstColonIndex = RemotePrefix.Length;
            int lastColonIndex = stepName.LastIndexOf(":", StringComparison.Ordinal);
            var remoteHost = stepName.Substring(firstColonIndex, lastColonIndex - firstColonIndex);
            var remoteSessionId = Guid.Parse(stepName.Substring(lastColonIndex + 1));

            // Make an http call to the remote service miniprofiler middelware to retrive the remote profiling session
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // NOTE this paths need to match the one used to configure miniprofiler in the Api project.
                // The actual value is the default setting, make sure to keep this and the remote one in sync!
                string path = "mini-profiler-resources/results";
                var response = await client.PostAsync($"{remoteHost}/{path}",
                    new StringContent(JsonConvert.SerializeObject(new MiniProfilerRequest(remoteSessionId)), Encoding.UTF8));

                response.EnsureSuccessStatusCode();
                return MiniProfiler.FromJson(await response.Content.ReadAsStringAsync());
            }
        }

        private class MiniProfilerRequest
        {
            public MiniProfilerRequest(Guid id)
            {
                Id = id;
            }

            public Guid Id { get; }
        }
    }
}
