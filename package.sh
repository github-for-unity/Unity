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

FrameworkVersion="v3.5"
if [ $# -gt 2 ]; then
	FrameworkVersion=$3
fi

pushd unity/PackageProject/Assets
git clean -xdf
popd

pushd src
git clean -xdf
popd

OS="Mac"
if [ -e "c:\\" ]; then
	OS="Windows"
fi

if [ x"$OS" == x"Windows" ]; then
	if [ -f "$1/Editor/Unity.exe" ]; then
		Unity="$1/Editor/Unity.exe"
	else
		echo "Can't find Unity in $1"
		exit 1
	fi
else
	if [ -f "$1/Unity.app/Contents/MacOS/Unity" ]; then
		Unity="$1/Unity.app/Contents/MacOS/Unity"
	elif [ -f "$1/Unity" ]; then
		Unity="$1/Unity"
	else
		echo "Can't find Unity in $1"
		exit 1
	fi
fi

if [ x"$OS" == x"Windows" ]; then
	common/nuget restore GitHub.Unity.sln
else
	nuget restore GitHub.Unity.sln
fi

xbuild GitHub.Unity.sln /property:Configuration=$Configuration /property:TargetFrameworkVersion=$FrameworkVersion

rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/deleteme*
rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/*.pdb
rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/*.pdb.meta
rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/*.xml

# There should be a better way to deal with these
if [ x"$FrameworkVersion" != x"v3.5" ]; then
	mv unity/PackageProject/Assets/Plugins/GitHub/Editor/AsyncBridge.Net35.dll.meta ./
	mv unity/PackageProject/Assets/Plugins/GitHub/Editor/ReadOnlyCollectionsInterfaces.dll.meta ./
	mv unity/PackageProject/Assets/Plugins/GitHub/Editor/System.Threading.dll.meta ./

	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/AsyncBridge.Net35.dll.meta
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/ReadOnlyCollectionsInterfaces.dll.meta
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/System.Threading.dll.meta
fi

Version=`sed -En 's,.*Version = "(.*)".*,\1,p' common/SolutionInfo.cs`
commitcount=`git rev-list  --count HEAD`
commit=`git log -n1 --pretty=format:%h`
DotNetVersion=${FrameworkVersion:1}
DotNetVersion="$(echo $DotNetVersion | tr -d .)"
Version="${Version}.${commitcount}-${commit}.net${DotNetVersion}"
Version=$Version

export GITHUB_UNITY_DISABLE=1
"$Unity" -batchmode -projectPath "`pwd`/unity/PackageProject" -exportPackage Assets/Plugins/GitHub/Editor github-for-unity-$Version.unitypackage -force-free -quit

if [ x"$FrameworkVersion" != x"v3.5" ]; then
	mv ./AsyncBridge.Net35.dll.meta unity/PackageProject/Assets/Plugins/GitHub/Editor/
	mv ./ReadOnlyCollectionsInterfaces.dll.meta unity/PackageProject/Assets/Plugins/GitHub/Editor/
	mv ./System.Threading.dll.meta unity/PackageProject/Assets/Plugins/GitHub/Editor/
fi
