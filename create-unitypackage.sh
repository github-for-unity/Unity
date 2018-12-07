#!/bin/sh -eu
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

version=$(sed -En 's,.*Version = "(.*)".*,\1,p' common/SolutionInfo.cs)
commitcount=$(git rev-list  --count HEAD)
commit=$(git log -n1 --pretty=format:%h)
version="${version}.${commitcount}-${commit}"

$DIR/submodules/packaging/unitypackage/run.sh --path $DIR/unity/PackageProject --out $DIR --file github-for-unity-$version
