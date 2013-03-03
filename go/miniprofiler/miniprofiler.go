package miniprofiler

import (
	"bytes"
	"encoding/json"
	"fmt"
	"html/template"
	"log"
	"net/http"
	"os"
	"strings"
	"time"
)

var (
	Enable      func(*http.Request) bool
	Store       func(*http.Request, *Profile)
	Get         func(*http.Request, string) *Profile
	MachineName func(*http.Request) string = Hostname

	Position        = "left"
	ShowTrivial     = false
	ShowChildren    = false
	MaxTracesToShow = 15
	ShowControls    = true
	ToggleShortcut  = "Alt+P"
	StartHidden     = false

	staticFiles map[string][]byte
)

const (
	PATH         = "/mini-profiler-resources/"
	PATH_RESULTS = PATH + "results"
)

func init() {
	http.HandleFunc(PATH, MiniProfilerHandler)

	staticFiles = map[string][]byte{
		"includes.css":   includes_css,
		"includes.js":    includes_js,
		"includes.tmpl":  includes_tmpl,
		"jquery.tmpl.js": jquery_tmpl_js,
	}
}

func MiniProfilerHandler(w http.ResponseWriter, r *http.Request) {
	path := r.URL.Path[strings.LastIndex(r.URL.Path, "/")+1:]
	if staticFiles[path] != nil {
		Static(w, r)
	} else if PATH_RESULTS == r.URL.Path {
		Results(w, r)
	} else {
		http.Error(w, "", http.StatusNotFound)
	}
}

func Results(w http.ResponseWriter, r *http.Request) {
	id := r.FormValue("id")
	isPopup := r.FormValue("popup") == "1"
	p := Get(r, id)
	if p == nil {
		http.Error(w, "", http.StatusNotFound)
		return
	}

	if isPopup {
		if j, err := json.Marshal(p); err == nil {
			w.Write(j)
		}
	}
}

func Static(w http.ResponseWriter, r *http.Request) {
	fname := r.URL.Path[strings.LastIndex(r.URL.Path, "/")+1:]
	if v, present := staticFiles[fname]; present {
		h := w.Header()

		if strings.HasSuffix(r.URL.Path, ".css") {
			h.Set("Content-type", "text/css")
		} else if strings.HasSuffix(r.URL.Path, ".js") {
			h.Set("Content-type", "text/javascript")
		}

		h.Set("Cache-Control", "public, max-age=expiry")
		expires := time.Now().Add(time.Hour)
		h.Set("Expires", expires.Format(time.RFC1123))

		w.Write(v)
	}
}

func Includes(r *http.Request, p *Profile) template.HTML {
	if !enable(r) {
		return ""
	}

	current := p.Id
	authorized := true

	v := struct {
		Ids                       Guid
		Path, Version, Position   string
		ShowTrivial, ShowChildren bool
		MaxTracesToShow           int
		ShowControls              bool
		CurrentId                 Guid
		Authorized                bool
		ToggleShortcut            string
		StartHidden               bool
	}{
		Ids:             current,
		Path:            PATH,
		Version:         Version,
		Position:        Position,
		ShowTrivial:     ShowTrivial,
		ShowChildren:    ShowChildren,
		MaxTracesToShow: MaxTracesToShow,
		ShowControls:    ShowControls,
		CurrentId:       current,
		Authorized:      authorized,
		ToggleShortcut:  ToggleShortcut,
		StartHidden:     StartHidden,
	}

	var w bytes.Buffer
	if err := includesTmpl.Execute(&w, v); err != nil {
		log.Print(err)
		return ""
	}
	return template.HTML(w.String())
}

func enable(r *http.Request) bool {
	if Enable == nil || Get == nil || Store == nil {
		return false
	}

	return Enable(r)
}

type Handler struct {
	f func(*Profile, http.ResponseWriter, *http.Request)
	p *Profile
}

func NewHandler(f func(*Profile, http.ResponseWriter, *http.Request)) Handler {
	return Handler{
		f: f,
	}
}

func (h Handler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if enable(r) {
		h.p = &Profile{
			Id:          NewGuid(),
			start:       time.Now(),
			MachineName: Hostname(r),
			Root: &Timing{
				Id:     NewGuid(),
				IsRoot: true,
				Name:   r.URL.String(),
			},
		}
		h.f(h.p, w, r)

		h.p.Started = fmt.Sprintf("/Date(%d)/", h.p.start.Unix()*1000)
		h.p.DurationMilliseconds = Since(h.p.start)
		h.p.Root.DurationMilliseconds = h.p.DurationMilliseconds

		Store(r, h.p)
	} else {
		h.f(nil, w, r)
	}
}

func Since(t time.Time) float64 {
	d := time.Since(t)
	return float64(d.Nanoseconds()) / 1000000
}

func Hostname(r *http.Request) string {
	name, err := os.Hostname()
	if err != nil {
		return ""
	}
	return name
}
