#!/bin/sh -eu
Configuration="Debug"
#case x"$1" in
#	xdebug | xDebug)
#		Configuration="Debug"
#		Publish="PublishDebug"
#		;;
#esac

nuget restore
xbuild GitHub.Unity.sln /property:Configuration=$Configuration
