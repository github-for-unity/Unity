#!/bin/sh -eu
DIR=$(pwd)
submodules/packaging/octorun/run.sh --path $DIR/octorun --out $DIR/src/GitHub.Api/Resources --source $DIR/src/GitHub.Api/Installer
