using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Options for configuring <see cref="MongoDbStorage"/>.
    /// </summary>
    public class MongoDbStorageOptions
    {
        /// <summary>
        /// The connection string to use for connecting to MongoDB.
        /// Defaults to <c>mongodb://localhost</c>.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Name of the collection in which to store <see cref="MiniProfiler"/> sessions in.
        /// Defaults to <c>profilers</c>.
        /// </summary>
        public string CollectionName { get; set; } = "profilers";

        /// <summary>
        /// If set to <see langword="true"/>, C# <c>decimal</c> fields will be serialized as <c>NumberDecimal</c>s in MongoDB.
        /// If set to <see langword="false"/>, will serialize these fields as strings (backwards-compatible with older versions of this provider).
        /// Defaults to <see langword="true"/>.
        /// </summary>
        public bool SerializeDecimalFieldsAsNumberDecimal { get; set; } = true;

        /// <summary>
        /// Specifies whether relevant indexes will automatically created when this provider is instantiated.
        /// Defaults to <see langword="true" />.
        /// </summary>
        public bool AutomaticallyCreateIndexes { get; set; } = true;

        /// <summary>
        /// Specifies whether relevant indexes will automatically recreated if creation fails (e.g. because something with
        /// different options was previously created).
        /// *THIS DROPS EXISTING DATA*
        /// Defaults to <see langword="false" />.
        /// </summary>
        public bool AutomaticallyRecreateIndexes { get; set; } = false;

        /// <summary>
        /// Gets or sets how long to cache each <see cref="MiniProfiler"/> for, in absolute terms.
        /// Defaults to one hour.
        /// </summary>
        /// <remarks><list type="bullet">
        /// <item>You need to either set <see cref="AutomaticallyCreateIndexes"/> to true or call
        /// <see cref="MongoDbStorage.WithIndexCreation(TimeSpan)"/> for this value to have any effect.</item>
        /// <item>Setting this option will drop any (<see cref="MiniProfiler.Started"/>, ascending) index previously
        /// defined, including those with custom options.</item>
        /// </list></remarks>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);
    }
}
