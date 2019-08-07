#!/bin/sh -eu
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

commitcount=$(git rev-list  --count HEAD)
commit=$(git log -n1 --pretty=format:%h)

version=$(sed -En 's,.*AssemblyInformationalVersion = "(.*)".*,\1,p' build/obj/Release/net35/GitHub.Unity.Version.cs)
version35="${version}.${commitcount}-${commit}"
version=$(sed -En 's,.*AssemblyInformationalVersion = "(.*)".*,\1,p' build/obj/Release/net471/GitHub.Unity.Version.cs)
version471="${version}.${commitcount}-${commit}"

commitcount=$(cd src/git-for-unity && git rev-list  --count HEAD)
commit=$(cd src/git-for-unity && git log -n1 --pretty=format:%h)
version=$(sed -En 's,.*AssemblyInformationalVersion = "(.*)".*,\1,p' build/obj/Release/net471/Unity.VersionControl.Git.Version.cs)
versiongitapi="${version}.${commitcount}-${commit}"

$DIR/submodules/packaging/unitypackage/run.sh --path "$DIR/unity/GHfU-net35" --out "$DIR" --file github-for-unity-net20-$version35 --project
$DIR/submodules/packaging/unitypackage/run.sh --path "$DIR/unity/GHfU-net471" --out "$DIR" --file github-for-unity-$version471 --project

$DIR/src/git-for-unity/create-unity-packages/run.sh --path "$DIR/packages/com.github.ui" --out "$DIR" --name com.github.ui --version "$version471" --ignores "$DIR/packages/com.github.ui/.npmignore"
$DIR/src/git-for-unity/create-unity-packages/run.sh --path "$DIR/packages/com.unity.git.api" --out "$DIR" --name com.unity.git.api --version "$versiongitapi" --ignores "$DIR/packages/com.unity.git.api/.npmignore"
