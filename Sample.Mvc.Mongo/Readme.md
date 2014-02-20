## Sample for Miniprofiler for MongoDB

This sample shows how `MongoDbStorage` and `MongoDB Profiler` works.

### MongoDB Storage

The easiest way to test how it works is to ensure that the server is running
on `localhost` on standart port, without authentication.

`MongoDbStorage` will create the database called `MiniProfiler` and put profiling data there.

### MongoDb Profiler

To test how profiling works you need some sample data. The app expects to find the `test`
database with `foo` collection with following data schema:

        {_id:<object_id>, r:<some_random_number_from_0_to_1>}

Use this JS code snippet to quickly populate it with sample data:

        for (var i = 0; i < 1000000; i++) {
            db.foo.save({
                i: i,
                r: Math.random()
            });
        }

_To be continued..._