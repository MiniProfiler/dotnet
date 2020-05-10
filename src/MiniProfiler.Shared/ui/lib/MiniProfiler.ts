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
        HasWarning: boolean;
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

    enum ColorScheme {
        Light = 'Light',
        Dark = 'Dark',
        Auto = 'Auto',
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
        HasWarnings: { [id: string]: boolean };
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
        colorScheme: ColorScheme;
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
        public container: HTMLDivElement;
        public controls: HTMLDivElement;
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
        public highlight = (elem: HTMLElement): void => undefined;
        public initCondition: () => boolean; // Example usage: window.MiniProfiler.initCondition = function() { return myOtherThingIsReady; };

        public init = (): MiniProfiler => {
            const mp = this;
            const script = document.getElementById('mini-profiler');
            const data = script.dataset;
            let wait = 0;
            let alreadyDone = false;

            if (!script || !window.fetch) {
                return;
            }

            const bool = (arg: string) => arg === 'true';

            this.options = {
                ids: (data.ids || '').split(','),
                path: data.path,
                version: data.version,
                renderPosition: data.position as RenderPosition,
                colorScheme: data.scheme as ColorScheme,
                showTrivial: bool(data.trivial),
                trivialMilliseconds: parseFloat(data.trivialMilliseconds),
                showChildrenTime: bool(data.children),
                maxTracesToShow: parseInt(data.maxTraces, 10),
                showControls: bool(data.controls),
                currentId: data.currentId,
                authorized: bool(data.authorized),
                toggleShortcut: data.toggleShortcut,
                startHidden: bool(data.startHidden),
                ignoredDuplicateExecuteTypes: (data.ignoredDuplicateExecuteTypes || '').split(','),
            };

            function doInit() {
                const initPopupView = () => {
                    if (mp.options.authorized) {
                        // all fetched profilers will go in here
                        // MiniProfiler.RenderIncludes() sets which corner to render in - default is upper left
                        const container = document.createElement('div');
                        container.className = 'mp-results mp-' + mp.options.renderPosition.toLowerCase() + ' mp-scheme-' + mp.options.colorScheme.toLowerCase()
                        document.body.appendChild(container);
                        mp.container = container;

                        // initialize the controls
                        mp.initControls(mp.container);

                        // fetch and render results
                        mp.fetchResults(mp.options.ids);

                        if (mp.options.startHidden) {
                            mp.container.style.display = 'none';
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
                const fullResults = document.getElementsByClassName('mp-result-full');
                if (fullResults.length > 0) {
                    mp.container = fullResults[0] as HTMLDivElement;

                    // Full page view
                    if (window.location.href.indexOf('&trivial=1') > 0) {
                        mp.options.showTrivial = true;
                    }

                    // profiler will be defined in the full page's head
                    window.profiler.Started = new Date('' + window.profiler.Started); // Ugh, JavaScript
                    const profilerHtml = mp.renderProfiler(window.profiler, false);
                    mp.container.insertAdjacentHTML('beforeend', profilerHtml);

                    // highight
                    mp.container.querySelectorAll('pre code').forEach(block => mp.highlight(block as HTMLElement));

                    mp.bindDocumentEvents(RenderMode.Full);
                } else {
                    initPopupView();
                    mp.bindDocumentEvents(RenderMode.Corner);
                }
            }

            function deferInit() {
                if (!alreadyDone) {
                    if ((mp.initCondition && !mp.initCondition())
                        || (window.performance && window.performance.timing && window.performance.timing.loadEventEnd === 0 && wait < 10000)) {
                        setTimeout(deferInit, 100);
                        wait += 100;
                    } else {
                        alreadyDone = true;
                        if (mp.options.authorized) {
                            document.head.insertAdjacentHTML('beforeend', `<link rel="stylesheet" type="text/css" href="${mp.options.path}includes.min.css?v=${mp.options.version}" />`);
                        }
                        doInit();
                    }
                }
            };

            function onLoad() {
                mp.installAjaxHandlers();
                deferInit();
            }

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', onLoad);
            }
            else {
                onLoad();
            }

            return this;
        }

        public listInit = (options: IOptions) => {
            const mp = this;
            const opt = this.options = options || {} as IOptions;

            function updateGrid(id?: string) {
                const getTiming = (profiler: IProfiler, name: string) =>
                    profiler.ClientTimings.Timings.filter((t) => t.Name === name)[0] || { Name: name, Duration: '', Start: '' };

                document.documentElement.classList.add('mp-scheme-' + opt.colorScheme.toLowerCase());
                fetch(opt.path + 'results-list?last-id=' + id, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    }
                })
                    .then(data => data.json())
                    .then((data: IProfiler[]) => {
                        let html = '';
                        data.forEach((profiler) => {
                            html += (`
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
                        document.querySelector('.mp-results-index').insertAdjacentHTML('beforeend', html);
                        const oldId = id;
                        const oldData = data;
                        setTimeout(() => {
                            let newId = oldId;
                            if (oldData.length > 0) {
                                newId = oldData[oldData.length - 1].Id;
                            }
                            updateGrid(newId);
                        }, 4000);
                    })
            }
            updateGrid();
        }

        private fetchResults = (ids: string[]) => {
            for (let i = 0; ids && i < ids.length; i++) {
                const id = ids[i];
                const request = new ResultRequest(id, id === this.options.currentId ? this.clientPerfTimings : null);
                const mp = this;

                if (!id || mp.fetchStatus.hasOwnProperty(id)) {
                    continue; // empty id or already fetching
                }

                const isoDate = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*)?)(?:Z|(\+|-)([\d|:]*))?$/;
                const parseDates = (key: string, value: any) =>
                    key === 'Started' && typeof value === 'string' && isoDate.exec(value) ? new Date(value) : value;

                mp.fetchStatus[id] = 'Starting fetch';

                fetch(this.options.path + 'results', {
                    method: 'POST',
                    body: JSON.stringify(request),
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    }
                })
                    .then(data => data.text())
                    .then(text => JSON.parse(text, parseDates))
                    .then(json => {
                        mp.fetchStatus[id] = 'Fetch succeeded';
                        if (json instanceof String) {
                            // hidden
                        } else {
                            mp.buttonShow(json as IProfiler);
                        }
                        mp.fetchStatus[id] = 'Fetch complete';
                    })
                    .catch(function (error) {
                        mp.fetchStatus[id] = 'Fetch complete';
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
                timing.HasWarnings = {};

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
                            if (customTiming.Errored) {
                                timing.HasWarnings[customType] = true;
                                result.HasWarning = true;
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
            result.forEach((elem) => {
                elem.PrevGap = {
                    duration: (elem.StartMilliseconds - time).toFixed(2),
                    start: time,
                    finish: elem.StartMilliseconds,
                } as IGapInfo;

                elem.PrevGap.Reason = determineGap(elem.PrevGap, profiler.Root, null);

                time = elem.StartMilliseconds + elem.DurationMilliseconds;
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

        private renderProfiler = (json: IProfiler, isNew: boolean) => {
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
    <td class="mp-label" title="${encode(timing.Name)}"${timing.Depth > 0 ? ` style="padding-left:${timing.Depth * 11}px;"` : ''}>
      ${encode(timing.Name)}
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
      <a class="mp-queries-show${(timing.HasWarnings[tk] ? ' mp-queries-warning' : '')}" title="${duration(timing.CustomTimingStats[tk].Duration)} ms in ${timing.CustomTimingStats[tk].Count} ${encode(tk)} call(s)${timing.HasDuplicateCustomTimings[tk] ? '; duplicate calls detected!' : ''}">
        ${duration(timing.CustomTimingStats[tk].Duration)}
        (${timing.CustomTimingStats[tk].Count}${((timing.HasDuplicateCustomTimings[tk] || timing.HasWarnings[tk]) ? '<span class="mp-warning">!</span>' : '')})
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

            const clientTimings = () => {
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

            const profilerQueries = () => {
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
                <div class="mp-call-type${(ct.Errored ? ' mp-warning' : '')}">${encode(ct.CallType)}${encode(!ct.ExecuteType || ct.CallType === ct.ExecuteType ? '' : ' - ' + ct.ExecuteType)}${((ct.IsDuplicate || ct.Errored) ? ' <span class="mp-warning" title="Duplicate">!</span>' : '')}</div>
                <div>${encode(ct.Parent.Name)}</div>
                <div class="mp-number">
                  ${duration(ct.DurationMilliseconds)} <span class="mp-unit">ms (T+${duration(ct.StartMilliseconds)} ms)</span>
                </div>
                ${(ct.FirstFetchDurationMilliseconds ? `<div>First Result: ${duration(ct.FirstFetchDurationMilliseconds)} <span class="mp-unit">ms</span></div>` : '')}
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

            return `
  <div class="mp-result${(this.options.showTrivial ? ' show-trivial' : '')}${(this.options.showChildrenTime ? ' show-columns' : '')}${(isNew ? ' new' : '')}">
    <div class="mp-button${(p.HasWarning ? ' mp-button-warning' : '')}" title="${encode(p.Name)}">
      <span class="mp-number">${duration(p.DurationMilliseconds)} <span class="mp-unit">ms</span></span>
      ${((p.HasDuplicateCustomTimings || p.HasWarning) ? '<span class="mp-warning">!</span>' : '')}
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
  </div>`;
        }

        private buttonShow = (json: IProfiler) => {
            if (!this.container) {
                // container not rendered yet
                this.savedJson.push(json);
                return;
            }

            const profilerHtml = this.renderProfiler(json, true);

            if (this.controls) {
                this.controls.insertAdjacentHTML('beforebegin', profilerHtml);
            } else {
                this.container.insertAdjacentHTML('beforeend', profilerHtml);
            }

            // limit count to maxTracesToShow, remove those before it
            const results = this.container.querySelectorAll('.mp-result');
            const toRemove = results.length - this.options.maxTracesToShow;
            for (let i = 0; i < toRemove; i++) {
                results[i].parentNode.removeChild(results[i]);
            }
        }

        private scrollToQuery = (link: HTMLElement, queries: HTMLElement) => {
            const id = link.closest('tr').dataset['timingId'];
            const rows = queries.querySelectorAll('tr[data-timing-id="' + id + '"]');
            rows.forEach(n => n.classList.add('highlight'));
            if (rows && rows[0]) {
                rows[0].scrollIntoView();
            }
        }

        // some elements want to be hidden on certain doc events
        private bindDocumentEvents = (mode: RenderMode) => {
            const mp = this;
            // Common handlers
            document.addEventListener('click', function (event) {
                const target = event.target as HTMLElement;
                if (target.matches('.mp-toggle-trivial')) {
                    target.closest('.mp-result').classList.toggle('show-trivial');
                }
                if (target.matches('.mp-toggle-columns')) {
                    target.closest('.mp-result').classList.toggle('show-columns');
                }
                if (target.matches('.mp-toggle-trivial-gaps')) {
                    target.closest('.mp-queries').classList.toggle('show-trivial');
                }
            }, false);

            // Full vs. Corner handlers
            if (mode === RenderMode.Full) {
                // since queries are already shown, just highlight and scroll when clicking a '1 sql' link
                document.addEventListener('click', function (event) {
                    const target = event.target as HTMLElement;
                    const queriesButton = target.closest('.mp-popup .mp-queries-show') as HTMLElement;
                    if (queriesButton) {
                        mp.scrollToQuery(queriesButton, document.body.querySelector('.mp-queries'));
                    }
                });
                document.documentElement.classList.add('mp-scheme-' + mp.options.colorScheme.toLowerCase());
            } else {
                document.addEventListener('click', function (event) {
                    const target = event.target as HTMLElement;
                    const button = target.closest('.mp-button') as HTMLElement;
                    if (button) {
                        const popup = button.parentElement.querySelector('.mp-popup') as HTMLDivElement;
                        const wasActive = button.parentElement.classList.contains('active');
                        const pos = mp.options.renderPosition;


                        let parent = button.parentElement;
                        parent.classList.remove('new');

                        const allChildren = button.parentElement.parentElement.children;
                        for (let i = 0; i < allChildren.length; i++) {
                            // Set Active only on the curent button
                            allChildren[i].classList.toggle('active', allChildren[i] == parent);
                        }

                        if (!wasActive) {
                            // move left or right, based on config
                            popup.style[pos === RenderPosition.Left || pos === RenderPosition.BottomLeft ? 'left' : 'right'] = `${(button.offsetWidth - 1)}px`;

                            // is this rendering on the bottom (if no, then is top by default)
                            if (pos === RenderPosition.BottomLeft || pos === RenderPosition.BottomRight) {
                                const bottom = window.innerHeight - button.getBoundingClientRect().top - button.offsetHeight + window.scrollY; // get bottom of button
                                popup.style.bottom = '0';
                                popup.style.maxHeight = 'calc(100vh - ' + (bottom + 25) + 'px)';
                            } else {
                                popup.style.top = '0';
                                popup.style.maxHeight = 'calc(100vh - ' + (button.getBoundingClientRect().top - window.window.scrollY + 25) + 'px)';
                            }
                        }
                        return;
                    }
                    const queriesButton = target.closest('.mp-queries-show') as HTMLElement;
                    if (queriesButton) {
                        // opaque background
                        document.body.insertAdjacentHTML('beforeend', '<div class="mp-overlay"><div class="mp-overlay-bg"/></div>');
                        const overlay = document.querySelector('.mp-overlay');
                        const queriesOrig = queriesButton.closest('.mp-result').querySelector('.mp-queries');
                        const queries = queriesOrig.cloneNode(true) as HTMLDivElement;
                        queries.style.display = 'block';
                        overlay.classList.add('mp-scheme-' + mp.options.colorScheme.toLowerCase());
                        overlay.appendChild(queries);

                        mp.scrollToQuery(queriesButton, queries);

                        // syntax highlighting
                        queries.querySelectorAll('pre code').forEach(block => mp.highlight(block as HTMLElement));
                        return;
                    }
                });
                // Background and esc binding to close popups
                const tryCloseActive = (event: MouseEvent | KeyboardEvent) => {
                    const target = event.target as HTMLElement;
                    const active = document.querySelector('.mp-result.active') as HTMLElement;
                    if (!active) return;

                    const bg = document.querySelector('.mp-overlay') as HTMLDivElement;
                    const isEscPress = event.type === 'keyup' && event.which === 27;
                    const isBgClick = event.type === 'click' && !target.closest('.mp-queries, .mp-results');

                    if (isEscPress || isBgClick) {
                        if (bg && bg.offsetParent !== null) {
                            bg.remove();
                        } else {
                            active.classList.remove('active');
                        }
                    }
                }
                document.addEventListener('click', tryCloseActive);
                document.addEventListener('keyup', tryCloseActive);

                if (mp.options.toggleShortcut && !mp.options.toggleShortcut.match(/^None$/i)) {
                    /**
                     * Based on http://www.openjs.com/scripts/events/keyboard_shortcuts/
                     * Version : 2.01.B
                     * By Binny V A
                     * License : BSD
                     */
                    const keys = mp.options.toggleShortcut.toLowerCase().split("+");

                    document.addEventListener('keydown', function (e) {
                        let element = e.target as HTMLElement;
                        if (element.nodeType == 3) element = element.parentElement;
                        if (element.tagName == 'INPUT' || element.tagName == 'TEXTAREA') return;

                        //Find Which key is pressed
                        let code;
                        if (e.keyCode) code = e.keyCode;
                        else if (e.which) code = e.which;

                        let character = String.fromCharCode(code).toLowerCase();
                        if (code == 188) character = ","; //If the user presses , when the type is onkeydown
                        if (code == 190) character = "."; //If the user presses , when the type is onkeydown

                        //Key Pressed - counts the number of valid keypresses - if it is same as the number of keys, the shortcut function is invoked
                        let kp = 0;
                        //Work around for stupid Shift key bug created by using lowercase - as a result the shift+num combination was broken
                        const shift_nums = {
                            "`": "~",
                            "1": "!",
                            "2": "@",
                            "3": "#",
                            "4": "$",
                            "5": "%",
                            "6": "^",
                            "7": "&",
                            "8": "*",
                            "9": "(",
                            "0": ")",
                            "-": "_",
                            "=": "+",
                            ";": ":",
                            "'": "\"",
                            ",": "<",
                            ".": ">",
                            "/": "?",
                            "\\": "|"
                        }
                        //Special Keys - and their codes
                        const special_keys = {
                            'esc': 27,
                            'escape': 27,
                            'tab': 9,
                            'space': 32,
                            'return': 13,
                            'enter': 13,
                            'backspace': 8,

                            'scrolllock': 145,
                            'scroll_lock': 145,
                            'scroll': 145,
                            'capslock': 20,
                            'caps_lock': 20,
                            'caps': 20,
                            'numlock': 144,
                            'num_lock': 144,
                            'num': 144,

                            'pause': 19,
                            'break': 19,

                            'insert': 45,
                            'home': 36,
                            'delete': 46,
                            'end': 35,

                            'pageup': 33,
                            'page_up': 33,
                            'pu': 33,

                            'pagedown': 34,
                            'page_down': 34,
                            'pd': 34,

                            'left': 37,
                            'up': 38,
                            'right': 39,
                            'down': 40,

                            'f1': 112,
                            'f2': 113,
                            'f3': 114,
                            'f4': 115,
                            'f5': 116,
                            'f6': 117,
                            'f7': 118,
                            'f8': 119,
                            'f9': 120,
                            'f10': 121,
                            'f11': 122,
                            'f12': 123
                        }

                        const modifiers = {
                            shift: { wanted: false },
                            ctrl: { wanted: false },
                            alt: { wanted: false }
                        };

                        for (let i = 0; i < keys.length; i++) {
                            const k = keys[i];
                            if (k == 'ctrl' || k == 'control') {
                                kp++;
                                modifiers.ctrl.wanted = true;
                            } else if (k == 'shift') {
                                kp++;
                                modifiers.shift.wanted = true;
                            } else if (k == 'alt') {
                                kp++;
                                modifiers.alt.wanted = true;
                            } else if (k.length > 1) { //If it is a special key
                                if (special_keys[k] == code) kp++;
                            } else { //The special keys did not match
                                if (character == k) kp++;
                                else if (shift_nums[character] && e.shiftKey) { //Stupid Shift key bug created by using lowercase
                                    character = shift_nums[character];
                                    if (character == k) kp++;
                                }
                            }
                        }
                        if (kp == keys.length
                            && e.ctrlKey == modifiers.ctrl.wanted
                            && e.shiftKey == modifiers.shift.wanted
                            && e.altKey == modifiers.alt.wanted) {
                            const results = document.querySelector('.mp-results') as HTMLElement;
                            results.style.display = results.style.display == 'none' ? 'block' : 'none';
                        }
                    }, false);
                }
            }
        }

        private initControls = (container: HTMLDivElement) => {
            if (this.options.showControls) {
                container.insertAdjacentHTML('beforeend', '<div class="mp-controls"><span class="mp-min-max">m</span><span class="mp-clear">c</span></div>');
                this.controls = container.querySelector('mp-controls') as HTMLDivElement;

                const minMax = container.querySelector('.mp-controls .mp-min-max') as HTMLSpanElement;
                minMax.addEventListener('click', function () {
                    container.classList.toggle('mp-min');
                });

                container.addEventListener('mouseover', function () {
                    if (this.classList.contains('mp-min')) {
                        minMax.style.display = 'block';
                    }
                });
                container.addEventListener('mouseout', function () {
                    if (this.classList.contains('mp-min')) {
                        minMax.style.display = 'none';
                    }
                });

                const clear = container.querySelector('.mp-result');
                clear.addEventListener('click', function () {
                    clear.parentNode.removeChild(clear);
                });
            } else {
                container.classList.add('mp-no-controls');
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

            function handleXHR(xhr: XMLHttpRequest | Sys.Net.WebRequestExecutor) {
                // iframed file uploads don't have headers
                if (xhr && xhr.getResponseHeader) {
                    // should be an array of strings, e.g. ["008c4813-9bd7-443d-9376-9441ec4d6a8c","16ff377b-8b9c-4c20-a7b5-97cd9fa7eea7"]
                    handleIds(xhr.getResponseHeader('X-MiniProfiler-Ids'));
                }
            }

            // we need to attach our AJAX complete handler to the window's (profiled app's) copy, not our internal, no conflict version
            const windowjQuery = window.jQuery;

            // fetch profile results for any AJAX calls
            if (windowjQuery && windowjQuery(document) && windowjQuery(document).ajaxComplete) {
                windowjQuery(document).ajaxComplete((_e: any, xhr: XMLHttpRequest, _settings: any) => handleXHR(xhr));
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
                    onComplete: function () {
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
