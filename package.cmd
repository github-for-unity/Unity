SETLOCAL
@ECHO off

set Configuration=Release

if %1.==. (
	echo Need path to Unity
	EXIT /b 1
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

set Unity=%UnityPath%\Editor\Unity.exe
if not exist "%Unity%" ( 
	echo Cannot find Unity at %Unity%
	exit /b 1
) else (
	cd unity\PackageProject\Assets
	git clean -xdf
	cd ..\..\..

	cd src
	git clean -xdf
	cd ..
	
	common\nuget.exe restore GitHub.Unity.sln
	xbuild GitHub.Unity.sln /property:Configuration=%Configuration%
	
	del /Q unity/PackageProject/Assets/Editor/GitHub/deleteme*
	del /Q unity/PackageProject/Assets/Editor/GitHub/*.pdb
	del /Q unity/PackageProject/Assets/Editor/GitHub/*.pdb.meta
	del /Q unity/PackageProject/Assets/Editor/GitHub/*.xml
	
	for /f tokens^=^2^ usebackq^ delims^=^" %%G in (`find "const string Version" common\SolutionInfo.cs`) do (
		set Version=%%G
		set GITHUB_UNITY_DISABLE=1
		"%Unity%" -batchmode -projectPath "%~dp0unity\PackageProject" -exportPackage Assets/Editor/GitHub github-for-unity-%Version%-alpha.unitypackage -force-free -quit
	)
)