/// <reference path="./node_modules/@types/jquery/index.d.ts">
/// <reference path="./node_modules/@types/extjs/index.d.ts">
/// <reference path="./node_modules/@types/microsoft-ajax/index.d.ts">
/// <reference path="./node_modules/@types/highlight.js/index.d.ts">
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
        ParentTimingId?: string;
        // added for gaps (TODO: change all this)
        parent: ITiming;
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
        ParentTimingId: string;
        ParentTimingName: string;
        CallType: string;
        IsDuplicate: boolean;
        // added for gaps
        prevGap: any;
        nextGap: any;
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
            ({ name: 'responseStart', description: 'Response Start', lineDescription: 'Response', type: 'request' }) as ITimingInfo,
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
                        mp.container = $('<div class="profiler-results"/>')
                            .addClass('profiler-' + mp.options.renderPosition)
                            .appendTo('body');

                        // initialize the controls
                        mp.initControls(mp.container);

                        // fetch and render results
                        mp.fetchResults(mp.options.ids);

                        if (mp.options.startHidden) {
                            mp.container.hide();
                        }

                        // if any data came in before the view popped up, render now
                        if (this.savedJson) {
                            for (const saved of this.savedJson) {
                                mp.buttonShow(saved);
                            }
                        }
                    } else {
                        mp.fetchResults(mp.options.ids);
                    }
                };

                // when rendering a shared, full page, this div will exist
                mp.container = $('.profiler-result-full');
                if (mp.container.length) {
                    if (window.location.href.indexOf('&trivial=1') > 0) {
                        mp.options.showTrivial = true;
                    }

                    // profiler will be defined in the full page's head
                    window.profiler.Started = new Date('' + window.profiler.Started); // Ugh, JavaScript
                    mp.renderProfiler(window.profiler).appendTo(mp.container);
                    // prettyPrint();

                    mp.bindDocumentEvents(RenderMode.Full);
                } else {
                    initPopupView();
                    mp.bindDocumentEvents(RenderMode.Corner);
                }
            }

            let wait = 0;
            let alreadyDone = false;
            const deferInit = () => {
                if (alreadyDone) {
                    return;
                }
                if (window.performance && window.performance.timing && window.performance.timing.loadEventEnd === 0 && wait < 10000) {
                    setTimeout(deferInit, 100);
                    wait += 100;
                } else {
                    alreadyDone = true;
                    if (mp.options.authorized) {
                        const url = mp.options.path + 'includes.css?v=' + mp.options.version;
                        if (document.createStyleSheet) {
                            document.createStyleSheet(url);
                        } else {
                            $('head').append($('<link rel="stylesheet" type="text/css" href="' + url + '" />'));
                        }
                    }
                    doInit();
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
  <td><a href="${options.path}results?id=${profiler.Id}">${escape(profiler.Name)}</a></td>
  <td>${escape(profiler.MachineName)}</td>
  <td class="profiler-results-index-date">${profiler.Started}</td>
  <td>${profiler.DurationMilliseconds}</td>` + (profiler.ClientTimings ? `
  <td>${getTiming(profiler, 'requestStart').Start}</td>
  <td>${getTiming(profiler, 'responseStart').Start}</td>
  <td>${getTiming(profiler, 'domComplete').Start}</td> ` : `
  <td colspan="3" class="profiler-results-none">(no client timings)</td>`) + `
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
            const json: IProfiler = { ...profiler };
            const mp = this;

            // TODO: nuke
            json.HasDuplicateCustomTimings = false;
            json.HasCustomTimings = false;
            json.HasTrivialTimings = false;
            json.CustomTimingStats = {};
            json.CustomLinks = json.CustomLinks || {};
            json.Root.ParentTimingId = json.Id;

            function processTiming(timing: ITiming, depth: number) {
                timing.DurationWithoutChildrenMilliseconds = timing.DurationMilliseconds;
                timing.Depth = depth;
                timing.HasCustomTimings = !!timing.CustomTimings;
                timing.HasDuplicateCustomTimings = {};
                json.HasCustomTimings = json.HasCustomTimings || timing.HasCustomTimings;

                if (timing.Children) {
                    for (const child of timing.Children) {
                        child.ParentTimingId = timing.Id;
                        processTiming(child, depth + 1);
                        timing.DurationWithoutChildrenMilliseconds -= child.DurationMilliseconds;
                    }
                } else {
                    timing.Children = [];
                }

                // do this after subtracting child durations
                timing.IsTrivial = timing.DurationWithoutChildrenMilliseconds < mp.options.trivialMilliseconds;
                json.HasTrivialTimings = json.HasTrivialTimings || timing.IsTrivial;

                function ignoreDuplicateCustomTiming(customTiming: ICustomTiming) {
                    return customTiming.ExecuteType && mp.options.ignoredDuplicateExecuteTypes.indexOf(customTiming.ExecuteType) > -1;
                }

                if (timing.CustomTimings) {
                    timing.CustomTimingStats = {};
                    for (const customType of Object.keys(timing.CustomTimings)) {
                        const customTimings = timing.CustomTimings[customType];
                        const customStat = {
                                  Duration: 0,
                                  Count: 0,
                              };
                        const duplicates: { [id: string]: boolean } = {};
                        for (const customTiming of customTimings) {
                            customTiming.ParentTimingId = timing.Id;
                            customStat.Duration += customTiming.DurationMilliseconds;
                            customStat.Count++;
                            if (customTiming.CommandString && duplicates[customTiming.CommandString]) {
                                customTiming.IsDuplicate = true;
                                timing.HasDuplicateCustomTimings[customType] = true;
                                json.HasDuplicateCustomTimings = true;
                            } else if (!ignoreDuplicateCustomTiming(customTiming)) {
                                duplicates[customTiming.CommandString] = true;
                            }
                        }
                        timing.CustomTimingStats[customType] = customStat;
                        if (!json.CustomTimingStats[customType]) {
                            json.CustomTimingStats[customType] = {
                                Duration: 0,
                                Count: 0,
                            };
                        }
                        json.CustomTimingStats[customType].Duration += customStat.Duration;
                        json.CustomTimingStats[customType].Count += customStat.Count;
                    }
                } else {
                    timing.CustomTimings = {};
                }
            }

            processTiming(json.Root, 0);

            return json;
        }

        private renderProfiler = (json: IProfiler) => {
            const p = this.processJson(json);
            const mp = this;
            const encode = (orig: string) => orig
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;')
                    .replace(/"/g, '&quot;')
                    .replace(/'/g, '&#039;');
            const duration = (milliseconds: number | undefined) => {
                    if (milliseconds === undefined) {
                        return '';
                    }
                    return (milliseconds || 0).toFixed(1);
                };

            const renderTiming = (timing: ITiming) => {
                const customTimingTypes = p.CustomTimingStats ? Object.keys(p.CustomTimingStats) : [];
                let str = `
  <tr class="${timing.IsTrivial ? 'profiler-trivial' : ''}" data-timing-id="${timing.Id}">
    <td class="profiler-label" title="${encode(timing.Name && timing.Name.length > 45 ? timing.Name : '')}"${timing.Depth > 0 ? ` style="padding-left:${timing.Depth * 11}px;"` : ''}>
      ${encode(timing.Name.slice(0, 45))}${encode(timing.Name && timing.Name.length > 45 ? '...' : '')}
    </td>
    <td class="profiler-duration" title="duration of this step without any children's durations">
      ${duration(timing.DurationWithoutChildrenMilliseconds)}
    </td>
    <td class="profiler-duration profiler-more-columns" title="duration of this step and its children">
      ${duration(timing.DurationMilliseconds)}
    </td>
    <td class="profiler-duration profiler-more-columns time-from-start" title="time elapsed since profiling started">
      <span class="profiler-unit">+</span>${duration(timing.StartMilliseconds)}
    </td>
    ${customTimingTypes.map((tk) => timing.CustomTimings[tk] ? `
    <td class="profiler-duration">
      <a class="profiler-queries-show" title="${duration(timing.CustomTimingStats[tk].Duration)} ms in ${timing.CustomTimings[tk].length} ${encode(tk)} calls${timing.HasDuplicateCustomTimings[tk] ? '; duplicate calls detected!' : ''}">
        ${duration(timing.CustomTimingStats[tk].Duration)}
        (${timing.CustomTimings[tk].length}${(timing.HasDuplicateCustomTimings[tk] ? '<span class="profiler-warning">!</span>' : '')})
      </a>
    </td>` : '<td></td>').join('')}
  </tr>`;
                // Append children
                timing.Children.forEach((ct) => str += renderTiming(ct));
                return str;
            };

            const timingsTable = `
        <table class="profiler-timings">
          <thead>
            <tr>
              <th></th>
              <th>duration (ms)</th>
              <th class="profiler-more-columns">with children (ms)</th>
              <th class="time-from-start profiler-more-columns">from start (ms)</th>
              ${Object.keys(p.CustomTimingStats).map((k) => `<th title="call count">${encode(k)} (ms)</th>`).join('')}
            </tr>
          </thead>
          <tbody>
            ${renderTiming(p.Root)}
          </tbody>
          <tfoot>
            <tr>
              <td colspan="2"></td>
              <td class="profiler-more-columns" colspan="2"></td>
            </tr>
          </tfoot>
        </table>`;

            const customTimings = () => {
                if (!p.HasCustomTimings) {
                    return '';
                }
                return `
        <table class="profiler-custom-timing-overview">
            ${Object.getOwnPropertyNames(p.CustomTimingStats).map((key) => `
          <tr title="${p.CustomTimingStats[key].Count} ${encode(key.toLowerCase())} calls spent ${duration(p.CustomTimingStats[key].Duration)} ms of total request time">
            <td class="profiler-number">
              ${encode(key)}:
            </td>
            <td class="profiler-number">
              ${duration(p.CustomTimingStats[key].Duration / p.DurationMilliseconds * 100)} <span class="profiler-unit">%</span>
            </td>
          </tr>`).join('')}
        </table>`;
            };

            function clientTimings() {
                if (!p.ClientTimings) {
                    return '';
                }

                const list = p.ClientTimings.Timings.map((t) => {
                    const results = this.clientPerfTimings ? this.clientPerfTimings.filter((pt: ITimingInfo) => pt.name === t.Name) : [];
                    const info = results.length > 0 ? results[0] : null;

                    return {
                        isTrivial: t.Duration === 0 && !(info && info.point),
                        name: info && info.lineDescription || t.Name,
                        duration: info && info.point ? undefined : t.Duration,
                        start: t.Start,
                    };
                });
                list.sort((a, b) => a.start - b.start);

                return `
        <table class="profiler-timings profiler-client-timings">
          <thead>
            <tr>
              <th style="text-align:left">client event</th>
              <th>duration (ms)</th>
              <th>from start (ms)</th>
            </tr>
          </thead>
          <tbody>
            ${list.map((t) => `
            <tr class="${(t.isTrivial ? 'profiler-trivial' : '')}">
              <td class="profiler-label">${encode(t.name)}</td>
              <td class="profiler-duration">
                ${(t.duration >= 0 ? `<span class="profiler-unit"></span>${duration(t.duration)}` : '')}
              </td>
              <td class="profiler-duration time-from-start">
                <span class="profiler-unit">+</span>${duration(t.start)}
              </td>
            </tr>`).join('')}
          </tbody>
        </table>`;
            }

            function profilerQueries() {
                if (!p.HasCustomTimings) {
                    return '';
                }

                const cts = mp.getCustomTimings(p.Root);
                const renderGap = (gap: any) => gap ? `
  <tr class="profiler-gap-info ${(gap.duration < 4 ? 'profiler-trivial-gap' : '')}">
    <td class="profiler-info">
      ${gap.duration} <span class="profiler-unit">ms</span>
    </td>
    <td class="query">
      <div>${encode(gap.topReason.name)} &mdash; ${gap.topReason.duration.toFixed(2)} <span class="profiler-unit">ms</span></div>
    </td>
  </tr>` : '';

                return `
    <div class="profiler-queries">
      <table>
        <thead>
          <tr>
            <th>
              <div class="profiler-call-type">Call Type</div>
              <div>Step</div>
              <div>Duration <span class="profiler-unit">(from start)</span></div>
            </th>
            <th>
              <div class="profiler-stack-trace">Call Stack</div>
              <div>Command</div>
            </th>
          </tr>
        </thead>
        <tbody>
          ${cts.map((ct, index) => `
            ${renderGap(ct.prevGap)}
            <tr class="${(index % 2 === 1 ? 'profiler-odd' : '')}" data-timing-id="${ct.ParentTimingId}">
              <td>
                <div class="profiler-call-type">${encode(ct.CallType)}${encode(!ct.ExecuteType || ct.CallType === ct.ExecuteType ? '' : ' - ' + ct.ExecuteType)}</div>
                <div>${encode(ct.ParentTimingName)}</div>
                <div class="profiler-number">
                  ${duration(ct.DurationMilliseconds)} <span class="profiler-unit">ms (T+${duration(ct.StartMilliseconds)} ms)</span>
                </div>
                ${(ct.FirstFetchDurationMilliseconds ? `<div>First Result: ${duration(ct.DurationMilliseconds)} <span class="profiler-unit">ms</span></div>` : '')}
                ${(ct.IsDuplicate ? '<div><span class="profiler-warning">(DUPLICATE)</span></div>' : '')}
              </td>
              <td>
                <div class="query">
                  <div class="profiler-stack-trace">${encode(ct.StackTraceSnippet)}</div>
                  <pre class="prettyprint lang-${encode(ct.CallType)}"><code>${encode(ct.CommandString)}</code></pre>
                </div>
              </td>
            </tr>
            ${renderGap(ct.nextGap)}`).join('')}
        </tbody>
      </table>
      <p class="profiler-trivial-gap-container">
        <a class="profiler-toggle-trivial-gaps" href="#">toggle trivial gaps</a>
      </p>
    </div>`;
            }

            return mp.jq(`
  <div class="profiler-result${(this.options.showTrivial ? 'show-trivial' : '')}${(this.options.showChildrenTime ? 'show-columns' : '')}">
    <div class="profiler-button" title="${encode(p.Name)}">
      <span class="profiler-number">${duration(p.DurationMilliseconds)} <span class="profiler-unit">ms</span></span>
      ${(p.HasDuplicateCustomTimings ? '<span class="profiler-warning">!</span>' : '')}
    </div>
    <div class="profiler-popup">
      <div class="profiler-info">
        <div>
          <div class="profiler-name">${encode(p.Name)}</div>
          <div class="profiler-machine-name">${encode(p.MachineName)}</div>
        </div>
        <div>
          <div class="profiler-overall-duration">(${duration(p.DurationMilliseconds)} ms)</div>
          <div class="profiler-started">${p.Started ? p.Started.toUTCString() : ''}</div>
        </div>
      </div>
      <div class="profiler-output">
        ${timingsTable}
		${customTimings()}
        ${clientTimings()}
        <div class="profiler-links">
          <a href="${this.options.path}results?id=${p.Id}" class="profiler-share-profiler-results" target="_blank">share</a>
          ${Object.keys(p.CustomLinks).map((k) => `<a href="${p.CustomLinks[k]}" class="profiler-custom-link" target="_blank">${k}</a>`).join('')}
  		  <span>
            <a class="profiler-toggle-columns" title="shows additional columns">more columns</a>
            <a class="profiler-toggle-columns profiler-more-columns" title="hides additional columns">fewer columns</a>
            ${(p.HasTrivialTimings ? `
            <a class="profiler-toggle-trivial" title="shows any rows with &lt; ${this.options.trivialMilliseconds} ms duration">show trivial</a>
            <a class="profiler-toggle-trivial profiler-trivial" title="hides any rows with &lt; ${this.options.trivialMilliseconds} ms duration">hide trivial</a>` : '')}
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
            this.container.find('.profiler-result:lt(' + -this.options.maxTracesToShow + ')').remove();
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
                .on('click', '.profiler-toggle-trivial', function(e) {
                    e.preventDefault();
                    $(this).closest('.profiler-result').toggleClass('show-trivial');
                }).on('click', '.profiler-toggle-columns', function(e) {
                    e.preventDefault();
                    $(this).closest('.profiler-result').toggleClass('show-columns');
                }).on('click', '.profiler-toggle-trivial-gaps', function(e) {
                    e.preventDefault();
                    $(this).closest('.profiler-queries').find('.profiler-trivial-gap').toggle();
                });

            // Full vs. Corner handlers
            if (mode === RenderMode.Full) {
                // since queries are already shown, just highlight and scroll when clicking a '1 sql' link
                $(document).on('click', '.profiler-popup .profiler-queries-show', function() {
                    mp.scrollToQuery($(this), $('.profiler-queries'), $(document));
                });
            } else {
                $(document)
                    .on('click', '.profiler-button', function(e) {
                        const button = $(this);
                        const popup = button.siblings('.profiler-popup');
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
                    }).on('click', '.profiler-queries-show', function(e) {
                        // opaque background
                        const overlay = $('<div class="profiler-overlay"><div class="profiler-overlay-bg"/></div>').appendTo('body');
                        const queries = $(this).closest('.profiler-result').find('.profiler-queries').clone().appendTo(overlay).show();

                        mp.scrollToQuery($(this), queries, queries);

                        // syntax highlighting
                        // prettyPrint();
                    }).on('click keyup', (e) => {
                        const active = $('.profiler-result.active');
                        if (active.length) {
                            const bg = $('.profiler-overlay');
                            const isEscPress = e.type === 'keyup' && e.which === 27;
                            const isBgClick = e.type === 'click' && !$(e.target).closest('.profiler-queries, .profiler-results').length;

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
                    $(document).bind('keydown', mp.options.toggleShortcut, (e) => $('.profiler-results').toggle());
                }
            }
        }

        private initControls = (container: JQuery) => {
            if (this.options.showControls) {
                this.controls = $('<div class="profiler-controls"><span class="profiler-min-max">m</span><span class="profiler-clear">c</span></div>').appendTo(container);

                $('.profiler-controls .profiler-min-max').click(() => container.toggleClass('profiler-min'));

                container.hover(
                    function() {
                        if ($(this).hasClass('profiler-min')) {
                            $(this).find('.profiler-min-max').show();
                        }
                    },
                    function() {
                        if ($(this).hasClass('profiler-min')) {
                            $(this).find('.profiler-min-max').hide();
                        }
                    });

                $('.profiler-controls .profiler-clear').click(() => container.find('.profiler-result').remove());
            } else {
                container.addClass('profiler-no-controls');
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

            function handleXHR(xhr: XMLHttpRequest | JQuery.jqXHR) {
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
                window$(document).ajaxComplete((e: any, xhr: JQuery.jqXHR, settings: any) => handleXHR(xhr));
            }

            // fetch results after ASP Ajax calls
            if (typeof (Sys) !== 'undefined' && typeof (Sys.WebForms) !== 'undefined' && typeof (Sys.WebForms.PageRequestManager) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest((sender: any, args: any) => {
                    if (args) {
                        const response = args.get_response();
                        if (response.get_responseAvailable() && response._xmlHttpRequest != null) {
                            handleXHR(response);
                        }
                    }
                });
            }

            if (typeof (Sys) !== 'undefined' && typeof (Sys.Net) !== 'undefined' && typeof (Sys.Net.WebRequestManager) !== 'undefined') {
                Sys.Net.WebRequestManager.add_completedRequest((sender: any, args: any) => {
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
                window.WebForm_ExecuteCallback = ((callbackObject: any) => {
                    // Store original function
                    const original = window.WebForm_ExecuteCallback;

                    return (callbackObjectInner: any) => {
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
                        if (typeof (this.miniprofiler) === undefined || typeof (this.miniprofiler.prev_onreadystatechange) === undefined) {
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
                        if (typeof (this.miniprofiler) === undefined || typeof (this.miniprofiler.prev_onload) === undefined) {
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

        private getCustomTimings = (root: ITiming) => {
            const result: ICustomTiming[] = [];

            function addToResults(timing: ITiming) {
                if (timing.CustomTimings) {
                    for (const customType of Object.keys(timing.CustomTimings)) {
                        const customTimings = timing.CustomTimings[customType];

                        for (const customTiming of customTimings) {
                            // HACK: add info about the parent Timing to each CustomTiming so UI can render
                            customTiming.ParentTimingName = timing.Name;
                            customTiming.CallType = customType;
                            result.push(customTiming);
                        }
                    }
                }

                if (timing.Children) {
                    for (const child of timing.Children) {
                        addToResults(child);
                    }
                }
            }

            // start adding at the root and recurse down
            addToResults(root);
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

            function processTimes(elem: ITiming, parent: ITiming) {
                const duration = ({ start: elem.StartMilliseconds, finish: (elem.StartMilliseconds + elem.DurationMilliseconds) }) as IGapTiming;
                elem.richTiming = [duration];
                if (parent != null) {
                    elem.parent = parent;
                    elem.parent.richTiming = removeDuration(elem.parent.richTiming, duration);
                }

                if (elem.Children) {
                    for (const child of elem.Children) {
                        processTimes(child, elem);
                    }
                }
            }

            processTimes(root, null);
            // sort results by time
            result.sort((a, b) => a.StartMilliseconds - b.StartMilliseconds);

            function determineOverlap(gap: IGapTiming, node: ITiming) {
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

            function determineGap(gap: IGapTiming, node: ITiming, match: any) {
                const overlap = determineOverlap(gap, node);
                if (match == null || overlap > match.duration) {
                    match = { name: node.Name, duration: overlap };
                } else if (match.name === node.Name) {
                    match.duration += overlap;
                }

                if (node.Children) {
                    for (const child of node.Children) {
                        match = determineGap(gap, child, match);
                    }
                }
                return match;
            }

            let time = 0;
            let prev = null;
            result.forEach((elem) => {
                elem.prevGap = {
                    duration: (elem.StartMilliseconds - time).toFixed(2),
                    start: time,
                    finish: elem.StartMilliseconds,
                };

                elem.prevGap.topReason = determineGap(elem.prevGap, root, null);

                time = elem.StartMilliseconds + elem.DurationMilliseconds;
                prev = elem;
            });


            if (result.length > 0) {
                const me = result[result.length - 1];
                me.nextGap = {
                    duration: (root.DurationMilliseconds - time).toFixed(2),
                    start: time,
                    finish: root.DurationMilliseconds,
                };
                me.nextGap.topReason = determineGap(me.nextGap, root, null);
            }

            return result;
        }
    }
}

window.MiniProfiler = new StackExchange.Profiling.MiniProfiler().init();
