@echo off
setlocal

set Configuration=Release

if %1.==. (
	echo Need path to Unity
	exit /b 1
)

set UnityPath=%1
set UnityPath=%UnityPath:"=%

set ChangeConfigurationToDebug=0

if "%2"=="debug" (
	set ChangeConfigurationToDebug=1
)

if "%2"=="Debug" (
	set ChangeConfigurationToDebug=1
)

if %ChangeConfigurationToDebug%==1 (
	set Configuration=Debug
)

set FrameworkVersion=v3.5
if not %3.==. (
	set FrameworkVersion=%3
)

set Unity=%UnityPath%\Editor\Unity.exe
if not exist "%Unity%" ( 
	echo Cannot find Unity at %Unity%
	exit /b 1
) else (
	cd unity\PackageProject\Assets
	call git clean -xdf
	cd ..\..\..

	cd src
	call git clean -xdf
	cd ..
	
	call common\nuget.exe restore GitHub.Unity.sln
	echo xbuild GitHub.Unity.sln /property:Configuration=%Configuration% property:TargetFrameworkVersion=%FrameworkVersion%
	call xbuild GitHub.Unity.sln /property:Configuration=%Configuration% property:TargetFrameworkVersion=%FrameworkVersion%
	
	del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\deleteme*
	del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\*.pdb
	del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\*.pdb.meta
	del /Q unity\PackageProject\Assets\Plugins\GitHub\Editor\*.xml
	
	for /f tokens^=^2^ usebackq^ delims^=^" %%G in (`find "const string Version" common\SolutionInfo.cs`) do call :Package %%G
	
	goto End

	rem TODO: Put FrameworkVersion into unitypackage name
	rem TODO: Remove AsyncBridge related metas
	
	:Package
	set Version=%1
	set GITHUB_UNITY_DISABLE=1
	echo "%Unity%" -batchmode -projectPath "%~dp0unity\PackageProject" -exportPackage Assets/Editor/GitHub github-for-unity-%Version%-alpha.unitypackage -force-free -quit
	call "%Unity%" -batchmode -projectPath "%~dp0unity\PackageProject" -exportPackage Assets/Editor/GitHub github-for-unity-%Version%-alpha.unitypackage -force-free -quit
	goto:eof
	
	:End
	echo Completed
)
endlocal