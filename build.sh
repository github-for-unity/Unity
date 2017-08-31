#!/bin/sh -eux
Configuration="dev"
if [ $# -gt 0 ]; then
	Configuration=$1
fi

Target="Build"
if [ $# -gt 1 ]; then
	Target=$2
fi

if [ x"$Target" == x"Rebuild" ]; then
	rm -f unity/PackageProject/Assets/GitHub/Editor/*.dll
	rm -f unity/PackageProject/Assets/GitHub/Editor/*.mdb
	rm -f unity/PackageProject/Assets/GitHub/Editor/*.pdb

	if [ -e ../github-unity-test/GitHubExtensionProject/Assets/GitHub/Editor ]; then
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/GitHub/Editor/*.dll
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/GitHub/Editor/*.mdb
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/GitHub/Editor/*.pdb
	fi
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

xbuild GitHub.Unity.sln /verbosity:normal /property:Configuration=$Configuration /target:$Target || true

rm -f unity/PackageProject/Assets/GitHub/Editor/deleteme*
rm -f unity/PackageProject/Assets/GitHub/Editor/deleteme*
rm -f unity/PackageProject/Assets/GitHub/Editor/*.xml

cp -r unity/PackageProject/Assets/GitHub ../github-unity-test/GitHubExtensionProject/Assets/ || true
cp -r unity/PackageProject/Assets/GitHub.meta ../github-unity-test/GitHubExtensionProject/Assets/ || true


if [ -e ../github-unity-test/GitHubExtensionProject/Assets/GitHub/Editor ]; then
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/GitHub/Editor/deleteme*
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/GitHub/Editor/deleteme*
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/GitHub/Editor/*.xml
fi