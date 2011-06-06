using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler.UI
{
    public class ProfileRenderer
    {
        public static string Render(MiniProfilerResultsModel model)
        {
            StringBuilder sb = new StringBuilder();

            if (!model.IsPopup)
            {
                RenderHeader(model,sb); 
            }

            RenderBody(model, sb);

            if (!model.IsPopup)
            {
                RenderFooter(model, sb);
            }

            return sb.ToString();
        }

        private static void RenderHeader(MiniProfilerResultsModel model, StringBuilder sb)
        {
            var p = model.MiniProfiler;
            sb.Append(@"<html>
<head>
    <title>").Append(p.Name).Append(@" (").Append(p.DurationMilliseconds).Append(@" ms) - MiniProfiler Results</title>
    <script type='text/javascript' src='https://ajax.googleapis.com/ajax/libs/jquery/1.6.1/jquery.min.js'></script>
    <link type='text/css' rel='stylesheet/less' href='/mini-profiler-includes.less' />
    <script type='text/javascript' src='/mini-profiler-includes.js'></script>
    <script type='text/javascript'>
        jQuery(function() { MiniProfiler.init(); });
    </script>
</head>
<body>");
        }

        private static void RenderBody(MiniProfilerResultsModel model, StringBuilder sb)
        {
            var p = model.MiniProfiler;

            var sqlTimings = p.GetSqlTimings();
            if (sqlTimings.Any())
            {
                RenderTimings(model, sb, sqlTimings);
            }

            sb.Append("</div>");
        }

        private static void RenderTimings(MiniProfilerResultsModel model, StringBuilder sb, List<MvcMiniProfiler.SqlTiming> sqlTimings)
        {
            throw new NotImplementedException();
        }

        private static void RenderFooter(MiniProfilerResultsModel model, StringBuilder sb)
        {
            sb.AppendLine(@"</body></html>");
        }
    }
}
