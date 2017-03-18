# MiniProfiler for RavenDB

This package allows you to profile your RavenDB requests using MiniProfiler.

Simply call `MiniProfilerRaven.InitializeFor(store)` when you initialize
your Raven `DocumentStore` (please note that `EmbeddableDocumentStore` is not supported).

```c#
var store = new DocumentStore();
store.Initialize();

// Initialize MiniProfiler
MiniProfilerRaven.InitializeFor(store);
```

A new column will appear on MiniProfiler that provides detail on your Raven requests, including `POST`ed data.

All of the existing settings and functionality of MiniProfiler can be taken advantage of.