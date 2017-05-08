#!/bin/sh -eux
Configuration="Debug"
if [ $# -gt 0 ]; then
	Configuration=$1
fi

Target="Build"
if [ $# -gt 1 ]; then
	Target=$2
fi

if [ x"$Target" == x"Rebuild" ]; then
	rm -f unity/PackageProject/Assets/Editor/GitHub/*.dll
	rm -f unity/PackageProject/Assets/Editor/GitHub/*.mdb
	rm -f unity/PackageProject/Assets/Editor/GitHub/*.pdb

	if [ -e ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub ]; then
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/*.dll
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/*.mdb
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/*.pdb
	fi
fi

OS="Mac"
if [ -e "/c/" ]; then
	OS="Windows"
fi

if [ x"$OS" == x"Windows" ]; then
	common/nuget restore
else
	nuget restore
fi

xbuild GitHub.Unity.sln /property:Configuration=$Configuration /target:$Target

cp -r unity/PackageProject/Assets/Editor/GitHub ../github-unity-test/GitHubExtensionProject/Assets/Editor || true

rm -f unity/PackageProject/Assets/Editor/GitHub/deleteme*
rm -f unity/PackageProject/Assets/Editor/GitHub/deleteme*
rm -f unity/PackageProject/Assets/Editor/GitHub/*.xml

if [ -e ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub ]; then
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/deleteme*
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/deleteme*
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/*.xml
fi