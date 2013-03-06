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
	"code.google.com/p/tcgl/identifier"
	"encoding/json"
	"fmt"
	"net/http"
	"time"
)

const (
	ExecuteType_None     = 0
	ExecuteType_NonQuery = 1
	ExecuteType_Scalar   = 2
	ExecuteType_Reader   = 3
)

func newGuid() string {
	return identifier.NewUUID().String()
}

type Profile struct {
	Id                                   string
	Name                                 string
	start                                time.Time
	Started                              string
	MachineName                          string
	Level                                int
	Root                                 *Timing
	User                                 string
	HasUserViewed                        bool
	ClientTimings                        *ClientTimings
	DurationMilliseconds                 float64
	HasTrivialTimings                    bool
	HasAllTrivialTimings                 bool
	TrivialDurationThresholdMilliseconds float64
	Head                                 *Timing
	DurationMillisecondsInSql            float64
	ExecutedNonQueries                   int
	ExecutedReaders                      int
	ExecutedScalars                      int
	HasDuplicateSqlTimings               bool
	HasSqlTimings                        bool
	CustomTimingStats                    map[string]*CustomTimingStat
	CustomTimingNames                    []string
	CustomLink                           string
	CustomLinkName                       string

	w http.ResponseWriter
	r *http.Request

	current *Timing
}

// NewProfile creates a new Profile with given name.
// For use only by miniprofiler extensions.
func NewProfile(w http.ResponseWriter, r *http.Request, name string) *Profile {
	p := &Profile{
		Id:          newGuid(),
		Name:        name,
		start:       time.Now(),
		MachineName: MachineName(),
		Root: &Timing{
			Id:     newGuid(),
			IsRoot: true,
		},

		w: w,
		r: r,
	}
	p.current = p.Root

	w.Header().Add("X-MiniProfiler-Ids", fmt.Sprintf("[\"%s\"]", p.Id))

	return p
}

// Finalize finalizes a Profile and Store()s it.
// For use only by miniprofiler extensions.
func (p *Profile) Finalize() {
	u := p.r.URL
	if !u.IsAbs() {
		u.Host = p.r.Host
		if p.r.TLS == nil {
			u.Scheme = "http"
		} else {
			u.Scheme = "https"
		}
	}
	p.Root.Name = p.r.Method + " " + u.String()

	p.Started = fmt.Sprintf("/Date(%d)/", p.start.Unix()*1000)
	p.DurationMilliseconds = Since(p.start)
	p.Root.DurationMilliseconds = p.DurationMilliseconds

	customNames := make(map[string]bool)
	timings := []*Timing{p.Root}
	for i := 0; i < len(timings); i++ {
		t := timings[i]
		timings = append(timings, t.Children...)

		t.DurationWithoutChildrenMilliseconds = t.DurationMilliseconds
		for _, c := range t.Children {
			t.DurationWithoutChildrenMilliseconds -= c.DurationMilliseconds
		}

		if t.HasSqlTimings {
			p.HasSqlTimings = true
		}

		for _, r := range t.SqlTimings {
			p.DurationMillisecondsInSql += r.DurationMilliseconds
			t.SqlTimingsDurationMilliseconds += r.DurationMilliseconds
			switch r.ExecuteType {
			case ExecuteType_NonQuery:
				p.ExecutedNonQueries++
				t.ExecutedNonQueries++
			case ExecuteType_Scalar:
				p.ExecutedScalars++
				t.ExecutedScalars++
			case ExecuteType_Reader:
				p.ExecutedReaders++
				t.ExecutedReaders++
			}
		}

		for n, c := range t.CustomTimingStats {
			customNames[n] = true
			if p.CustomTimingStats == nil {
				p.CustomTimingStats = make(map[string]*CustomTimingStat)
			}
			pc := p.CustomTimingStats[n]
			if pc == nil {
				pc = new(CustomTimingStat)
				p.CustomTimingStats[n] = pc
			}
			pc.Count += c.Count
			pc.Duration += c.Duration
		}
	}

	for n := range customNames {
		p.CustomTimingNames = append(p.CustomTimingNames, n)
	}

	Store(p.r, p)
}

// ProfileFromJson returns a Profile from JSON data.
func ProfileFromJson(b []byte) *Profile {
	p := Profile{}
	json.Unmarshal(b, &p)
	return &p
}

// Json converts a profile to JSON.
func (p *Profile) Json() []byte {
	b, _ := json.Marshal(p)
	return b
}

// Step adds a new child node with given name.
// f should generally be in a closure.
func (p *Profile) Step(name string, f func()) {
	t := &Timing{
		Id:                newGuid(),
		Name:              name,
		ParentTimingId:    p.current.Id,
		Depth:             p.current.Depth + 1,
		StartMilliseconds: Since(p.start),
	}
	p.current.HasChildren = true
	p.current.Children = append(p.current.Children, t)
	t.parent = p.current
	p.current = t

	f()

	t.DurationMilliseconds = Since(p.start) - t.StartMilliseconds
	p.current = p.current.parent
}

// AddSqlTiming adds a new SqlTiming to the current node.
func (p *Profile) AddSqlTiming(s *SqlTiming) {
	t := p.current
	s.ParentTimingId = t.Id
	t.SqlTimings = append(t.SqlTimings, s)
	t.HasSqlTimings = true
}

// AddCustomTiming adds a new CustomTiming with given type to the current node.
func (p *Profile) AddCustomTiming(Type string, s *CustomTiming) {
	t := p.current
	if t.CustomTimings == nil {
		t.CustomTimings = make(map[string][]*CustomTiming)
		t.CustomTimingStats = make(map[string]*CustomTimingStat)
	}

	s.ParentTimingId = t.Id
	t.CustomTimings[Type] = append(t.CustomTimings[Type], s)
	c := t.CustomTimingStats[Type]
	if c == nil {
		c = new(CustomTimingStat)
		t.CustomTimingStats[Type] = c
	}
	c.Count++
	c.Duration += s.DurationMilliseconds
}

type Timing struct {
	Id                                  string
	Name                                string
	DurationMilliseconds                float64
	StartMilliseconds                   float64
	Children                            []*Timing
	KeyValues                           map[string]string
	SqlTimings                          []*SqlTiming
	ParentTimingId                      string
	DurationWithoutChildrenMilliseconds float64
	SqlTimingsDurationMilliseconds      float64
	IsTrivial                           bool
	HasChildren                         bool
	HasSqlTimings                       bool
	HasDuplicateSqlTimings              bool
	IsRoot                              bool
	Depth                               int
	ExecutedReaders                     int
	ExecutedScalars                     int
	ExecutedNonQueries                  int
	CustomTimingStats                   map[string]*CustomTimingStat
	CustomTimings                       map[string][]*CustomTiming

	parent *Timing
}

type SqlTiming struct {
	Id                             string
	ExecuteType                    int
	CommandString                  string
	FormattedCommandString         string
	StackTraceSnippet              string
	StartMilliseconds              float64
	DurationMilliseconds           float64
	FirstFetchDurationMilliseconds float64
	Parameters                     []*SqlTimingParameter
	ParentTimingId                 string
	IsDuplicate                    bool
}

type SqlTimingParameter struct {
	ParentSqlTimingId string
	Name              string
	Value             string
	DbType            string
	Size              int
}

type ClientTimings struct {
	RedirectCount int64
	Timings       []*ClientTiming
}

func (c *ClientTimings) Len() int           { return len(c.Timings) }
func (c *ClientTimings) Less(i, j int) bool { return c.Timings[i].Start < c.Timings[j].Start }
func (c *ClientTimings) Swap(i, j int)      { c.Timings[i], c.Timings[j] = c.Timings[j], c.Timings[i] }

type ClientTiming struct {
	Name     string
	Start    int64
	Duration int64
}

type CustomTiming struct {
	ParentTimingId       string
	StartMilliseconds    float64
	DurationMilliseconds float64
}

type CustomTimingStat struct {
	Duration float64
	Count    int
}
