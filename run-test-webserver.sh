#!/bin/sh -eu
PORT="50000"
if [ $# -eq 1 ]; then
	PORT="$1"
fi

EXEC="mono "
if [ -e "/c/" ]; then
	EXEC=""
fi

if [ ! -e build/CommandLine/CommandLine.exe ]; then
	>&2 xbuild /target:CommandLine GitHub.Unity.sln /verbosity:minimal
fi

"$EXEC"build/CommandLine/CommandLine.exe --web --port $PORT
