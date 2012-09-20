using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sample.Wcf
{
    public static class WcfCommon
    {
        private static string _connectionString;
        public static string ConnectionString
        {
            get {
                if (_connectionString == null)
                {
                    _connectionString = "Data Source = " + HttpContext.Current.Server.MapPath("~/App_Data/TestMiniProfiler.sqlite"); 
                }
                return _connectionString;
            }
        }
    }
}