var MiniProfiler = {};
MiniProfiler.list = {
    init:
        function (options) {
            var opt = options || {};

            $.get(options.path + 'list.tmpl?v=' + options.version, function (data) {
                if (data) {
                    $('body').append(data);
                    $('body').append($('#tableTemplate').tmpl());
                }
            });
            $.tmpl()
        }
};