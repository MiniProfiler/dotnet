using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Allow for results to be stored in and retrieved from multiple IAsyncStorage stores.
    /// When reading Loading a MiniProfiler, will load from the first Store that returns a record for the Guid.
    /// When saving, will save in all Stores.
    /// </summary>
    /// <example>Ideal usage scenario - you want to store requests in Cache and Sql Server, but only want to retrieve from Cache if it is available</example>
    public class MultiStorageProvider : IAsyncStorage
    {
        /// <summary>
        /// The stores that are exposed by this <see cref="MultiStorageProvider"/>
        /// </summary>
        public List<IAsyncStorage> Stores { get; set; }

        /// <summary>
        /// Should operations use Parallel.ForEach when it makes sense to do so (all save operations, and data retrieval where all items in <see cref="Stores"/> are hit? 
        /// If False, all operations will run synchronously, in order. Defaults to False.
        /// </summary>
        public bool AllowParallelOps { get; set; }

        /// <summary>
        /// Create the <see cref="MultiStorageProvider"/> with the given collection of <see cref="IAsyncStorage"/> objects (order is important!)
        /// </summary>
        /// <param name="stores">The <see cref="IAsyncStorage"/> objects to use for storage (order is important!)</param>
        public MultiStorageProvider(params IAsyncStorage[] stores)
        {
            Stores = stores.Where(x => x != null).ToList();
            if (Stores.Count == 0)
            {
                throw new ArgumentNullException(nameof(stores), "Please include at least one IAsyncStorage object when initializing a MultiStorageProvider");
            }
        }

        /// <summary>
        /// Run the List command on the first Store from <see cref="Stores"/> that returns a result with any values. 
        /// Will NOT return a superset of results from all <see cref="Stores"/>.
        /// </summary>
        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            if (Stores != null)
            {
                foreach (var store in Stores)
                {
                    var results = store.List(maxResults, start, finish, orderBy);
                    if (results != null && results.Any())
                    {
                        return results;
                    }
                }
            }
            return Enumerable.Empty<Guid>();
        }

        /// <summary>
        /// Asynchronously run the List command on the first Store from <see cref="Stores"/> that returns a result with any values. 
        /// Will NOT return a superset of results from all <see cref="Stores"/>.
        /// </summary>
        public async Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = default(DateTime?), DateTime? finish = default(DateTime?), ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            if (Stores == null) return Enumerable.Empty<Guid>();
            foreach (var store in Stores)
            {
                var results = await store.ListAsync(maxResults, start, finish, orderBy).ConfigureAwait(false);
                if (results != null && results.Any())
                {
                    return results;
                }
            }
            return Enumerable.Empty<Guid>();
        }

        /// <summary>
        /// Stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/> in all of the <see cref="Stores"/>.
        /// </summary>
        /// <param name="profiler">The results of a profiling session.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being un-viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        public void Save(MiniProfiler profiler)
        {
            if (Stores == null) return;
            if (AllowParallelOps)
            {
                Parallel.ForEach(Stores, x => x.Save(profiler));
            }
            else
            {
                Stores.ForEach(x => x.Save(profiler));
            }
        }

        /// <summary>
        /// Asynchronously stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/> in all of the <see cref="Stores"/>.
        /// </summary>
        /// <param name="profiler">The results of a profiling session.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being un-viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        public Task SaveAsync(MiniProfiler profiler)
        {
            if (Stores == null) return Task.CompletedTask;

            return Task.WhenAll(Stores.Select(s => s.SaveAsync(profiler)));
        }

        /// <summary>
        /// Returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>, 
        /// which should map to <see cref="MiniProfiler.Id"/>. Will check in all of the <see cref="IAsyncStorage"/>
        /// classes in <see cref="Stores"/>, and will return the first <see cref="MiniProfiler"/> that it finds.
        /// </summary>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        public MiniProfiler Load(Guid id)
        {
            if (Stores == null) return null;
            foreach (var store in Stores)
            {
                var result = store.Load(id);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Asynchronously returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>, 
        /// which should map to <see cref="MiniProfiler.Id"/>. Will check in all of the <see cref="IAsyncStorage"/>
        /// classes in <see cref="Stores"/>, and will return the first <see cref="MiniProfiler"/> that it finds.
        /// </summary>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        public async Task<MiniProfiler> LoadAsync(Guid id)
        {
            if (Stores == null) return null;
            foreach (var store in Stores)
            {
                var result = await store.LoadAsync(id).ConfigureAwait(false);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Sets a particular profiler session so it is considered "un-viewed".
        /// Will set this to all <see cref="IAsyncStorage"/> items in <see cref="Stores"/>
        /// </summary>
        public void SetUnviewed(string user, Guid id)
        {
            if (Stores == null) return;
            if (AllowParallelOps)
            {
                Parallel.ForEach(Stores, x => x.SetUnviewed(user, id));
            }
            else
            {
                Stores.ForEach(x => x.SetUnviewed(user, id));
            }
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "un-viewed".
        /// Will set this to all <see cref="IAsyncStorage"/> items in <see cref="Stores"/>
        /// </summary>
        public Task SetUnviewedAsync(string user, Guid id)
        {
            if (Stores == null) return Task.CompletedTask;

            return Task.WhenAll(Stores.Select(s => s.SetUnviewedAsync(user, id)));
        }

        /// <summary>
        /// Sets a particular profiler session to "viewed".
        /// Will set this to all <see cref="IAsyncStorage"/> items in <see cref="Stores"/>
        /// </summary>
        public void SetViewed(string user, Guid id)
        {
            if (Stores == null) return;
            if (AllowParallelOps)
            {
                Parallel.ForEach(Stores, x => x.SetViewed(user, id));
            }
            else
            {
                Stores.ForEach(x => x.SetViewed(user, id));
            }
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed".
        /// Will set this to all <see cref="IAsyncStorage"/> items in <see cref="Stores"/>
        /// </summary>
        public Task SetViewedAsync(string user, Guid id)
        {
            if (Stores == null) return Task.CompletedTask;

            return Task.WhenAll(Stores.Select(s => s.SetViewedAsync(user, id)));
        }

        /// <summary>
        /// Runs <see cref="IAsyncStorage.GetUnviewedIds"/> on each <see cref="IAsyncStorage"/> object in <see cref="Stores"/> and returns the Union of results.
        /// Will run on multiple stores in parallel if <see cref="AllowParallelOps"/> = true.
        /// </summary>
        /// <param name="user">The user to fetch IDs for</param>
        /// <returns>A distinct list of unviewed IDs</returns>
        public List<Guid> GetUnviewedIds(string user)
        {
            var results = new List<Guid>();
            if (Stores == null) return results;
            if (AllowParallelOps)
            {
                var locker = new object();
                Parallel.ForEach(Stores, x =>
                {
                    var result = x.GetUnviewedIds(user);
                    lock (locker)
                    {
                        results.AddRange(result);
                    }
                });
            }
            else
            {
                Stores.ForEach(x => results.AddRange(x.GetUnviewedIds(user)));
            }
            return results.Distinct().ToList(); // get rid of duplicates
        }

        /// <summary>
        /// Asynchronously runs <see cref="IAsyncStorage.GetUnviewedIds"/> on each <see cref="IAsyncStorage"/> object in <see cref="Stores"/> and returns the Union of results.
        /// </summary>
        /// <param name="user">The user to fetch IDs for</param>
        /// <returns>A distinct list of unviewed IDs</returns>
        public async Task<List<Guid>> GetUnviewedIdsAsync(string user)
        {
            var results = new List<Guid>();
            if (Stores == null) return results;

            var tasks = Stores.Select(s => s.GetUnviewedIdsAsync(user));
            await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach(var t in tasks)
            {
                results.AddRange(t.Result);
            }

            return results.Distinct().ToList(); // get rid of duplicates
        }
    }
}
