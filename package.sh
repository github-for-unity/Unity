#!/bin/sh -xeu

Configuration="Release"
Publish="Publish"
case x"$2" in
	xdebug | xDebug)
		Configuration="Debug"
		Publish="PublishDebug"
		;;
esac

nuget restore
xbuild GitHub.Unity.sln /property:Configuration=$Configuration

Unity=""
if [ -f $1/Unity.app/Contents/MacOS/Unity ]; then
	Unity="$1/Unity.app/Contents/MacOS/Unity"
elif [ -f $1/Unity ]; then
	Unity="$1/Unity"
else
	echo "Can't find Unity in $1"
	exit 1
fi
rm unity/PackageProject/Assets/Editor/GitHub/CopyLibraries*
export GITHUB_UNITY_DISABLE=1
$Unity -batchmode -projectPath `pwd`/unity/PackageProject -exportPackage Assets/Editor/GitHub github-for-unity-windows.unitypackage -force-free -quit
