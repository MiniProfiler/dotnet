using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler.SqlFormatters
{
    public class InlineFormatter : ISqlFormatter
    {
        /* TODO: port this JS
         
         *     var inlineSqlParameters = function(sqlTiming) {
        if (!sqlTiming.Parameters) return sqlTiming;

        var txt = sqlTiming.CommandString;

        for (var i = 0, p; i < sqlTiming.Parameters.length; i++) {
            p = sqlTiming.Parameters[i];
            ensureParameterName(txt, p);
            txt = txt.replace(new RegExp(p.Name, 'gi'), getParameterValue(p));
        }

        sqlTiming.CommandString = txt;
    };

    var ensureParameterName = function(txt, p) {
        // DbParameters don't have to have a @ or : as prefix, so ensure the name we have matches what's used in the query
        if (p.Name.match(/[@:?].+/)) { return; }

        var matches = txt.match(/([@:?])\w+/);
        if (matches) {
            p.Name = matches[1] + p.Name;
        }
    };

    var getParameterValue = function(p) {
        // TODO: ugh, figure out how to allow different db providers to specify how values are represented (e.g. bit in oracle)
        var result = p.Value,
            t = (p.DbType || '').toLowerCase();
        
        if (t.match(/(string|datetime)/)) {
            result = "'" + result + "'";
        }
        else if (t.match(/boolean/)) {
            result = result == "True" ? 1 : result == "False" ? 0 : null;
        }

        if (result === null) {
            result = 'null';
        }

        return result + ' /* ' + p.Name + ' DbType.' + p.DbType + ' * /'; 
    };
         
         
         */


        public string FormatSql(SqlTiming timing)
        {
            throw new NotImplementedException();
        }
    }
}
