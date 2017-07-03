#!/bin/sh -eu

Configuration="Release"
if [ $# -lt 1 ]; then
	echo "Need path to Unity"
	exit 1
fi

if [ $# -gt 1 ]; then
	case x"$2" in
		xdebug | xDebug)
			Configuration="Debug"
			;;
	esac
fi

pushd unity/PackageProject/Assets
git clean -xdf
popd

pushd src
git clean -xdf
popd

if [ -f "$1/Unity.app/Contents/MacOS/Unity" ]; then
	Unity="$1/Unity.app/Contents/MacOS/Unity"
elif [ -f $1/Unity ]; then
	Unity="$1/Unity"
else
	echo "Can't find Unity in $1"
	exit 1
fi

OS="Mac"
if [ -e "/c/" ]; then
	OS="Windows"
fi

if [ x"$OS" == x"Windows" ]; then
	common/nuget restore GitHub.Unity.sln
else
	nuget restore GitHub.Unity.sln
fi
xbuild GitHub.Unity.sln /property:Configuration=$Configuration


rm -f unity/PackageProject/Assets/Editor/GitHub/deleteme*
rm -f unity/PackageProject/Assets/Editor/GitHub/*.pdb
rm -f unity/PackageProject/Assets/Editor/GitHub/*.pdb.meta
rm -f unity/PackageProject/Assets/Editor/GitHub/*.xml

Version=`sed -En 's,.*Version = "(.*)".*,\1,p' common/SolutionInfo.cs`
export GITHUB_UNITY_DISABLE=1
"$Unity" -batchmode -projectPath "`pwd`/unity/PackageProject" -exportPackage Assets/Editor/GitHub github-for-unity-$Version-alpha.unitypackage -force-free -quit
