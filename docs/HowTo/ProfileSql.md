---
layout: "default"
---
### How-To Profile SQL Server (...or anything else using ADO.NET)

MiniProfiler takes a wrapping approach to profiling, which means profiling SQL Server, MySQL, etc. is all the same because they're all based on ADO.NET base classes and interfaces like `DbConnection` and friends.

As an example, to profile SQL Server you just wrap the connection before using it, like this:

```c#
public DbConnection GetConnection()
{
    DbConnection connection = new System.Data.SqlClient.SqlConnection("...");
    return new StackExchange.Profiling.Data.ProfiledDbConnection(connection, MiniProfiler.Current);
}
```
...and it'd look very similar for Sqlite, here's that example:

```c#
public static DbConnection GetConnection()
{
    DbConnection connection = new System.Data.SQLite.SQLiteConnection("Data Source=:memory:");
    return new StackExchange.Profiling.Data.ProfiledDbConnection(connection, MiniProfiler.Current);
}
```


...then use this connection wherever you want to access your SQL database. You can use it with [Dapper](https://github.com/StackExchange/Dapper), or Linq2SQL, or whatever you're using to access SQL and you'll see profiler timings.