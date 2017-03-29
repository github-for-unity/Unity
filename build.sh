#!/bin/sh -eu
nuget restore
xbuild GitHub.Unity.sln
