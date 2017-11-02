@echo off
setlocal

:: make sure at Unity project root directory
set NunitDirectory=packages\NUnit.Runners.2.6.4\tools
echo %NunitDirectory%
set ConsoleRunner=%NunitDirectory%\nunit-console.exe
echo %ConsoleRunner%

:: run tests
echo Running "build\IntegrationTests\IntegrationTests.dll" "build\IntegrationTests\TestUtils.dll" "build\TaskSystemIntegrationTests\TaskSystemIntegrationTests.dll" "build\UnitTests\TestUtils.dll" "build\UnitTests\UnitTests.dll" "src\tests\TestUtils\bin\Release\TestUtils.dll" %1
call %ConsoleRunner% "build\IntegrationTests\IntegrationTests.dll" "build\IntegrationTests\TestUtils.dll" "build\TaskSystemIntegrationTests\TaskSystemIntegrationTests.dll" "build\UnitTests\TestUtils.dll" "build\UnitTests\UnitTests.dll" "src\tests\TestUtils\bin\Release\TestUtils.dll" %1

endlocal
