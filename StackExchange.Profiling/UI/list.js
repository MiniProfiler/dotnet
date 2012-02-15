var MiniProfiler = MiniProfiler || {};
MiniProfiler.list = {
    init:
        function (options) {
            var opt = options || {};

            var updateGrid = function (id) {
                jQueryMP.ajax({
                    url: options.path + 'results-list',
                    data: { "last-id": id },
                    dataType: 'json',
                    type: 'GET',
                    success: function (data) {
                        jQueryMP('table tbody').append(jQueryMP("#rowTemplate").tmpl(data));
                        var oldId = id;
                        var oldData = data;
                        setTimeout(function () {
                            var newId = oldId;
                            if (oldData.length > 0) {
                                newId = oldData[oldData.length - 1].Id;
                            }
                            updateGrid(newId);
                        }, 4000);
                    }
                });
            }

            MiniProfiler.path = options.path;
            jQueryMP.get(options.path + 'list.tmpl?v=' + options.version, function (data) {
                if (data) {
                    jQueryMP('body').append(data);
                    jQueryMP('body').append(jQueryMP('#tableTemplate').tmpl());
                    updateGrid();
                }
            });
        }
};