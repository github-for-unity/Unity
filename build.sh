#!/bin/sh -eu
Configuration="Release"
if [ $# -gt 0 ]; then
	Configuration=$1
fi

Target="Build"
if [ $# -gt 1 ]; then
	Target=$2
fi

OS="Mac"
if [ -e "/c/" ]; then
	OS="Windows"
fi

if [ x"$OS" == x"Windows" ]; then
	./build.cmd $Configuration $Target
else
	dotnet restore
	dotnet build -c $Configuration
fi
