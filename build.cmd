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
	del /Q unity\PackageProject\Assets\Editor\GitHub\*.dll
	del /Q unity\PackageProject\Assets\Editor\GitHub\*.mdb
	del /Q unity\PackageProject\Assets\Editor\GitHub\*.pdb

	if exist "..\github-unity-test\GitHubExtensionProject\Assets\Editor\GitHub" (
		del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Editor\GitHub\*.dll
		del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Editor\GitHub\*.mdb
		del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Editor\GitHub\*.pdb
	)
)

call common\nuget.exe restore GitHub.Unity.sln
echo xbuild GitHub.Unity.sln /verbosity:normal /property:Configuration=%Configuration% /target:%Target%
call xbuild GitHub.Unity.sln /verbosity:normal /property:Configuration=%Configuration% /target:%Target%

echo xcopy /C /H /R /S /Y /Q unity\PackageProject\Assets\Editor\GitHub ..\github-unity-test\GitHubExtensionProject\Assets\Editor
call xcopy /C /H /R /S /Y /Q unity\PackageProject\Assets\Editor\GitHub ..\github-unity-test\GitHubExtensionProject\Assets\Editor

del /Q unity\PackageProject\Assets\Editor\GitHub\deleteme*
del /Q unity\PackageProject\Assets\Editor\GitHub\deleteme*
del /Q unity\PackageProject\Assets\Editor\GitHub\*.xml

if exist ..\github-unity-test\GitHubExtensionProject\Assets\Editor\GitHub (
	del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Editor\GitHub\deleteme*
	del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Editor\GitHub\deleteme*
	del /Q ..\github-unity-test\GitHubExtensionProject\Assets\Editor\GitHub\*.xml
)