using System.Linq;
using System.Threading;
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
        /// Creates the index specified by <paramref name="model"/>. If an index with the same name already exists, it is first dropped.
        /// </summary>
        /// <typeparam name="TDocument">Type of the document to be indexed.</typeparam>
        /// <param name="indexManager">Manager to create the index in.</param>
        /// <param name="model">Model defining the index.</param>
        /// <param name="options">Additional index creation options, if required.</param>
        /// <returns>Name of the index that was created.</returns>
        /// <remarks>The standard <see cref="IMongoIndexManager{TDocument}.CreateOne(CreateIndexModel{TDocument}, CreateOneIndexOptions, CancellationToken)"/>
        /// method will throw an exception if attempting to create an index with different options to one that already exists.
        /// By dropping that index first, this method ensures an exception will never be thrown, even if different options are used.</remarks>
        public static string CreateOneForce<TDocument>(this IMongoIndexManager<TDocument> indexManager, CreateIndexModel<TDocument> model, CreateOneIndexOptions options = null)
        {
            var indexNames = indexManager
                .List().ToList()
                .SelectMany(index => index.Elements)
                .Where(element => element.Name == "name")
                .Select(name => name.Value.ToString());
            var indexName = IndexNameHelper.GetIndexName(model.Keys.Render(indexManager.DocumentSerializer, indexManager.Settings.SerializerRegistry));

            if (indexNames.Contains(indexName))
            {
                indexManager.DropOne(indexName);
            }

            return indexManager.CreateOne(model, options);
        }
    }
}
