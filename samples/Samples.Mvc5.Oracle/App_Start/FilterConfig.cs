using System.Web.Mvc;
using StackExchange.Profiling.Mvc;

namespace Samples.Mvc5
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new ProfilingActionFilter());
        }
    }
}
