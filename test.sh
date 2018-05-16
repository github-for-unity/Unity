#!/bin/sh -eu
Configuration="Debug"
if [ $# -gt 0 ]; then
	Configuration=$1
fi

Exclude=""
if [ $# -gt 1 ]; then
	Exclude="/exclude=$2"
fi

NunitDirectory="packages\NUnit.Runners.2.6.4\tools"
ConsoleRunner="$NunitDirectory\nunit-console.exe"

$ConsoleRunner "build\UnityTests\UnityTests.dll" "build\IntegrationTests\IntegrationTests.dll" "build\TaskSystemIntegrationTests\TaskSystemIntegrationTests.dll" "build\UnitTests\UnitTests.dll" $Exclude