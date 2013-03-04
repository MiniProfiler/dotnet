package miniprofiler_gae

import (
	"appengine"
	"appengine/memcache"
	"appengine/user"
	"bytes"
	"fmt"
	"github.com/mjibson/MiniProfiler/go/miniprofiler"
	"github.com/mjibson/appstats"
	"net/http"
	"strings"
)

func init() {
	miniprofiler.Enable = EnableIfAdminOrDev
	miniprofiler.Get = GetMemcache
	miniprofiler.Store = StoreMemcache
	miniprofiler.MachineName = Instance
}

func EnableIfAdminOrDev(r *http.Request) bool {
	if appengine.IsDevAppServer() {
		return true
	}
	c := appengine.NewContext(r)
	u := user.Current(c)
	return u.Admin
}

func Instance() string {
	return appengine.InstanceID()
}

func StoreMemcache(r *http.Request, p *miniprofiler.Profile) {
	item := &memcache.Item{
		Key:   mp_key(string(p.Id)),
		Value: p.Json(),
	}
	c := appengine.NewContext(r)
	memcache.Set(c, item)
}

func GetMemcache(r *http.Request, id string) *miniprofiler.Profile {
	c := appengine.NewContext(r)
	item, err := memcache.Get(c, mp_key(id))
	if err != nil {
		return nil
	}
	return miniprofiler.ProfileFromJson(item.Value)
}

type Context struct {
	appstats.Context
	P *miniprofiler.Profile
}

func NewHandler(f func(Context, http.ResponseWriter, *http.Request)) appstats.Handler {
	return appstats.NewHandler(func(c appengine.Context, w http.ResponseWriter, r *http.Request) {
		pc := Context{
			Context: c.(appstats.Context),
		}

		if miniprofiler.Enabled(r) {
			pc.P = miniprofiler.NewProfile(w, r, miniprofiler.FuncName(f))
			f(pc, w, r)

			for _, v := range pc.Context.Stats.RPCStats {
				pc.P.Root.AddSqlTiming(&miniprofiler.SqlTiming{
					Id:                     miniprofiler.NewGuid(),
					ExecuteType:            miniprofiler.ExecuteType_Reader,
					FormattedCommandString: v.Name() + "\n\n" + v.Request(),
					StartMilliseconds:      float64(v.Offset.Nanoseconds()) / 1000000,
					DurationMilliseconds:   float64(v.Duration.Nanoseconds()) / 1000000,
					StackTraceSnippet:      stackSnippet(v.Stack()),
				})
			}

			pc.P.Finalize()
		} else {
			f(pc, w, r)
		}
	})
}

func mp_key(id string) string {
	return fmt.Sprintf("mini-profiler-results:%s", id)
}

func stackSnippet(s appstats.Stack) string {
	var b bytes.Buffer

	for i, f := range s {
		if i > 0 {
			b.WriteString(" ")
		}
		idx := strings.Index(f.Call, ":")
		if idx == -1 {
			continue
		}
		c := f.Call[:idx]
		idx = strings.LastIndex(f.Call, "/")
		if idx != -1 {
			c = c[idx+1:]
		}
		b.WriteString(c)
	}

	return b.String()
}
