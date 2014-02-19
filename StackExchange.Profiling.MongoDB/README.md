## MiniProfiler for MongoDB

This project serves two purposes.

First, it's a MongoDbStorage. It allows you to use MongoDB to save profiling data.

Secondly, it allows you to profile MongoDB queries.

### Using MongoDbStorage

To use the storage simply create an instance of `MongoDbStorage` type and assign it to `MiniProfiler.Settings.Storage` property:

    MiniProfiler.Settings.Storage = new MongoDbStorage("mongodb://localhost");


### Profiling MongoDB commands

Work in progress...
