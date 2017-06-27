# Contributing to GitHub for Unity

## Build Requirements
- To build GitHub for Unity we recommend using Visual Studio 2015 or Mono 4.x and bash.
- Building from the command line or from Visual Stuido requires referencing Unity DLLs
  - Windows: `C:\Program Files (x86)\Unity\`
  - macOS: `\Applications\Unity\`
- Alternatively `UnityEditor.dll` and `UnityEngine.dll` can be copied to `.\script\lib`

## How to Build

Clone the repository and its submodules in a git GUI client or via the command line:

```
git clone https://github.com/github-for-unity/Unity
cd Unity
git submodule init
git submodule deinit script
git submodule update
```
    
### Visual Studio

To build with Visual Studio 2015 open the solution file `GitHub.Unity.sln`. Select `Build Solution` in the `Build` menu.

### Mono and Bash

To build with Mono 4.x and Bash execute `build.sh` in a bash shell.

## Build Output

Building the project creates an output folder named `github-unity-test` that is a sibling to the cloned repository. For instance, if the solution is located at `c:\Projects\Unity` the test output can be foud at `c:\Projects\github-unity-test`. The output folder contains a blank Unity project folder named `GitHubExtensionProject`. This folder is a blank Unity 5.5 project with GitHub for Unity installed.
