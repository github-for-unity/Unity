
Unit and Integration tests for Unity can be found under `src/tests/`.

## Testing requirements
Tests currently run with NUnit 2.6.4.

## Running tests
Tests can be run after building the Unity project. To run the tests execute `test.cmd` on Windows or `test.sh` on Mac.

We use [Appveyor](https://ci.appveyor.com/project/github-windows/unity/build/tests) as the CI for this project to run tests, but it is also necessary to run tests locally when making code changes.
