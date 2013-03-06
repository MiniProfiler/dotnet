package miniprofiler

import (
	"html/template"
)

const tmpl_include = `<script async type="text/javascript" id="mini-profiler" src="{{.Path}}includes.js?v={{.Version}}" data-version="{{.Version}}" data-path="{{.Path}}" data-current-id="{{.CurrentId}}" data-ids="{{.Ids}}" data-position="{{.Position}}" data-trivial="{{.ShowTrivial}}" data-children="{{.ShowChildren}}" data-max-traces="{{.MaxTracesToShow}}" data-controls="{{.ShowControls}}" data-authorized="{{.Authorized}}" data-toggle-shortcut="{{.ToggleShortcut}}" data-start-hidden="{{.StartHidden}}"></script>`

const tmpl_share = `<html>
    <head>
        <title>{{.Name}} ({{.Duration}} ms) - Profiling Results</title>
        <script type='text/javascript' src='{{.Path}}jquery.1.7.1.js?v={{.Version}}'></script>
        <script type='text/javascript'> var profiler = {{.Json}}; </script>
        {{.Includes}}
    </head>
    <body>
        <div class='profiler-result-full'></div>
    </body>
</html>`

var includesTmpl = template.Must(template.New("includes").Parse(tmpl_include))
var shareHtml = template.Must(template.New("share").Parse(tmpl_share))
