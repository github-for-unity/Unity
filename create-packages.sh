#!/bin/sh -eux
rootDirectory="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

outdir="$rootDirectory/artifacts"

uiVersion=$(sed -En 's,.*AssemblyInformationalVersion = "(.*)".*,\1,p' build/obj/Release/net471/GitHub.Unity.Version.cs)
apiVersion=$(sed -En 's,.*AssemblyInformationalVersion = "(.*)".*,\1,p' build/obj/Release/net471/Unity.VersionControl.Git.Version.cs)

packageName="github-for-unity"

name1="com.github.ui"
path1="$rootDirectory/build/packages/$name1"
extras1="$rootDirectory/src/extras/$name1"
ignores1="$rootDirectory/build/packages/$name1/.npmignore"
version1="$uiVersion"

name2="com.unity.git.api"
path2="$rootDirectory/build/packages/$name2"
extras2="$rootDirectory/src/git-for-unity/src/extras/$name2"
ignores2="$rootDirectory/build/packages/$name2/.npmignore"
version2="$apiVersion"

$rootDirectory/submodules/packaging/unitypackage/run.sh --path "$rootDirectory/unity/GHfU-net35" --out "$outdir" --file $packageName-net20-$uiVersion
$rootDirectory/submodules/packaging/unitypackage/run.sh --path "$rootDirectory/unity/GHfU-net471" --out "$outdir" --file $packageName-$uiVersion

$rootDirectory/src/git-for-unity/packaging/create-unity-packages/run.sh --out "$outdir" --name "$name1" --version "$version1" --path "$path1" --extras "$extras1" --ignores "$ignores1"
$rootDirectory/src/git-for-unity/packaging/create-unity-packages/run.sh --out "$outdir" --name "$name2" --version "$version2" --path "$path2" --extras "$extras2" --ignores "$ignores2"

$rootDirectory/src/git-for-unity/packaging/create-unity-packages/multipackage.sh --out "$outdir" --name "$packageName" --version "$uiVersion" --path1 "$path1" --extras1 "$extras1" --ignores1 "$ignores1" --path2 "$path2" --extras2 "$extras2" --ignores2 "$ignores2"
