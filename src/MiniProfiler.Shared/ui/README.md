# MiniProfiler data structures

This document specifies the MiniProfiler data structures.

## Profile

The root element.

 - **Id** (string) - a unique identifier, like a guid
 - **Name** (string) - request name, generally `Controller/Route`
 - **Started** (number) - request start time, in milliseconds since Unix epoch
 - **DurationMilliseconds** (number) - duration of request in milliseconds
 - **MachineName** (string) - name of the server
 - **CustomLinks** (object, optional) - object of links; keys are names, values are URLs
 - **Root** (Timing, optional) - root timing object
 - **ClientTimings** (ClientTimings, optional) - client timings object

## Timing

 - **Id** (string) - a unique identifier, like a guid
 - **Name** (string) - step name
 - **StartMilliseconds** (number) - step start time, in milliseconds since start of request
 - **DurationMilliseconds** (number) - duration of step in milliseconds (including children)
 - **Children** (array of Timing, optional) - array of child steps
 - **CustomTimings** (object, optional) - object with keys as call type ("redis", "sql", etc.) and values as arrays of CustomTiming of recorded calls (does not include child step's custom timings)

## CustomTiming

 - **Id** (string) - a unique identifier, like a guid
 - **ExecuteType** (string, optional) - execute type of call ("read", "write", "query", etc.)
 - **CommandString** (string) - HTML-escaped command; rendered in a pre, so supports newlines
 - **StackTraceSnippet** (string) - shortened, one-line stack trace; generally all function names on the stack separated by a space
 - **StartMilliseconds** (number) - call start time, in milliseconds since start of request
 - **DurationMilliseconds** (number) - duration of call in milliseconds
 - **FirstFetchDurationMilliseconds** (number, optional) - duration in milliseconds of the time to first result

## ClientTimings

This data should be recorded by the browser and reported back to MiniProfiler.

 - **RedirectCount** (number) - redirect count
 - **Timings** (array of ClientTiming)

## ClientTiming

 - **Name** (string)
 - **Start** (number)
 - **Duration** (number)

## Example

```json
{
    "Id": "7659f15a-6c47-4731-2854-67c6f2b92a3f",
    "Name": "goapp.ListFeeds",
    "Started": 1368211081000,
    "MachineName": "mjibson-mbp.local",
    "Root": {
        "Id": "0ee41f07-c4a7-44f9-2b81-b300cba7e856",
        "Name": "GET http://localhost:8080/user/list-feeds",
        "DurationMilliseconds": 17.595,
        "StartMilliseconds": 0,
        "Children": [
            {
                "Id": "9f6660ee-286b-4d2a-2d1d-90d2dcbfb4f3",
                "Name": "unmarshal user data",
                "DurationMilliseconds": 0.03399999999999981,
                "StartMilliseconds": 5.828,
                "Children": null,
                "CustomTimings": null
            },
            {
                "Id": "8f35fc4b-57dd-4455-22d4-e25fb27a13c8",
                "Name": "fetch feeds",
                "DurationMilliseconds": 2.6899999999999995,
                "StartMilliseconds": 5.865,
                "Children": null,
                "CustomTimings": {
                    "memcache": [
                        {
                            "Id": "7345c3d3-6fd0-4417-21e9-1b3b2014b18e",
                            "ExecuteType": "Get",
                            "CommandString": "Get\n\nkey:&#34;agtkZXZ-Z28tcmVhZHIlCxIBRiIeaHR0cDovL21hdHRqaWJzb24uY29tL2F0b20ueG1sDA&#34; for_cas:true ",
                            "StackTraceSnippet": "Call Call GetMulti Step ListFeeds",
                            "StartMilliseconds": 6.221,
                            "DurationMilliseconds": 1.442,
                            "FirstFetchDurationMilliseconds": 0
                        }
                    ]
                }
            },
            {
                "Id": "62078d64-7f70-4194-2a3f-d778d1844a35",
                "Name": "feed fetch + wait",
                "DurationMilliseconds": 8.904000000000002,
                "StartMilliseconds": 8.571,
                "Children": null,
                "CustomTimings": {
                    "datastore_v3": [
                        {
                            "Id": "decc860e-be66-4274-2c9e-bf63380c59c0",
                            "ExecuteType": "RunQuery",
                            "CommandString": "RunQuery\n\napp:&#34;dev~go-read&#34; kind:&#34;S&#34; ancestor:&lt;app:&#34;dev~go-read&#34; path:&lt;Element{type:&#34;F&#34; name:&#34;http://mattjibson.com/atom.xml&#34; } &gt; &gt; Filter{op:GREATER_THAN prope...",
                            "StackTraceSnippet": "Call Call GetAll",
                            "StartMilliseconds": 8.963,
                            "DurationMilliseconds": 5.435,
                            "FirstFetchDurationMilliseconds": 0
                        }
                    ],
                    "memcache": [
                        {
                            "Id": "d8ccb046-f3e8-41cc-22a6-fee03497f4fb",
                            "ExecuteType": "Get",
                            "CommandString": "Get\n\nfor_cas:true ",
                            "StackTraceSnippet": "Call Call GetMulti",
                            "StartMilliseconds": 14.921,
                            "DurationMilliseconds": 2.486,
                            "FirstFetchDurationMilliseconds": 0
                        }
                    ]
                }
            },
            {
                "Id": "92011933-a3d9-40e8-23ff-799492f7530a",
                "Name": "json marshal",
                "DurationMilliseconds": 0.06099999999999994,
                "StartMilliseconds": 17.529,
                "Children": null,
                "CustomTimings": null
            }
        ],
        "CustomTimings": {
            "memcache": [
                {
                    "Id": "e5a454d0-d434-4c97-29b7-1c383a8711d5",
                    "ExecuteType": "Get",
                    "CommandString": "Get\n\nkey:&#34;agtkZXZ-Z28tcmVhZHIcCxIBVSIVMTg1ODA0NzY0MjIwMTM5MTI0MTE4DA&#34; key:&#34;agtkZXZ-Z28tcmVhZHIoCxIBVSIVMTg1ODA0NzY0MjIwMTM5MTI0MTE4DAsSAlVEIgRkYXRhDA&#34; for_...",
                    "StackTraceSnippet": "Call Call GetMulti ListFeeds",
                    "StartMilliseconds": 0.535,
                    "DurationMilliseconds": 4.032,
                    "FirstFetchDurationMilliseconds": 0
                }
            ]
        }
    },
    "ClientTimings": null,
    "DurationMilliseconds": 17.595,
    "CustomLinks": {
        "appstats": "http://localhost:8080/_ah/stats/details?time=542064000"
    }
}
```

### Client Timings example

```json
{
    "Id": "779f22e9-2c1d-45ae-2677-ae5f34c3f39f",
    "Name": "goapp.Main",
    "Started": 1368211203000,
    "MachineName": "mjibson-mbp.local",
    "Root": {
        "Id": "4362ec49-b625-4184-2bb4-95d904cb466d",
        "Name": "GET http://localhost:8080/",
        "DurationMilliseconds": 5.964,
        "StartMilliseconds": 0,
        "Children": null,
        "CustomTimings": {
            "memcache": [
                {
                    "Id": "83a9d0a9-ed37-4e9d-27cd-4f8813ac6408",
                    "ExecuteType": "Get",
                    "CommandString": "Get\n\nkey:&#34;agtkZXZ-Z28tcmVhZHIcCxIBVSIVMTg1ODA0NzY0MjIwMTM5MTI0MTE4DA&#34; for_cas:true ",
                    "StackTraceSnippet": "Call Call GetMulti Get includes Main",
                    "StartMilliseconds": 0.978,
                    "DurationMilliseconds": 2.323,
                    "FirstFetchDurationMilliseconds": 0
                }
            ]
        }
    },
    "ClientTimings": {
        "RedirectCount": 0,
        "Timings": [
            {
                "Name": "Connect",
                "Start": 2,
                "Duration": 0
            },
            {
                "Name": "Request",
                "Start": 2,
                "Duration": -1
            },
            {
                "Name": "Response",
                "Start": 17,
                "Duration": 1
            },
            {
                "Name": "Unload Event",
                "Start": 18,
                "Duration": 0
            },
            {
                "Name": "Dom Content Loaded Event",
                "Start": 183,
                "Duration": 67
            },
            {
                "Name": "Load Event",
                "Start": 320,
                "Duration": 1
            }
        ]
    },
    "DurationMilliseconds": 5.964,
    "CustomLinks": {
        "appstats": "http://localhost:8080/_ah/stats/details?time=511483000"
    }
}
```
