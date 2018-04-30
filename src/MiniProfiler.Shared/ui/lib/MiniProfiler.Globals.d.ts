declare global {
    interface Window {
        PerformancePaintTiming: Function;
        mPt: MiniProfilerProbeTiming;
        chrome: {
            loadTimes(): {
                firstPaintTime: number;
                firstPaintAfterLoadTime: number;
            }
        };
        JSON: any;
        jQuery: any;
        angular: any;
        axios: any;
        xhr: any;
        profiler: StackExchange.Profiling.Profiler;
        MiniProfiler: StackExchange.Profiling.MiniProfiler;
        WebForm_ExecuteCallback(object: any): void;
    }

    interface Document {
        createStyleSheet(url: string): Function;
    }

    interface MiniProfilerProbeTiming {
        start(name: string): void;
        end(name: string): void;
        results(): { [id: string]: { start: number; end: number; } };
        flush(): void;
    }

    interface Request {
        addEvents(object: any): void;
    }

    interface JQuery {
        tmpl(object?: any): JQuery;
    }

    class MooTools { }

    //function WebForm_ExecuteCallback(object: any): void;
    // TODO: highlight.js
    function prettyPrint(): void;
}

export { };