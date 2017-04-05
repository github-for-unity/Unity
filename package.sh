#!/bin/sh -x

Configuration="Release"
Publish="Publish"
case x"$2" in
	xdebug | xDebug)
		Configuration="Debug"
		Publish="PublishDebug"
		;;
esac

xbuild GitHub.Unity.sln /Configuration:$Configuration
xbuild GitHub.Unity.sln /Configuration:$Publish

Unity=""
if [ -f $1/Unity.app/Contents/MacOS/Unity ]; then
	Unity="$1/Unity.app/Contents/MacOS/Unity"
elif [ -f $1/Unity ]; then
	Unity="$1/Unity"
else
	echo "Can't find Unity in $1"
	exit 1
fi

export GITHUB_UNITY_DISABLE=1
$Unity -batchmode -projectPath `pwd`/unity/PackageProject -exportPackage Assets/Editor/GitHub github-for-unity-windows.unitypackage -force-free -quit
