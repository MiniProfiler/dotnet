using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MvcMiniProfiler;

namespace Sample.WebForms
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var mp = MiniProfiler.Current;

            using (mp.Step("Page_Load"))
            {
                System.Threading.Thread.Sleep(40);
            }
        }
    }
}
