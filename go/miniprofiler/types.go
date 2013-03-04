package miniprofiler

import (
	"code.google.com/p/tcgl/identifier"
	"encoding/json"
	"fmt"
	"net/http"
	"time"
)

const (
	ProfileLevel_Info    = 0
	ProfileLevel_Verbose = 1

	ExecuteType_None     = 0
	ExecuteType_NonQuery = 1
	ExecuteType_Scalar   = 2
	ExecuteType_Reader   = 3
)

type Guid string

func NewGuid() Guid {
	u := identifier.NewUUID()
	return Guid(u.String())
}

type Profile struct {
	Id                                   Guid
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

	w http.ResponseWriter
	r *http.Request
}

func NewProfile(w http.ResponseWriter, r *http.Request, name string) *Profile {
	p := &Profile{
		Id:          NewGuid(),
		Name:        name,
		start:       time.Now(),
		MachineName: Hostname(),
		Root: &Timing{
			Id:     NewGuid(),
			IsRoot: true,
		},

		w: w,
		r: r,
	}

	w.Header().Add("X-MiniProfiler-Ids", fmt.Sprintf("[\"%s\"]", p.Id))

	return p
}

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
	p.Root.Name = u.String()

	p.Started = fmt.Sprintf("/Date(%d)/", p.start.Unix()*1000)
	p.DurationMilliseconds = Since(p.start)
	p.Root.DurationMilliseconds = p.DurationMilliseconds

	timings := []*Timing{p.Root}
	for i := 0; i < len(timings); i++ {
		t := timings[i]
		timings = append(timings, t.Children...)

		if t.HasSqlTimings {
			p.HasSqlTimings = true
		}

		for _, r := range t.SqlTimings {
			p.DurationMillisecondsInSql += r.DurationMilliseconds
			switch r.ExecuteType {
			case ExecuteType_NonQuery:
				p.ExecutedNonQueries++
			case ExecuteType_Scalar:
				p.ExecutedScalars++
			case ExecuteType_Reader:
				p.ExecutedReaders++
			}
		}
	}

	Store(p.r, p)
}

func ProfileFromJson(b []byte) *Profile {
	p := Profile{}
	json.Unmarshal(b, &p)
	return &p
}

func (p *Profile) Json() []byte {
	b, _ := json.Marshal(p)
	return b
}

type Timing struct {
	Id                                  Guid
	Name                                string
	DurationMilliseconds                float64
	StartMilliseconds                   float64
	Children                            []*Timing
	KeyValues                           map[string]string
	SqlTimings                          []*SqlTiming
	ParentTimingId                      Guid
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
}

func (t *Timing) AddSqlTiming(s *SqlTiming) {
	s.ParentTimingId = t.Id
	t.SqlTimings = append(t.SqlTimings, s)
	t.HasSqlTimings = true
}

type SqlTiming struct {
	Id                             Guid
	ExecuteType                    int
	CommandString                  string
	FormattedCommandString         string
	StackTraceSnippet              string
	StartMilliseconds              float64
	DurationMilliseconds           float64
	FirstFetchDurationMilliseconds float64
	Parameters                     []*SqlTimingParameter
	ParentTimingId                 Guid
	IsDuplicate                    bool
}

type SqlTimingParameter struct {
	ParentSqlTimingId Guid
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
