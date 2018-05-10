#!/bin/sh -eu
Configuration="dev"
if [ $# -gt 0 ]; then
	Configuration=$1
fi

Target="Build"
if [ $# -gt 1 ]; then
	Target=$2
fi

if [ x"$Target" == x"Rebuild" ]; then
	rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/*.dll
	rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/*.mdb
	rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/*.pdb

	if [ -e ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor ]; then
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/*.dll
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/*.mdb
		rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/*.pdb
	fi
fi

OS="Mac"
if [ -e "/c/" ]; then
	OS="Windows"
fi

if [ x"$OS" == x"Windows" ]; then
	common/nuget restore GitHub.Unity.sln
else
	mono common/nuget.exe restore GitHub.Unity.sln
fi

xbuild GitHub.Unity.sln /verbosity:minimal /property:Configuration=$Configuration /target:$Target || true

rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/deleteme*
rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/deleteme*
rm -f unity/PackageProject/Assets/Plugins/GitHub/Editor/*.xml

cp -r unity/PackageProject/Assets/Plugins/GitHub ../github-unity-test/GitHubExtensionProject/Assets/Plugins/ || true
cp -r unity/PackageProject/Assets/Plugins/GitHub.meta ../github-unity-test/GitHubExtensionProject/Assets/Plugins/ || true


if [ -e ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor ]; then
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/deleteme*
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/deleteme*
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Plugins/GitHub/Editor/*.xml
fi