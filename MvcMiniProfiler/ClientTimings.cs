using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Runtime.Serialization;

namespace MvcMiniProfiler
{
    public class ClientTimings
    {
        const string clientTimingPrefix = "clientPerformance[timing][";

        /// <summary>
        /// Returns null if there is not client timing stuff
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static ClientTimings FromRequest(HttpRequest request)
        {
            ClientTimings timing = null;
            long navigationStart = 0;
            long.TryParse(request[clientTimingPrefix + "navigationStart]"], out navigationStart);
            if (navigationStart > 0)
            {
                timing = new ClientTimings();

                int redirectCount = 0;
                int.TryParse(request["clientPerformance[navigation][redirectCount]"], out redirectCount);
                timing.RedirectCount = redirectCount;

                foreach (string key in request.Form.Keys)
                {
                    if (key.StartsWith(clientTimingPrefix))
                    {
                        long val = 0;
                        long.TryParse(request[key], out val);
                        val -= navigationStart;

                        string parsedName = key.Substring(clientTimingPrefix.Length, (key.Length-1) - clientTimingPrefix.Length);

                        // just ignore stuff that is negative ... not relevant
                        if (val > 0)
                        {
                            switch (parsedName)
                            {
                                case "unloadEventStart" :
                                    timing.UnloadEventStart = val;
                                    break;
                                case "unloadEventEnd":
                                    timing.UnloadEventEnd = val;
                                    break;
                                case "redirectStart":
                                    timing.RedirectStart = val;
                                    break;
                                case "redirectEnd":
                                    timing.RedirectEnd = val;
                                    break;
                                case "fetchStart":
                                    timing.FetchStart = val;
                                    break;
                                case "domainLookupStart":
                                    timing.DomainLookupStart = val;
                                    break;
                                case "domainLookupEnd":
                                    timing.DomainLookupEnd = val;
                                    break;
                                case "connectStart":
                                    timing.ConnectStart = val;
                                    break;
                                case "connectEnd":
                                    timing.ConnectEnd = val;
                                    break;
                                case "secureConnectionStart":
                                    timing.SecureConnectionStart = val;
                                    break;
                                case "requestStart":
                                    timing.RequestStart = val;
                                    break;
                                case "responseStart":
                                    timing.ResponseStart = val;
                                    break;
                                case "responseEnd":
                                    timing.ResponseEnd = val;
                                    break;
                                case "domLoading":
                                    timing.DomLoading = val;
                                    break;
                                case "domInteractive":
                                    timing.DomInteractive = val;
                                    break;
                                case "domContentLoadedEventStart":
                                    timing.DomContentLoadedEventStart = val;
                                    break;
                                case "domContentLoadedEventEnd":
                                    timing.DomContentLoadedEventEnd = val;
                                    break;
                                case "domComplete":
                                    timing.DomComplete = val;
                                    break;
                                case "loadEventStart":
                                    timing.LoadEventStart = val;
                                    break;
                                case "loadEventEnd":
                                    timing.LoadEventEnd = val;
                                    break;
                                default:
                                    break;
                            }
                        }

                        
                    }
                }
            }
            return timing;
        }

        private ClientTimings()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 1)]
        public int RedirectCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 2)]
        public Decimal NavigationStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 3)]
        public Decimal UnloadEventStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 4)]
        public Decimal UnloadEventEnd { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 5)]
        public Decimal RedirectStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 6)]
        public Decimal RedirectEnd { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 7)]
        public Decimal FetchStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 8)]
        public Decimal DomainLookupStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 9)]
        public Decimal DomainLookupEnd { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 10)]
        public Decimal ConnectStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 11)]
        public Decimal ConnectEnd { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 12)]
        public Decimal SecureConnectionStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 13)]
        public Decimal RequestStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 14)]
        public Decimal ResponseStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 15)]
        public Decimal ResponseEnd { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 16)]
        public Decimal DomLoading { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 17)]
        public Decimal DomInteractive { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 18)]
        public Decimal DomContentLoadedEventStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 19)]
        public Decimal DomContentLoadedEventEnd { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 20)]
        public Decimal DomComplete { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 21)]
        public Decimal LoadEventStart { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 22)]
        public Decimal LoadEventEnd { get; set; }

    }
}
