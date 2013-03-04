package miniprofiler

import (
	"code.google.com/p/tcgl/identifier"
	"encoding/json"
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
}

/* todo: figure out why this is broken
func ProfileFromGob(b []byte) *Profile {
	p := Profile{}
	fmt.Println("B", len(b))
	buf := bytes.NewBuffer(b)
	gob.NewDecoder(buf).Decode(&p)
	if err := gob.NewDecoder(bytes.NewBuffer(b)).Decode(&p); err != nil {
		fmt.Println("GOB ERR", err)
		return nil
	}
	fmt.Println("PROFILE:", p)
	return &p
}

func (p *Profile) Gob() []byte {
	var buf bytes.Buffer
	if err := gob.NewEncoder(&buf).Encode(p); err != nil {
		return nil
	}
	return buf.Bytes()
}
*/

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
