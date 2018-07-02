#!/bin/sh -eu
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

URL="http://ghfvs-installer.github.com"
GIT=0
LFS=0
GHU=0
FILE=""
VERSION=""
RN=""
MSG=""
OS=""

while [[ $# -gt 0 ]]
do
key="$1"

case $key in
    -h)
    URL="$2"
    shift
    shift
    ;;
    -p)
    FILE="--path $2"
    shift
    shift
    ;;
    -v)
    VERSION="--version $2"
    shift
    shift
    ;;
    -r)
    RN="--rn $2"
    shift
    shift
    ;;
    -m)
    MSG="--msg $2"
    shift
    shift
    ;;
    -git)
    GIT=1
    shift # past value
    ;;
    -lfs)
    LFS=1
    shift # past value
    ;;
    -ghu)
    GHU=1
    shift # past value
    ;;
    -windows)
    OS="windows"
    shift # past value
    ;;
    -mac)
    OS="mac"
    shift # past value
    ;;
    -linux)
    OS="linux"
    shift # past value
    ;;
esac
done

if [ x"$GIT" = "x0" -a x"$LFS" = "x0" -a x"$GHU" = "x0" ]; then
    echo "Usage: generate-package.sh [-git|-lfs|-ghu] [-windows|-mac|-linux only if -git or -lfs] [-v version] [-p path to file] [-h host url (optional)] [-r release notes file (optional)] [-m message file (optional)]"
    exit 1
fi

if [ x"$GHU" = x"1" ]; then
    URL="--url $URL/unity/releases"
else
    URL="--url $URL/unity/git/$OS"
fi

EXEC=
if [ ! -e "/c/" ]; then
    EXEC=mono
fi

if [ ! -e "build/CommandLine/CommandLine.exe" ]; then
    >&2 xbuild /target:CommandLine "$DIR/GitHub.Unity.sln" /verbosity:minimal
fi

$EXEC build/CommandLine/CommandLine.exe --gen-package $VERSION $FILE $URL $RN $MSG
