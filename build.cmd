@echo off
setlocal

set Configuration=dev
if not %1.==. (
	set Configuration=%1
)

set Target=Build
if not %2.==. (
	set Target=%2
)

if %Target%==Rebuild (
	del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\*.dll
	del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\*.mdb
	del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\*.pdb

	if exist "..\github-unity-test\GitHubExtensionProject\Assets\Plugins\GitHub\Editor" (
		del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\GitHub\Editor\*.dll
		del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\GitHub\Editor\*.mdb
		del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\GitHub\Editor\*.pdb
	)
)

call common\nuget.exe restore GitHub.Unity.sln

echo xbuild GitHub.Unity.sln /verbosity:normal /property:Configuration=%Configuration% /target:%Target%
call xbuild GitHub.Unity.sln /verbosity:normal /property:Configuration=%Configuration% /target:%Target%

del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\deleteme*
del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\deleteme*
del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\*.xml

echo xcopy /C /H /R /S /Y /Q unity\PackageProject\Assets\Plugins\GitHub ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\
call xcopy /C /H /R /S /Y /Q unity\PackageProject\Assets\Plugins\GitHub ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\

echo xcopy /C /H /R /Y /Q unity\PackageProject\Assets\Plugins\GitHub.meta ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\
call xcopy /C /H /R /Y /Q unity\PackageProject\Assets\Plugins\GitHub.meta ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\

if exist ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\GitHub\Editor (
	del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\GitHub\Editor\deleteme*
	del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\GitHub\Editor\deleteme*
	del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Plugins\GitHub\Editor\*.xml
)