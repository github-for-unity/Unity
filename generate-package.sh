#!/bin/sh -eu
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
if [ $# -lt 3 ]; then
	echo "Usage: generate-package.sh [git|lfs|ghu] [windows|mac|linux] [version] [path to file] [host url (optional)] [release notes file (optional)] [message file (optional)]"
	exit 1
fi

URL="http://ghfvs-installer.github.com"
if [ $# -ge 5 ]; then
	URL=$5
fi

if [ "$1" == "git" ]; then
	URL="$URL/unity/git/$2"
fi
if [ "$1" == "lfs" ]; then
	URL="$URL/unity/git/$2"
fi
if [ "$1" == "ghu" ]; then
	URL="$URL/unity/releases"
fi

RN=""
MSG=""
if [ $# -ge 6 ]; then
	RN="$6"
fi

if [ $# -ge 7 ]; then
	MSG="$7"
fi

EXEC="mono "
if [ -e "/c/" ]; then
	EXEC=""
fi

if [ ! -e "build/CommandLine/CommandLine.exe" ]; then
	>&2 xbuild /target:CommandLine "$DIR/GitHub.Unity.sln" /verbosity:minimal
fi

$EXEC build/CommandLine/CommandLine.exe --gen-package --version "$3" --path "$4" --url "$URL" --rn "$RN" --msg "$MSG"
