#!/bin/sh -eu
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
if [ $# -lt 3 ]; then
	echo "Usage: generate-package.sh [git|lfs|ghu] [version] [path to file] [host url (optional)] [release notes file (optional)] [message file (optional)]"
	exit 1
fi

LFS_MD5="4294df6cbb467b8133553570450757c7"
GIT_MD5="50570ed932559f294d1a1361801740b9"
MD5=""

URL="http://ghfvs-installer.github.com"
if [ $# -ge 4 ]; then
	URL=$4
fi

if [ "$1" == "git" ]; then
	MD5=$GIT_MD5
	URL="$URL/unity/git"
fi
if [ "$1" == "lfs" ]; then
	MD5=$LFS_MD5
	URL="$URL/unity/git"
fi
if [ "$1" == "ghu" ]; then
	MD5=
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

"$EXEC""$DIR/build/CommandLine/CommandLine.exe" --gen-package --version "$2" --path "$3" --url "$URL" --md5 "$MD5" --rn "$RN" --msg "$MSG"
