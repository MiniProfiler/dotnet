/// <reference path="./node_modules/@types/jquery/index.d.ts">
/// <reference path="./node_modules/@types/extjs/index.d.ts">
/// <reference path="./node_modules/@types/microsoft-ajax/index.d.ts">
/// <reference path="./MiniProfiler.Globals.d.ts">

namespace StackExchange.Profiling {

    export interface IProfiler {
        Id: string;
        Name: string;
        Started: Date;
        DurationMilliseconds: number;
        MachineName: string;
        CustomLinks: { [id: string]: string };
        Root: ITiming;
        ClientTimings: IClientTimings;
        User: string;
        HasUserViewed: boolean;
        // additive on client side
        CustomTimingStats: { [id: string]: ICustomTimingStat };
        HasCustomTimings: boolean;
        HasDuplicateCustomTimings: boolean;
        HasTrivialTimings: boolean;
        AllCustomTimings: ICustomTiming[];
    }

    interface IClientTimings {
        Timings: ClientTiming[];
        RedirectCount: number;
    }

    class ClientTiming {
        public Name: string;
        public Start: number;
        public Duration: number;
        constructor(name: string, start: number, duration?: number) {
            this.Name = name;
            this.Start = start;
            this.Duration = duration;
        }
    }

    interface ITiming {
        Id: string;
        Name: string;
        DurationMilliseconds: number;
        StartMilliseconds: number;
        Children: ITiming[];
        CustomTimings: { [id: string]: ICustomTiming[] };
        // additive on client side
        CustomTimingStats: { [id: string]: ICustomTimingStat };
        DurationWithoutChildrenMilliseconds: number;
        Depth: number;
        HasCustomTimings: boolean;
        HasDuplicateCustomTimings: { [id: string]: boolean };
        IsTrivial: boolean;
        Parent: ITiming;
        // added for gaps (TODO: change all this)
        richTiming: IGapTiming[];
    }

    interface ICustomTiming {
        Id: string;
        CommandString: string;
        ExecuteType: string;
        StackTraceSnippet: string;
        StartMilliseconds: number;
        DurationMilliseconds: number;
        FirstFetchDurationMilliseconds?: number;
        Errored: boolean;
        // client side:
        Parent: ITiming;
        CallType: string;
        IsDuplicate: boolean;
        // added for gaps
        PrevGap: IGapInfo;
        NextGap: IGapInfo;
    }

    interface ICustomTimingStat {
        Count: number;
        Duration: number;
    }

    interface ITimingInfo {
        name: string;
        description: string;
        lineDescription: string;
        type: string;
        point: boolean;
    }

    interface IOptions {
        authorized: boolean;
        currentId: string;
        ids: string[];
        ignoredDuplicateExecuteTypes: string[];
        maxTracesToShow: number;
        path: string;
        renderPosition: RenderPosition;
        showChildrenTime: boolean;
        showControls: boolean;
        showTrivial: boolean;
        startHidden: boolean;
        toggleShortcut: string;
        trivialMilliseconds: number;
        version: string;
    }

    enum RenderMode {
        Corner,
        Full,
    }

    enum RenderPosition {
        Left = 'Left',
        Right = 'Right',
        BottomLeft = 'BottomLeft',
        BottomRight = 'BottomRight',
    }

    class ResultRequest {
        public Id: string;
        public Performance?: ClientTiming[];
        public Probes?: ClientTiming[];
        public RedirectCount?: number;
        constructor(id: string, perfTimings: ITimingInfo[]) {
            this.Id = id;
            if (perfTimings && window.performance && window.performance.timing) {
                const resource = window.performance.timing;
                const start = resource.fetchStart;

                this.Performance = perfTimings
                    .filter((current) => resource[current.name])
                    .map((current, i) => ({ item: current, index: i }))
                    .sort((a, b) => resource[a.item.name] - resource[b.item.name] || a.index - b.index)
                    .map((x, i, sorted) => {
                        const current = x.item;
                        const next = i + 1 < sorted.length ? sorted[i + 1].item : null;

                        return {
                            ...current,
                            ...{
                                startTime: resource[current.name] - start,
                                timeTaken: !next ? 0 : (resource[next.name] - resource[current.name]),
                            },
                        };
                    })
                    .map((item, i) => ({
                        Name: item.name,
                        Start: item.startTime,
                        Duration: item.point ? undefined : item.timeTaken,
                    }));

                if (window.performance.navigation) {
                    this.RedirectCount = window.performance.navigation.redirectCount;
                }

                if (window.mPt) {
                    const pResults = window.mPt.results();
                    this.Probes = Object.keys(pResults).map((k) => pResults[k].start && pResults[k].end
                        ? {
                            Name: k,
                            Start: pResults[k].start - start,
                            Duration: pResults[k].end - pResults[k].start,
                        } : null).filter((v) => v);
                    window.mPt.flush();
                }

                if (window.performance.getEntriesByType && window.PerformancePaintTiming) {
                    const entries = window.performance.getEntriesByType('paint');
                    let firstPaint;
                    let firstContentPaint;

                    for (const entry of entries) {
                        switch (entry.name) {
                            case 'first-paint':
                                firstPaint = new ClientTiming('firstPaintTime', Math.round(entry.startTime));
                                this.Performance.push(firstPaint);
                                break;
                            case 'first-contentful-paint':
                                firstContentPaint = new ClientTiming('firstContentfulPaintTime', Math.round(entry.startTime));
                                break;
                        }
                    }
                    if (firstPaint && firstContentPaint && firstContentPaint.Start > firstPaint.Start) {
                        this.Performance.push(firstContentPaint);
                    }

                } else if (window.chrome && window.chrome.loadTimes) {
                    // fallback to Chrome timings
                    const chromeTimes = window.chrome.loadTimes();
                    if (chromeTimes.firstPaintTime) {
                        this.Performance.push(new ClientTiming('firstPaintTime', Math.round(chromeTimes.firstPaintTime * 1000 - start)));
                    }
                    if (chromeTimes.firstPaintAfterLoadTime && chromeTimes.firstPaintAfterLoadTime > chromeTimes.firstPaintTime) {
                        this.Performance.push(new ClientTiming('firstPaintAfterLoadTime', Math.round(chromeTimes.firstPaintAfterLoadTime * 1000 - start)));
                    }
                }
            }
        }
    }

    // Gaps
    interface IGapTiming {
        start: number;
        finish: number;
        duration: number;
    }

    interface IGapInfo {
        start: number;
        finish: number;
        duration: string;
        Reason: IGapReason;
    }

    interface IGapReason {
        name: string;
        duration: number;
    }

    export class MiniProfiler {
        public options: IOptions;
        public container: JQuery;
        public controls: JQuery;
        public jq: JQueryStatic = window.jQuery.noConflict();
        public fetchStatus: { [id: string]: string } = {}; // so we never pull down a profiler twice
        public clientPerfTimings: ITimingInfo[] = [
            // { name: 'navigationStart', description: 'Navigation Start' },
            // { name: 'unloadEventStart', description: 'Unload Start' },
            // { name: 'unloadEventEnd', description: 'Unload End' },
            // { name: 'redirectStart', description: 'Redirect Start' },
            // { name: 'redirectEnd', description: 'Redirect End' },
            ({ name: 'fetchStart', description: 'Fetch Start', lineDescription: 'Fetch', point: true }) as ITimingInfo,
            ({ name: 'domainLookupStart', description: 'Domain Lookup Start', lineDescription: 'DNS Lookup', type: 'dns' }) as ITimingInfo,
            ({ name: 'domainLookupEnd', description: 'Domain Lookup End', type: 'dns' }) as ITimingInfo,
            ({ name: 'connectStart', description: 'Connect Start', lineDescription: 'Connect', type: 'connect' }) as ITimingInfo,
            ({ name: 'secureConnectionStart', description: 'Secure Connection Start', lineDescription: 'SSL/TLS Connect', type: 'ssl' }) as ITimingInfo,
            ({ name: 'connectEnd', description: 'Connect End', type: 'connect' }) as ITimingInfo,
            ({ name: 'requestStart', description: 'Request Start', lineDescription: 'Request', type: 'request' }) as ITimingInfo,
            ({ name: 'responseStart', description: 'Response Start', lineDescription: 'Response', type: 'response' }) as ITimingInfo,
            ({ name: 'responseEnd', description: 'Response End', type: 'response' }) as ITimingInfo,
            ({ name: 'domLoading', description: 'DOM Loading', lineDescription: 'DOM Loading', type: 'dom' }) as ITimingInfo,
            ({ name: 'domInteractive', description: 'DOM Interactive', lineDescription: 'DOM Interactive', type: 'dom', point: true }) as ITimingInfo,
            ({ name: 'domContentLoadedEventStart', description: 'DOM Content Loaded Event Start', lineDescription: 'DOM Content Loaded', type: 'domcontent' }) as ITimingInfo,
            ({ name: 'domContentLoadedEventEnd', description: 'DOM Content Loaded Event End', type: 'domcontent' }) as ITimingInfo,
            ({ name: 'domComplete', description: 'DOM Complete', lineDescription: 'DOM Complete', type: 'dom', point: true }) as ITimingInfo,
            ({ name: 'loadEventStart', description: 'Load Event Start', lineDescription: 'Load Event', type: 'load' }) as ITimingInfo,
            ({ name: 'loadEventEnd', description: 'Load Event End', type: 'load' }) as ITimingInfo,
            ({ name: 'firstPaintTime', description: 'First Paint', lineDescription: 'First Paint', type: 'paint', point: true }) as ITimingInfo,
            ({ name: 'firstContentfulPaintTime', description: 'First Content Paint', lineDescription: 'First Content Paint', type: 'paint', point: true }) as ITimingInfo,
        ];
        private savedJson: IProfiler[] = [];
        private path: string;
        public highlight = (elem: HTMLElement): void => undefined;
        public initCondition: () => boolean; // Example usage: window.MiniProfiler.initCondition = function() { return myOtherThingIsReady; };

        public init = (): MiniProfiler => {
            this.jq = jQuery.noConflict(true);
            const mp = this;
            const $ = this.jq;
            const script = this.jq('#mini-profiler');
            const data = script.data();

            if (!script.length) {
                 return;
            }

            this.options = {
                ids: data.ids.split(','),
                path: data.path,
                version: data.version,
                renderPosition: data.position,
                showTrivial: data.trivial,
                trivialMilliseconds: parseFloat(data.trivialMilliseconds),
                showChildrenTime: data.children,
                maxTracesToShow: data.maxTraces,
                showControls: data.controls,
                currentId: data.currentId,
                authorized: data.authorized,
                toggleShortcut: data.toggleShortcut,
                startHidden: data.startHidden,
                ignoredDuplicateExecuteTypes: (data.ignoredDuplicateExecuteTypes || '').split(','),
            };

            function doInit() {
                const initPopupView = () => {
                    if (mp.options.authorized) {
                        // all fetched profilers will go in here
                        // MiniProfiler.RenderIncludes() sets which corner to render in - default is upper left
                        mp.container = $('<div class="mp-results"/>')
                            .addClass('mp-' + mp.options.renderPosition.toLowerCase())
                            .appendTo('body');

                        // initialize the controls
                        mp.initControls(mp.container);

                        // fetch and render results
                        mp.fetchResults(mp.options.ids);

                        if (mp.options.startHidden) {
                            mp.container.hide();
                        }

                        // if any data came in before the view popped up, render now
                        if (mp.savedJson) {
                            for (const saved of mp.savedJson) {
                                mp.buttonShow(saved);
                            }
                        }
                    } else {
                        mp.fetchResults(mp.options.ids);
                    }
                };

                // when rendering a shared, full page, this div will exist
                mp.container = $('.mp-result-full');
                if (mp.container.length) {
                    // Full page view
                    if (window.location.href.indexOf('&trivial=1') > 0) {
                        mp.options.showTrivial = true;
                    }

                    // profiler will be defined in the full page's head
                    window.profiler.Started = new Date('' + window.profiler.Started); // Ugh, JavaScript
                    mp.renderProfiler(window.profiler).appendTo(mp.container);

                    // highight
                    $('pre code').each((i, block) => mp.highlight(block));

                    mp.bindDocumentEvents(RenderMode.Full);
                } else {
                    initPopupView();
                    mp.bindDocumentEvents(RenderMode.Corner);
                }
            }

            let wait = 0;
            let alreadyDone = false;
            const deferInit = () => {
                if (!alreadyDone) {
                    if ((mp.initCondition && !mp.initCondition())
                        || (window.performance && window.performance.timing && window.performance.timing.loadEventEnd === 0 && wait < 10000)) {
                        setTimeout(deferInit, 100);
                        wait += 100;
                    } else {
                        alreadyDone = true;
                        if (mp.options.authorized) {
                            $('head').append(`<link rel="stylesheet" type="text/css" href="${mp.options.path}includes.min.css?v=${mp.options.version}" />`);
                        }
                        doInit();
                    }
                }
            };

            $(mp.installAjaxHandlers);
            $(deferInit);

            return this;
        }

        public listInit = (options: IOptions) => {
            const mp = this;
            const $ = mp.jq;
            const opt = this.options = options || {} as IOptions;

            function updateGrid(id?: string) {
                const getTiming = (profiler: IProfiler, name: string) =>
                    profiler.ClientTimings.Timings.filter((t) => t.Name === name)[0] || { Name: name, Duration: '', Start: '' };

                $.ajax({
                    url: opt.path + 'results-list',
                    data: { 'last-id': id },
                    dataType: 'json',
                    type: 'GET',
                    success: (data: IProfiler[]) => {
                        let str = '';
                        data.forEach((profiler) => {
                            str += (`
<tr>
  <td><a href="${options.path}results?id=${profiler.Id}">${mp.htmlEscape(profiler.Name)}</a></td>
  <td>${mp.htmlEscape(profiler.MachineName)}</td>
  <td class="mp-results-index-date">${profiler.Started}</td>
  <td>${profiler.DurationMilliseconds}</td>` + (profiler.ClientTimings ? `
  <td>${getTiming(profiler, 'requestStart').Start}</td>
  <td>${getTiming(profiler, 'responseStart').Start}</td>
  <td>${getTiming(profiler, 'domComplete').Start}</td> ` : `
  <td colspan="3" class="mp-results-none">(no client timings)</td>`) + `
</tr>`);
                        });
                        $('table tbody').append(str);
                        const oldId = id;
                        const oldData = data;
                        setTimeout(() => {
                            let newId = oldId;
                            if (oldData.length > 0) {
                                newId = oldData[oldData.length - 1].Id;
                            }
                            updateGrid(newId);
                        }, 4000);
                    },
                });
            }
            updateGrid();
        }

        private fetchResults = (ids: string[]) => {
            for (let i = 0; ids && i < ids.length; i++) {
                const id = ids[i];
                const request = new ResultRequest(id, id === this.options.currentId ? this.clientPerfTimings : null);
                const mp = this;

                if (mp.fetchStatus.hasOwnProperty(id)) {
                    continue; // already fetching
                }

                const isoDate = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*))(?:Z|(\+|-)([\d|:]*))?$/;
                const parseDates = (key: string, value: any) =>
                          key === 'Started' && typeof value === 'string' && isoDate.exec(value) ? new Date(value) : value;

                mp.fetchStatus[id] = 'Starting fetch';
                this.jq.ajax({
                    url: this.options.path + 'results',
                    data: JSON.stringify(request),
                    dataType: 'json',
                    contentType: 'application/json',
                    type: 'POST',
                    converters: {
                        'text json': (result) => JSON.parse(result, parseDates),
                    },
                    success: (json: IProfiler | string) => {
                        mp.fetchStatus[id] = 'Fetch succeeded';
                        if (json instanceof String) {
                            // hidden
                        } else {
                            mp.buttonShow(json as IProfiler);
                        }
                    },
                    complete: () => {
                        mp.fetchStatus[id] = 'Fetch complete';
                    },
                });
            }
        }

        private processJson = (profiler: IProfiler) => {
            const result: IProfiler = { ...profiler };
            const mp = this;

            result.CustomTimingStats = {};
            result.CustomLinks = result.CustomLinks || {};
            result.AllCustomTimings = [];

            function processTiming(timing: ITiming, parent: ITiming, depth: number) {
                timing.DurationWithoutChildrenMilliseconds = timing.DurationMilliseconds;
                timing.Parent = parent;
                timing.Depth = depth;
                timing.HasDuplicateCustomTimings = {};

                for (const child of timing.Children || []) {
                    processTiming(child, timing, depth + 1);
                    timing.DurationWithoutChildrenMilliseconds -= child.DurationMilliseconds;
                }

                // do this after subtracting child durations
                if (timing.DurationWithoutChildrenMilliseconds < mp.options.trivialMilliseconds) {
                    timing.IsTrivial = true;
                    result.HasTrivialTimings = true;
                }

                function ignoreDuplicateCustomTiming(customTiming: ICustomTiming) {
                    return customTiming.ExecuteType && mp.options.ignoredDuplicateExecuteTypes.indexOf(customTiming.ExecuteType) > -1;
                }

                if (timing.CustomTimings) {
                    timing.CustomTimingStats = {};
                    timing.HasCustomTimings = true;
                    result.HasCustomTimings = true;
                    for (const customType of Object.keys(timing.CustomTimings)) {
                        const customTimings = timing.CustomTimings[customType];
                        const customStat = {
                                  Duration: 0,
                                  Count: 0,
                              };
                        const duplicates: { [id: string]: boolean } = {};
                        for (const customTiming of customTimings) {
                            // Add to the overall list for the queries view
                            result.AllCustomTimings.push(customTiming);
                            customTiming.Parent = timing;
                            customTiming.CallType = customType;

                            customStat.Duration += customTiming.DurationMilliseconds;

                            const ignored = ignoreDuplicateCustomTiming(customTiming);
                            if (!ignored) {
                                customStat.Count++;
                            }

                            if (customTiming.CommandString && duplicates[customTiming.CommandString]) {
                                customTiming.IsDuplicate = true;
                                timing.HasDuplicateCustomTimings[customType] = true;
                                result.HasDuplicateCustomTimings = true;
                            } else if (!ignored) {
                                duplicates[customTiming.CommandString] = true;
                            }
                        }
                        timing.CustomTimingStats[customType] = customStat;
                        if (!result.CustomTimingStats[customType]) {
                            result.CustomTimingStats[customType] = {
                                Duration: 0,
                                Count: 0,
                            };
                        }
                        result.CustomTimingStats[customType].Duration += customStat.Duration;
                        result.CustomTimingStats[customType].Count += customStat.Count;
                    }
                } else {
                    timing.CustomTimings = {};
                }
            }

            processTiming(result.Root, null, 0);
            this.processCustomTimings(result);

            return result;
        }

        private processCustomTimings = (profiler: IProfiler) => {
            const result = profiler.AllCustomTimings;

            result.sort((a, b) => a.StartMilliseconds - b.StartMilliseconds);

            function removeDuration(list: IGapTiming[], duration: IGapTiming) {

                const newList: IGapTiming[] = [];
                for (const item of list) {
                    if (duration.start > item.start) {
                        if (duration.start > item.finish) {
                            newList.push(item);
                            continue;
                        }
                        newList.push(({ start: item.start, finish: duration.start }) as IGapTiming);
                    }

                    if (duration.finish < item.finish) {
                        if (duration.finish < item.start) {
                            newList.push(item);
                            continue;
                        }
                        newList.push(({ start: duration.finish, finish: item.finish }) as IGapTiming);
                    }
                }

                return newList;
            }

            function processTimes(elem: ITiming) {
                const duration = ({ start: elem.StartMilliseconds, finish: (elem.StartMilliseconds + elem.DurationMilliseconds) }) as IGapTiming;
                elem.richTiming = [duration];
                if (elem.Parent != null) {
                    elem.Parent.richTiming = removeDuration(elem.Parent.richTiming, duration);
                }

                for (const child of elem.Children || []) {
                    processTimes(child);
                }
            }

            processTimes(profiler.Root);
            // sort results by time
            result.sort((a, b) => a.StartMilliseconds - b.StartMilliseconds);

            function determineOverlap(gap: IGapInfo, node: ITiming) {
                let overlap = 0;
                for (const current of node.richTiming) {
                    if (current.start > gap.finish) {
                        break;
                    }
                    if (current.finish < gap.start) {
                        continue;
                    }

                    overlap += Math.min(gap.finish, current.finish) - Math.max(gap.start, current.start);
                }
                return overlap;
            }

            function determineGap(gap: IGapInfo, node: ITiming, match: IGapReason) {
                const overlap = determineOverlap(gap, node);
                if (match == null || overlap > match.duration) {
                    match = { name: node.Name, duration: overlap };
                } else if (match.name === node.Name) {
                    match.duration += overlap;
                }

                for (const child of node.Children || []) {
                    match = determineGap(gap, child, match);
                }
                return match;
            }

            let time = 0;
            let prev = null;
            result.forEach((elem) => {
                elem.PrevGap = {
                    duration: (elem.StartMilliseconds - time).toFixed(2),
                    start: time,
                    finish: elem.StartMilliseconds,
                } as IGapInfo;

                elem.PrevGap.Reason = determineGap(elem.PrevGap, profiler.Root, null);

                time = elem.StartMilliseconds + elem.DurationMilliseconds;
                prev = elem;
            });


            if (result.length > 0) {
                const me = result[result.length - 1];
                me.NextGap = {
                    duration: (profiler.Root.DurationMilliseconds - time).toFixed(2),
                    start: time,
                    finish: profiler.Root.DurationMilliseconds,
                } as IGapInfo;
                me.NextGap.Reason = determineGap(me.NextGap, profiler.Root, null);
            }

            return result;
        }

        private htmlEscape = (orig: string) => (orig || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;')

        private renderProfiler = (json: IProfiler) => {
            const p = this.processJson(json);
            const mp = this;
            const encode = this.htmlEscape;
            const duration = (milliseconds: number | undefined, decimalPlaces?: number) => {
                if (milliseconds === undefined) {
                    return '';
                }
                return (milliseconds || 0).toFixed(decimalPlaces === undefined ? 1 : decimalPlaces);
            };

            const renderTiming = (timing: ITiming) => {
                const customTimingTypes = p.CustomTimingStats ? Object.keys(p.CustomTimingStats) : [];
                let str = `
  <tr class="${timing.IsTrivial ? 'mp-trivial' : ''}" data-timing-id="${timing.Id}">
    <td class="mp-label" title="${encode(timing.Name && timing.Name.length > 45 ? timing.Name : '')}"${timing.Depth > 0 ? ` style="padding-left:${timing.Depth * 11}px;"` : ''}>
      ${encode(timing.Name.slice(0, 45))}${encode(timing.Name && timing.Name.length > 45 ? '...' : '')}
    </td>
    <td class="mp-duration" title="duration of this step without any children's durations">
      ${duration(timing.DurationWithoutChildrenMilliseconds)}
    </td>
    <td class="mp-duration mp-more-columns" title="duration of this step and its children">
      ${duration(timing.DurationMilliseconds)}
    </td>
    <td class="mp-duration mp-more-columns time-from-start" title="time elapsed since profiling started">
      <span class="mp-unit">+</span>${duration(timing.StartMilliseconds)}
    </td>
    ${customTimingTypes.map((tk) => timing.CustomTimings[tk] ? `
    <td class="mp-duration">
      <a class="mp-queries-show" title="${duration(timing.CustomTimingStats[tk].Duration)} ms in ${timing.CustomTimingStats[tk].Count} ${encode(tk)} call(s)${timing.HasDuplicateCustomTimings[tk] ? '; duplicate calls detected!' : ''}">
        ${duration(timing.CustomTimingStats[tk].Duration)}
        (${timing.CustomTimingStats[tk].Count}${(timing.HasDuplicateCustomTimings[tk] ? '<span class="mp-warning">!</span>' : '')})
      </a>
    </td>` : '<td></td>').join('')}
  </tr>`;
                // Append children
                if (timing.Children) {
                    timing.Children.forEach((ct) => str += renderTiming(ct));
                }
                return str;
            };

            const timingsTable = `
        <table class="mp-timings">
          <thead>
            <tr>
              <th></th>
              <th>duration (ms)</th>
              <th class="mp-more-columns">with children (ms)</th>
              <th class="time-from-start mp-more-columns">from start (ms)</th>
              ${Object.keys(p.CustomTimingStats).map((k) => `<th title="call count">${encode(k)} (ms)</th>`).join('')}
            </tr>
          </thead>
          <tbody>
            ${renderTiming(p.Root)}
          </tbody>
          <tfoot>
            <tr>
              <td colspan="2"></td>
              <td class="mp-more-columns" colspan="2"></td>
            </tr>
          </tfoot>
        </table>`;

            const customTimings = () => {
                if (!p.HasCustomTimings) {
                    return '';
                }
                return `
        <table class="mp-custom-timing-overview">
            ${Object.getOwnPropertyNames(p.CustomTimingStats).map((key) => `
          <tr title="${p.CustomTimingStats[key].Count} ${encode(key.toLowerCase())} calls spent ${duration(p.CustomTimingStats[key].Duration)} ms of total request time">
            <td class="mp-number">
              ${encode(key)}:
            </td>
            <td class="mp-number">
              ${duration(p.CustomTimingStats[key].Duration / p.DurationMilliseconds * 100)} <span class="mp-unit">%</span>
            </td>
          </tr>`).join('')}
        </table>`;
            };

            function clientTimings() {
                if (!p.ClientTimings) {
                    return '';
                }

                let end = 0;
                const list = p.ClientTimings.Timings.map((t) => {
                    const results = mp.clientPerfTimings ? mp.clientPerfTimings.filter((pt: ITimingInfo) => pt.name === t.Name) : [];
                    const info: ITimingInfo = results.length > 0 ? results[0] : null;
                    end = Math.max(end, t.Start + t.Duration);

                    return {
                        isTrivial: t.Start === 0 || t.Duration < 2, // all points are considered trivial
                        name: info && info.lineDescription || t.Name,
                        duration: info && info.point ? undefined : t.Duration,
                        type: info && info.type || 'unknown',
                        point: info && info.point,
                        start: t.Start,
                        left: null,
                        width: null,
                    };
                });
                p.HasTrivialTimings = p.HasTrivialTimings || list.some((t) => t.isTrivial);

                list.sort((a, b) => a.start - b.start);
                list.forEach((l) => {
                    const percent = (100 * l.start / end) + '%';
                    l.left = l.point ? `calc(${percent} - 2px)` : percent;
                    l.width = l.point ? '4px' : (100 * l.duration / end + '%');
                });

                return `
        <table class="mp-timings mp-client-timings">
          <thead>
            <tr>
              <th style="text-align:left">client event</th>
              <th></th>
              <th>duration (ms)</th>
              <th class="mp-more-columns">from start (ms)</th>
            </tr>
          </thead>
          <tbody>
            ${list.map((t) => `
            <tr class="${(t.isTrivial ? 'mp-trivial' : '')}">
              <td class="mp-label">${encode(t.name)}</td>
              <td class="t-${t.type}${t.point ? ' t-point' : ''}"><div style="margin-left: ${t.left}; width: ${t.width};"></div></td>
              <td class="mp-duration">
                ${(t.duration >= 0 ? `<span class="mp-unit"></span>${duration(t.duration, 0)}` : '')}
              </td>
              <td class="mp-duration time-from-start mp-more-columns">
                <span class="mp-unit">+</span>${duration(t.start, 0)}
              </td>
            </tr>`).join('')}
          </tbody>
        </table>`;
            }

            function profilerQueries() {
                if (!p.HasCustomTimings) {
                    return '';
                }

                const renderGap = (gap: IGapInfo) => gap && gap.Reason.duration > 0.02 ? `
  <tr class="mp-gap-info ${(gap.Reason.duration < 4 ? 'mp-trivial-gap' : '')}">
    <td class="mp-info">
      ${gap.duration} <span class="mp-unit">ms</span>
    </td>
    <td class="query">
      <div>${encode(gap.Reason.name)} &mdash; ${gap.Reason.duration.toFixed(2)} <span class="mp-unit">ms</span></div>
    </td>
  </tr>` : '';

                return `
    <div class="mp-queries">
      <table>
        <thead>
          <tr>
            <th>
              <div class="mp-call-type">Call Type</div>
              <div>Step</div>
              <div>Duration <span class="mp-unit">(from start)</span></div>
            </th>
            <th>
              <div class="mp-stack-trace">Call Stack</div>
              <div>Command</div>
            </th>
          </tr>
        </thead>
        <tbody>
          ${p.AllCustomTimings.map((ct, index) => `
            ${renderGap(ct.PrevGap)}
            <tr class="${(index % 2 === 1 ? 'mp-odd' : '')}" data-timing-id="${ct.Parent.Id}">
              <td>
                <div class="mp-call-type">${encode(ct.CallType)}${encode(!ct.ExecuteType || ct.CallType === ct.ExecuteType ? '' : ' - ' + ct.ExecuteType)}${(ct.IsDuplicate ? ' <span class="mp-warning" title="Duplicate">!</span>' : '')}</div>
                <div>${encode(ct.Parent.Name)}</div>
                <div class="mp-number">
                  ${duration(ct.DurationMilliseconds)} <span class="mp-unit">ms (T+${duration(ct.StartMilliseconds)} ms)</span>
                </div>
                ${(ct.FirstFetchDurationMilliseconds ? `<div>First Result: ${duration(ct.DurationMilliseconds)} <span class="mp-unit">ms</span></div>` : '')}
              </td>
              <td>
                <div class="query">
                  <div class="mp-stack-trace">${encode(ct.StackTraceSnippet)}</div>
                  <pre><code>${encode(ct.CommandString)}</code></pre>
                </div>
              </td>
            </tr>
            ${renderGap(ct.NextGap)}`).join('')}
        </tbody>
      </table>
      <p class="mp-trivial-gap-container">
        <a class="mp-toggle-trivial-gaps" href="#">toggle trivial gaps</a>
      </p>
    </div>`;
            }

            return mp.jq(`
  <div class="mp-result${(this.options.showTrivial ? ' show-trivial' : '')}${(this.options.showChildrenTime ? ' show-columns' : '')}">
    <div class="mp-button" title="${encode(p.Name)}">
      <span class="mp-number">${duration(p.DurationMilliseconds)} <span class="mp-unit">ms</span></span>
      ${(p.HasDuplicateCustomTimings ? '<span class="mp-warning">!</span>' : '')}
    </div>
    <div class="mp-popup">
      <div class="mp-info">
        <div>
          <div class="mp-name">${encode(p.Name)}</div>
          <div class="mp-machine-name">${encode(p.MachineName)}</div>
        </div>
        <div>
          <div class="mp-overall-duration">(${duration(p.DurationMilliseconds)} ms)</div>
          <div class="mp-started">${p.Started ? p.Started.toUTCString() : ''}</div>
        </div>
      </div>
      <div class="mp-output">
        ${timingsTable}
		${customTimings()}
        ${clientTimings()}
        <div class="mp-links">
          <a href="${this.options.path}results?id=${p.Id}" class="mp-share-mp-results" target="_blank">share</a>
          ${Object.keys(p.CustomLinks).map((k) => `<a href="${p.CustomLinks[k]}" class="mp-custom-link" target="_blank">${k}</a>`).join('')}
  		  <span>
            <a class="mp-toggle-columns" title="shows additional columns">more columns</a>
            <a class="mp-toggle-columns mp-more-columns" title="hides additional columns">fewer columns</a>
            ${(p.HasTrivialTimings ? `
            <a class="mp-toggle-trivial" title="shows any rows with &lt; ${this.options.trivialMilliseconds} ms duration">show trivial</a>
            <a class="mp-toggle-trivial mp-trivial" title="hides any rows with &lt; ${this.options.trivialMilliseconds} ms duration">hide trivial</a>` : '')}
          </span>
        </div>
      </div>
    </div>
    ${profilerQueries()}
  </div>`);
        }

        private buttonShow = (json: IProfiler) => {
            if (!this.container) {
                // container not rendered yet
                this.savedJson.push(json);
                return;
            }

            const result = this.renderProfiler(json).addClass('new');

            if (this.controls) {
                result.insertBefore(this.controls);
            } else {
                result.appendTo(this.container);
            }

            // limit count to maxTracesToShow, remove those before it
            this.container.find('.mp-result:lt(' + -this.options.maxTracesToShow + ')').remove();
        }

        private scrollToQuery = (link: JQuery, queries: JQuery, whatToScroll: JQuery) => {
            const id = link.closest('tr').data('timing-id');
            const rows = queries.find('tr[data-timing-id="' + id + '"]').addClass('highlight');

            // ensure they're in view
            whatToScroll.scrollTop(whatToScroll.scrollTop() + rows.position().top - 100);
        }

        // some elements want to be hidden on certain doc events
        private bindDocumentEvents = (mode: RenderMode) => {
            const mp = this;
            const $ = this.jq;
            // Common handlers
            $(document)
                .on('click', '.mp-toggle-trivial', function(e) {
                    e.preventDefault();
                    $(this).closest('.mp-result').toggleClass('show-trivial');
                }).on('click', '.mp-toggle-columns', function(e) {
                    e.preventDefault();
                    $(this).closest('.mp-result').toggleClass('show-columns');
                }).on('click', '.mp-toggle-trivial-gaps', function(e) {
                    e.preventDefault();
                    $(this).closest('.mp-queries').find('.mp-trivial-gap').toggle();
                });

            // Full vs. Corner handlers
            if (mode === RenderMode.Full) {
                // since queries are already shown, just highlight and scroll when clicking a '1 sql' link
                $(document).on('click', '.mp-popup .mp-queries-show', function() {
                    mp.scrollToQuery($(this), $('.mp-queries'), $(document));
                });
            } else {
                $(document)
                    .on('click', '.mp-button', function(e) {
                        const button = $(this);
                        const popup = button.siblings('.mp-popup');
                        const wasActive = button.parent().hasClass('active');
                        const pos = mp.options.renderPosition;

                        button.parent().removeClass('new').toggleClass('active')
                            .siblings('.active').removeClass('active');

                        if (!wasActive) {
                            // move left or right, based on config
                            popup.css(pos === RenderPosition.Left || pos === RenderPosition.BottomLeft ? 'left' : 'right', button.outerWidth() - 1);

                            // is this rendering on the bottom (if no, then is top by default)
                            if (pos === RenderPosition.BottomLeft || pos === RenderPosition.BottomRight) {
                                const bottom = $(window).height() - button.offset().top - button.outerHeight() + $(window).scrollTop(); // get bottom of button
                                popup.css({ 'bottom': 0, 'max-height': 'calc(100vh - ' + (bottom + 25) + 'px)' });
                            } else {
                                popup.css({ 'top': 0, 'max-height': 'calc(100vh - ' + (button.offset().top - $(window).scrollTop() + 25) + 'px)' });
                            }
                        }
                    }).on('click', '.mp-queries-show', function(e) {
                        // opaque background
                        const overlay = $('<div class="mp-overlay"><div class="mp-overlay-bg"/></div>').appendTo('body');
                        const queries = $(this).closest('.mp-result').find('.mp-queries').clone().appendTo(overlay).show();

                        mp.scrollToQuery($(this), queries, queries);

                        // syntax highlighting
                        queries.find('pre code').each((i, block) => mp.highlight(block));
                    }).on('click keyup', (e) => {
                        const active = $('.mp-result.active');
                        if (active.length) {
                            const bg = $('.mp-overlay');
                            const isEscPress = e.type === 'keyup' && e.which === 27;
                            const isBgClick = e.type === 'click' && !$(e.target).closest('.mp-queries, .mp-results').length;

                            if (isEscPress || isBgClick) {
                                if (bg.is(':visible')) {
                                    bg.remove();
                                } else {
                                    active.removeClass('active');
                                }
                            }
                        }
                    });
                if (mp.options.toggleShortcut && !mp.options.toggleShortcut.match(/^None$/i)) {
                    $(document).bind('keydown', mp.options.toggleShortcut, (e) => $('.mp-results').toggle());
                }
            }
        }

        private initControls = (container: JQuery) => {
            const $ = this.jq;
            if (this.options.showControls) {
                this.controls = $('<div class="mp-controls"><span class="mp-min-max">m</span><span class="mp-clear">c</span></div>').appendTo(container);

                $('.mp-controls .mp-min-max').click(() => container.toggleClass('mp-min'));

                container.hover(
                    function() {
                        if ($(this).hasClass('mp-min')) {
                            $(this).find('.mp-min-max').show();
                        }
                    },
                    function() {
                        if ($(this).hasClass('mp-min')) {
                            $(this).find('.mp-min-max').hide();
                        }
                    });

                $('.mp-controls .mp-clear').click(() => container.find('.mp-result').remove());
            } else {
                container.addClass('mp-no-controls');
            }
        }

        private installAjaxHandlers = () => {
            const mp = this;

            function handleIds(jsonIds: string) {
                if (jsonIds) {
                    const ids: string[] = JSON.parse(jsonIds);
                    mp.fetchResults(ids);
                }
            }

            function handleXHR(xhr: XMLHttpRequest | JQuery.jqXHR | Sys.Net.WebRequestExecutor) {
                // iframed file uploads don't have headers
                if (xhr && xhr.getResponseHeader) {
                    // should be an array of strings, e.g. ["008c4813-9bd7-443d-9376-9441ec4d6a8c","16ff377b-8b9c-4c20-a7b5-97cd9fa7eea7"]
                    handleIds(xhr.getResponseHeader('X-MiniProfiler-Ids'));
                }
            }

            // we need to attach our AJAX complete handler to the window's (profiled app's) copy, not our internal, no conflict version
            const window$ = window.jQuery;

            // fetch profile results for any AJAX calls
            if (window$ && window$(document) && window$(document).ajaxComplete) {
                window$(document).ajaxComplete((e, xhr, settings) => handleXHR(xhr));
            }

            // fetch results after ASP Ajax calls
            if (typeof (Sys) !== 'undefined' && typeof (Sys.WebForms) !== 'undefined' && typeof (Sys.WebForms.PageRequestManager) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest((sender, args) => {
                    if (args) {
                        const response = args.get_response() as any; // Trust me, it's there.
                        if (response.get_responseAvailable() && response._xmlHttpRequest != null) {
                            handleXHR(response);
                        }
                    }
                });
            }

            if (typeof (Sys) !== 'undefined' && typeof (Sys.Net) !== 'undefined' && typeof (Sys.Net.WebRequestManager) !== 'undefined') {
                Sys.Net.WebRequestManager.add_completedRequest((sender, args) => {
                    if (sender) {
                        const webRequestExecutor = sender;
                        if (webRequestExecutor.get_responseAvailable()) {
                            handleXHR(webRequestExecutor);
                        }
                    }
                });
            }

            // more Asp.Net callbacks
            if (typeof (window.WebForm_ExecuteCallback) === 'function') {
                window.WebForm_ExecuteCallback = ((callbackObject: { xmlRequest: XMLHttpRequest }) => {
                    // Store original function
                    const original = window.WebForm_ExecuteCallback;

                    return (callbackObjectInner: { xmlRequest: XMLHttpRequest }) => {
                        original(callbackObjectInner);
                        handleXHR(callbackObjectInner.xmlRequest);
                    };
                })(null);
            }

            // also fetch results after ExtJS requests, in case it is being used
            if (typeof (Ext) !== 'undefined' && typeof (Ext.Ajax) !== 'undefined' && typeof (Ext.Ajax.on) !== 'undefined') {
                // Ext.Ajax is a singleton, so we just have to attach to its 'requestcomplete' event
                Ext.Ajax.on('requestcomplete', (e: any, xhr: XMLHttpRequest, settings: any) => handleXHR(xhr));
            }

            if (typeof (MooTools) !== 'undefined' && typeof (Request) !== 'undefined') {
                Request.prototype.addEvents({
                    onComplete() {
                        handleXHR(this.xhr);
                    },
                });
            }

            // add support for AngularJS, which uses the basic XMLHttpRequest object.
            if ((window.angular || window.axios || window.xhr) && typeof (XMLHttpRequest) !== 'undefined') {
                const oldSend = XMLHttpRequest.prototype.send;

                XMLHttpRequest.prototype.send = function sendReplacement(data) {
                    if (this.onreadystatechange) {
                        if (typeof (this.miniprofiler) === 'undefined' || typeof (this.miniprofiler.prev_onreadystatechange) === 'undefined') {
                            this.miniprofiler = { prev_onreadystatechange: this.onreadystatechange };

                            this.onreadystatechange = function onReadyStateChangeReplacement() {
                                if (this.readyState === 4) {
                                    handleXHR(this);
                                }

                                if (this.miniprofiler.prev_onreadystatechange != null) {
                                    return this.miniprofiler.prev_onreadystatechange.apply(this, arguments);
                                }
                            };
                        }
                    } else if (this.onload) {
                        if (typeof (this.miniprofiler) === 'undefined' || typeof (this.miniprofiler.prev_onload) === 'undefined') {
                            this.miniprofiler = { prev_onload: this.onload };

                            this.onload = function onLoadReplacement() {
                                handleXHR(this);

                                if (this.miniprofiler.prev_onload != null) {
                                    return this.miniprofiler.prev_onload.apply(this, arguments);
                                }
                            };
                        }
                    }

                    return oldSend.apply(this, arguments);
                };
            }

            // wrap fetch
            if (window.fetch) {
                const windowFetch = window.fetch;
                window.fetch = (input, init) => {
                    return windowFetch(input, init).then((response) => {
                        handleIds(response.headers.get('X-MiniProfiler-Ids'));
                        return response;
                    });
                };
            }
        }
    }
}

window.MiniProfiler = new StackExchange.Profiling.MiniProfiler().init();
