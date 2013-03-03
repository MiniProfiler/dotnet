package miniprofiler

import (
	"html/template"
)

var INCLUDE_TMPL = `<script async type="text/javascript" id="mini-profiler" src="{{.Path}}includes.js?v={{.Version}}" data-version="{{.Version}}" data-path="{{.Path}}" data-current-id="{{.CurrentId}}" data-ids="{{.Ids}}" data-position="{{.Position}}" data-trivial="{{.ShowTrivial}}" data-children="{{.ShowChildren}}" data-max-traces="{{.MaxTracesToShow}}" data-controls="{{.ShowControls}}" data-authorized="{{.Authorized}}" data-toggle-shortcut="{{.ToggleShortcut}}" data-start-hidden="{{.StartHidden}}"></script>`

var includesTmpl = template.Must(template.New("includes").Parse(INCLUDE_TMPL))
