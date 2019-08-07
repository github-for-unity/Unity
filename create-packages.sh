#!/bin/sh -eux
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

outdir="$DIR/artifacts"

version35=$(sed -En 's,.*AssemblyInformationalVersion = "(.*)".*,\1,p' build/obj/Release/net35/GitHub.Unity.Version.cs)
version471=$(sed -En 's,.*AssemblyInformationalVersion = "(.*)".*,\1,p' build/obj/Release/net471/GitHub.Unity.Version.cs)
versiongitapi=$(sed -En 's,.*AssemblyInformationalVersion = "(.*)".*,\1,p' build/obj/Release/net471/Unity.VersionControl.Git.Version.cs)

#$DIR/submodules/packaging/unitypackage/run.sh --path "$DIR/unity/GHfU-net35" --out "$outdir" --file github-for-unity-net20-$version35
#$DIR/submodules/packaging/unitypackage/run.sh --path "$DIR/unity/GHfU-net471" --out "$outdir" --file github-for-unity-$version471

$DIR/src/git-for-unity/packaging/create-unity-packages/run.sh --path "$DIR/build/packages/com.github.ui" --out "$outdir" --name com.github.ui --version $version471 --ignores "$DIR/build/packages/com.github.ui/.npmignore"
$DIR/src/git-for-unity/packaging/create-unity-packages/run.sh --path "$DIR/build/packages/com.unity.git.api" --out "$outdir" --name com.unity.git.api --version "$versiongitapi" --ignores "$DIR/build/packages/com.unity.git.api/.npmignore"
