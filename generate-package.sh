#!/bin/sh -eu
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
if [ $# -lt 3 ]; then
	echo "Usage: generate-package.sh [git|lfs|ghu] [version] [path to file] [host url (optional)] [release notes file (optional)] [message file (optional)]"
	exit 1
fi

URL="http://ghfvs-installer.github.com"
if [ $# -ge 4 ]; then
	URL=$4
fi

if [ "$1" == "git" ]; then
	URL="$URL/unity/git"
fi
if [ "$1" == "lfs" ]; then
	URL="$URL/unity/git"
fi
if [ "$1" == "ghu" ]; then
	URL="$URL/unity/releases"
fi

RN=""
MSG=""
if [ $# -ge 5 ]; then
	RN="$5"
fi

if [ $# -ge 6 ]; then
	MSG="$6"
fi

EXEC="mono"
if [ -e "/c/" ]; then
	EXEC=""
fi

if [ ! -e "$DIR/build/CommandLine/CommandLine.exe" ]; then
	>&2 xbuild /target:CommandLine "$DIR/GitHub.Unity.sln" /verbosity:minimal
fi

"$EXEC""$DIR/build/CommandLine/CommandLine.exe" --gen-package --version "$2" --path "$3" --url "$URL" --rn "$RN" --msg "$MSG"
