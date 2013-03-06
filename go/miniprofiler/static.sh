#!/bin/sh

cd ../../StackExchange.Profiling/UI
bin2go -a -p miniprofiler -s ../../go/miniprofiler/static.go includes.js jquery.tmpl.js includes.css jquery.1.7.1.js includes.tmpl
