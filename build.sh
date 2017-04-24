#!/bin/sh -eux
Configuration="Debug"
if [ $# -gt 0 ]; then
	Configuration=$1
fi

nuget restore
xbuild GitHub.Unity.sln /property:Configuration=$Configuration

rm -f unity/PackageProject/Assets/Editor/GitHub/deleteme*
rm -f unity/PackageProject/Assets/Editor/GitHub/deleteme*
rm -f unity/PackageProject/Assets/Editor/GitHub/*.xml

if [ -e ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub ]; then
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/deleteme*
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/deleteme*
	rm -f ../github-unity-test/GitHubExtensionProject/Assets/Editor/GitHub/*.xml
fi