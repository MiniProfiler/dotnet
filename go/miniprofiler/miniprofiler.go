/*
 * Copyright (c) 2013 Matt Jibson <matt.jibson@gmail.com>
 *
 * Permission to use, copy, modify, and distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 */

package miniprofiler

import (
	"bytes"
	"encoding/json"
	"html/template"
	"log"
	"net/http"
	"os"
	"reflect"
	"runtime"
	"sort"
	"strconv"
	"strings"
	"time"
	"unicode"
)

var (
	// Enable returns true if the request should be profiled.
	Enable func(*http.Request) bool

	// Store stores the Profile by its Id field.
	Store func(*http.Request, *Profile)

	// Get retrieves a Profile by its Id field.
	Get func(*http.Request, string) *Profile

	// MachineName returns the machine name to display.
	// The default is to use the machine's hostname.
	MachineName func() string = Hostname

	Position        = "left"
	ShowTrivial     = false
	ShowChildren    = false
	MaxTracesToShow = 15
	ShowControls    = true
	ToggleShortcut  = "Alt+P"
	StartHidden     = false

	Version = "0.2"

	staticFiles map[string][]byte
)

const (
	PATH         = "/mini-profiler-resources/"
	path_results = PATH + "results"

	clientTimingsPrefix = "clientPerformance[timing]["
)

func init() {
	http.HandleFunc(PATH, miniProfilerHandler)

	staticFiles = map[string][]byte{
		"includes.css":    includes_css,
		"includes.js":     includes_js,
		"includes.tmpl":   includes_tmpl,
		"jquery.1.7.1.js": jquery_1_7_1_js,
		"jquery.tmpl.js":  jquery_tmpl_js,
	}
}

func miniProfilerHandler(w http.ResponseWriter, r *http.Request) {
	path := r.URL.Path[strings.LastIndex(r.URL.Path, "/")+1:]
	if staticFiles[path] != nil {
		static(w, r)
	} else if path_results == r.URL.Path {
		results(w, r)
	} else {
		http.Error(w, "", http.StatusNotFound)
	}
}

func results(w http.ResponseWriter, r *http.Request) {
	id := r.FormValue("id")
	isPopup := r.FormValue("popup") == "1"
	p := Get(r, id)
	if p == nil {
		http.Error(w, "", http.StatusNotFound)
		return
	}

	needsSave := false
	if p.ClientTimings == nil {
		p.ClientTimings = getClientTimings(r)
		if p.ClientTimings != nil {
			needsSave = true
		}
	}
	if !p.HasUserViewed {
		p.HasUserViewed = true
		needsSave = true
	}

	if needsSave {
		Store(r, p)
	}

	var j []byte
	j, err := json.Marshal(p)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	if isPopup {
		w.Write(j)
	} else {
		v := struct {
			Name     string
			Duration float64
			Path     string
			Json     template.JS
			Includes template.HTML
			Version  string
		}{
			Name:     p.Name,
			Duration: p.DurationMilliseconds,
			Path:     PATH,
			Json:     template.JS(j),
			Includes: p.Includes(),
			Version:  Version,
		}

		shareHtml.Execute(w, v)
	}
}

func getClientTimings(r *http.Request) *ClientTimings {
	var navigationStart int64
	if i, err := strconv.ParseInt(r.FormValue(clientTimingsPrefix+"navigationStart]"), 10, 64); err != nil {
		return nil
	} else {
		navigationStart = i
	}
	ct := new(ClientTimings)

	if i, err := strconv.ParseInt(r.FormValue("clientPerformance[navigation][redirectCount]"), 10, 64); err == nil {
		ct.RedirectCount = i
	}

	r.ParseForm()
	clientPerf := make(map[string]ClientTiming)
	for k, v := range r.Form {
		if len(v) < 1 || !strings.HasPrefix(k, clientTimingsPrefix) {
			continue
		}

		if i, err := strconv.ParseInt(v[0], 10, 64); err == nil && i > navigationStart {
			i -= navigationStart
			name := k[len(clientTimingsPrefix) : len(k)-1]

			if strings.HasSuffix(name, "Start") {
				shortName := name[:len(name)-5]
				if c, present := clientPerf[shortName]; !present {
					clientPerf[shortName] = ClientTiming{
						Name:     shortName,
						Duration: -1,
						Start:    i,
					}
				} else {
					c.Start = i
					c.Duration -= i
					clientPerf[shortName] = c
				}
			} else if strings.HasSuffix(name, "End") {
				shortName := name[:len(name)-3]
				if c, present := clientPerf[shortName]; !present {
					clientPerf[shortName] = ClientTiming{
						Duration: i,
						Name:     shortName,
					}
				} else {
					c.Duration = i - c.Start
					clientPerf[shortName] = c
				}
			}
		}
	}
	for _, v := range clientPerf {
		ct.Timings = append(ct.Timings, &ClientTiming{
			Name:     sentenceCase(v.Name),
			Start:    v.Start,
			Duration: v.Duration,
		})
	}
	sort.Sort(ct)

	return ct
}

func sentenceCase(s string) string {
	var buf bytes.Buffer
	for k, v := range s {
		if k == 0 {
			buf.WriteRune(unicode.ToUpper(v))
			continue
		}
		if unicode.IsUpper(v) {
			buf.WriteString(" ")
		}
		buf.WriteRune(v)
	}
	return buf.String()
}

func static(w http.ResponseWriter, r *http.Request) {
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

// Includes renders the JavaScript includes for this request, if enabled.
func (p *Profile) Includes() template.HTML {
	if !Enable(p.r) {
		return ""
	}

	current := p.Id
	authorized := true

	v := struct {
		Ids                       string
		Path, Version, Position   string
		ShowTrivial, ShowChildren bool
		MaxTracesToShow           int
		ShowControls              bool
		CurrentId                 string
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

type Handler struct {
	f func(*Profile, http.ResponseWriter, *http.Request)
	p *Profile
}

// NewHandler returns a new profiled handler.
func NewHandler(f func(*Profile, http.ResponseWriter, *http.Request)) Handler {
	return Handler{
		f: f,
	}
}

func (h Handler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	h.p = NewProfile(w, r, FuncName(h.f))
	h.f(h.p, w, r)
	h.p.Finalize()
}

// Since returns the number of milliseconds since t.
func Since(t time.Time) float64 {
	d := time.Since(t)
	return float64(d.Nanoseconds()) / 1000000
}

// Hostname returns the os.Hostname() of the current machine,
// or "" if unavailable.
func Hostname() string {
	name, err := os.Hostname()
	if err != nil {
		return ""
	}
	return name
}

// FuncName returns the name of the function f, or "" if f is not a function.
func FuncName(f interface{}) string {
	v := reflect.ValueOf(f)
	if v.Kind() != reflect.Func {
		return ""
	}
	fp := v.Pointer()
	if fn := runtime.FuncForPC(fp); fn != nil {
		return fn.Name()
	}
	return ""
}
