using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;

namespace Misc
{
    /// <summary>
    /// <para>
    /// This is an example of how you can use a shared storage (e.g. Redis) to get a complete MiniProfiler call
    /// across applications. For example, if App1 makes an HTTP call to App2 and you want the profiler from App2
    /// to show as a child in that tree when you're viewing it in App1, we can fetch and append the remote profiling
    /// session recorded from App1 at fetch time from App1.
    /// </para>
    /// <param>
    /// This requires shared storage! Or else you need to switch up how the .Load/.LoadAsync methods work below.
    /// </param>
    /// <para>
    /// If the source is this:
    /// Root
    ///  - HTTP Call timing
    /// And the remote side is:
    /// Root
    ///  - Some Work
    ///    - Some Work 2
    /// </para>
    /// <para>
    /// The result looks like this:
    /// Root
    ///  - HTTP Call timing
    ///    - Some Work
    ///      - Some Work 2
    /// </para>
    /// </summary>
    public class RemoteAppendExample : IAsyncStorage
    {
        private IAsyncStorage Wrapped { get; }

        /// <summary>
        /// We're just wrapping the base storage here, so that we can intercept the .Load(Async)() methods
        /// and append a remote profiler if needed.
        /// </summary>
        public RemoteAppendExample(IAsyncStorage storage) => Wrapped = storage ?? throw new ArgumentNullException(nameof(storage));

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
        public const string RemotePrefix = "Remote: ";

        /// <summary>
        /// Loads a profiler and appends any remote ones found.
        /// </summary>
        public MiniProfiler Load(Guid id)
        {
            var result = Wrapped.Load(id);
            if (result == null) return null;

            // TODO: You may want to filter here to only run this search for routes you expect to have a child profiler

            // Gets the timing hierarchy of the whole tree in a flat list form for iteration.
            foreach (var t in result.GetTimingHierarchy())
            {
                // We should only expect it once. Hop out as soon as we find it.
                // Check if this looks like a remote profile, and if so, attempt to load it:
                if (t.Name?.StartsWith(RemotePrefix) == true)
                {
                    // Note: if you wanted to recursively do this, call Load() instead of Wrapped.Load() here.
                    var remote = Wrapped.Load(t.Id);
                    if (remote != null)
                    {
                        // Found it!
                        // Let's pretty up the result and indicate which server we hit.
                        remote.Root.Name = $"Remote: {remote.Name} ({remote.MachineName})";
                        // In case you're using in-memory caching, let's protect against a double append on dupe runs.
                        if (!t.Children.Any(c => c.Id == remote.Root.Id))
                        {
                            // Add the remote profiler!
                            t.AddChild(remote.Root);
                        }
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// This is just an async version of above.
        /// </summary>
        public async Task<MiniProfiler> LoadAsync(Guid id)
        {
            var result = await Wrapped.LoadAsync(id).ConfigureAwait(false);
            if (result == null) return null;

            // TODO: You may want to filter here to only run this search for routes you expect to have a child profiler

            // Gets the timing hierarchy of the whole tree in a flat list form for iteration.
            foreach (var t in result.GetTimingHierarchy())
            {
                // We should only expect it once. Hop out as soon as we find it.
                // Check if this looks like a remote profile, and if so, attempt to load it:
                if (t.Name?.StartsWith(RemotePrefix) == true)
                {
                    // Note: if you wanted to recursively do this, call Load() instead of Wrapped.Load() here.
                    var remote = await Wrapped.LoadAsync(t.Id).ConfigureAwait(false);
                    if (remote != null)
                    {
                        // Found it!
                        // Let's pretty up the result and indicate which server we hit.
                        remote.Root.Name = $"Remote: {remote.Name} ({remote.MachineName})";
                        // In case you're using in-memory caching, let's protect against a double append on dupe runs.
                        if (!t.Children.Any(c => c.Id == remote.Root.Id))
                        {
                            // Add the remote profiler!
                            t.AddChild(remote.Root);
                        }
                        break;
                    }
                }
            }
            return result;
        }
    }

    public class RemoteAppendExampleUsage
    {
        // TODO: Add HttpClient example usage
    }
}
