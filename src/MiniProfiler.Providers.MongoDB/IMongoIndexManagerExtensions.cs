using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Extension methods for <see cref="IMongoIndexManager{TDocument}"/>.
    /// </summary>
    public static class IMongoIndexManagerExtensions
    {
        /// <summary>
        /// Drops the index defined by the specified <paramref name="keys"/> from the provided <paramref name="indexManager"/>.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document containing the index.</typeparam>
        /// <param name="indexManager">The manager potentially containing the index.</param>
        /// <param name="keys">The index definition.</param>
        /// <remarks>No exception is thrown if the index does not exist.</remarks>
        public static void DropOne<TDocument>(this IMongoIndexManager<TDocument> indexManager, IndexKeysDefinition<TDocument> keys)
        {
            var indexName = IndexNameHelper.GetIndexName(keys.Render(indexManager.DocumentSerializer, indexManager.Settings.SerializerRegistry));

            indexManager.DropOne(indexName);
        }
    }
}
