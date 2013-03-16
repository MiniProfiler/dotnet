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

/*
Package miniprofiler_gae is a simple but effective mini-profiler for app engine.

miniprofiler_gae hooks into the appstats package, and all app engine RPCs are automatically profiled.
An appstats link is listed in each Profile.

Installation

In your main .go file:

    import mpg "github.com/mjibson/MiniProfiler/go/miniprofiler_gae"

Change all handler functions to the following signature:

    func(mpg.Context, http.ResponseWriter, *http.Request)

Wrap all calls to those functions in the miniprofiler.NewHandler wrapper:

    http.Handle("/", mpg.NewHandler(Main))

Send output of miniprofiler.Includes to your HTML (it is empty if Enable returns
false).

By default, miniprofiler is enabled on dev for all and on prod for admins.
Override miniprofiler.Enable to change.

Step

The Step function can be used to profile more specific parts of your code. It
should be called with the name of the step and a closure:

    c.P.Step("something", func() {
        // do some work
    })

Configuration

Refer to the variables section of the documentation: http://godoc.org/github.com/mjibson/MiniProfiler/go/miniprofiler#_variables.

Also refer to the base miniprofiler docs: http://godoc.org/github.com/mjibson/MiniProfiler/go/miniprofiler.

Other implementations and resources: http://miniprofiler.com.
*/
package miniprofiler_gae
