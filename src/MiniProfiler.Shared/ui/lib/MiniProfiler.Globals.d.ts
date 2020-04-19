declare global {
    interface Window {
        profiler: StackExchange.Profiling.IProfiler;
        MiniProfiler: StackExchange.Profiling.MiniProfiler;
        PerformancePaintTiming: Function;
        mPt: MiniProfilerProbeTiming;
        chrome: {
            loadTimes(): {
                firstPaintTime: number;
                firstPaintAfterLoadTime: number;
            }
        };
        // We only check for these existing to hook up xhr events...we need to know noting about them.
        jQuery: any;
        angular: any;
        axios: any;
        xhr: any;
        WebForm_ExecuteCallback(callbackObject: { xmlRequest: XMLHttpRequest }): void;
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
        addEvents(object: {onComplete: Function}): void;
    }

    class MooTools { }
}

export { };