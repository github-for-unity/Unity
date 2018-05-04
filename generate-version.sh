#!/bin/sh -eu
if [ $# -lt 1 ]; then
	echo "Usage: generate-version.sh [version] [host url (default: http://ghfvs-installer.github.com)]"
	exit 1
fi

URL="http://ghfvs-installer.github.com"
if [ $# -eq 2 ]; then
	URL=$2
fi

EXEC="mono"
if [ -e "/c/" ]; then
	EXEC=""
fi

if [ ! -e build/CommandLine/CommandLine.exe ]; then
	>&2 xbuild /target:CommandLine GitHub.Unity.sln /verbosity:minimal
fi

"$EXEC" build/CommandLine/CommandLine.exe -g -v "$1" -u "$URL"
