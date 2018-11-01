# Contributing to GitHub for Unity

## Build Requirements

This repository is LFS-enabled. To clone it, you should use a git client that supports git LFS 2.x and submodules.

### Windows

- Visual Studio 2015+ or [Mono 4.x](https://download.mono-project.com/archive/4.8.1/windows-installer/) + bash shell (git bash).
  - Mono 5.x will not work
- `UnityEngine.dll` and `UnityEditor.dll`.
  - If you've installed Unity in the default location of `C:\Program Files\Unity` or `C:\Program Files (x86)\Unity`, the build will be able to reference these DLLs automatically. Otherwise, you'll need to copy these DLLs from `[Unity installation path]\Unity\Editor\Data\Managed` into the `lib` directory in order for the build to work

### MacOS

- [Mono 4.x](https://download.mono-project.com/archive/4.8.1/macos-10-universal/) required. You can install it via brew with `brew tap shana/mono && brew install mono@4.8`
  - Mono 5.x will not work
- `UnityEngine.dll` and `UnityEditor.dll`.
  - If you've installed Unity in the default location of `/Applications/Unity`, the build will be able to reference these DLLs automatically. Otherwise, you'll need to copy these DLLs from `[Unity installation path]/Unity.app/Contents/Managed` into the `lib` directory in order for the build to work

## How to Build

Clone the repository and its submodules in a git GUI client that supports Git LFS, or via the command line with the following command:

```
git lfs clone https://github.com/github-for-unity/Unity
```

*Note*: git might complain that it can't checkout the `script` submodule. That submodule is not required for normal builds and you can ignore the error,
or run the following to stop it complaining:

```
git submodule deinit script
```

### Important pre-build steps

The build needs to reference `UnityEngine.dll` and `UnityEditor.dll`. These DLLs are included with Unity. If you've installed Unity in the default location, the build will be able to find them automatically. If not, copy these DLLs from `[your Unity installation path]\Unity\Editor\Data\Managed` into the `lib` directory in order for the build to work.

#### Developer OAuth app

Because GitHub for Unity uses OAuth web application flow to interact with the GitHub API and perform actions on behalf of a user, it needs to be bundled with a Client ID and Secret.

For external contributors, we have bundled a developer OAuth application in the source so that you can complete the sign in flow locally without needing to configure your own application.

These are listed in `src/GitHub.Api/Application/ApplicationInfo.cs`

DO NOT TRUST THIS CLIENT ID AND SECRET! THIS IS ONLY FOR TESTING PURPOSES!!

The limitation with this developer application is that this will not work with GitHub Enterprise. You will see sign-in will fail on the OAuth callback due to the credentials not being present there.

To provide your own Client ID and Client Secret:

- [Register a new developer application](https://github.com/settings/developers) in your profile.
- Copy [common/ApplicationInfo_Local.cs-example](../../common/ApplicationInfo_Local.cs-example) to `common/ApplicationInfo_Local.cs` and fill out the clientId/clientSecret fields for your application.


### Visual Studio

To build with Visual Studio 2015+, open the solution file `GitHub.Unity.sln`. Select `Build Solution` in the `Build` menu.

### Mono and Bash (windows and mac)

To build with Mono 4.x and Bash, first ensure Mono is added to PATH. Mono installs to `C:\Program Files\Mono\bin\` by default. Then execute `build.sh` in a bash shell.

## Build Output

Once you've built the solution for the first time, you can open `src/UnityExtension` in Unity. This folder contains the `GitHub.Unity` project and all the Unity UI and other Unity-specific code that you can have Unity compile as normal for quick testing and prototyping.

The build also creates a Unity test project called `GitHubExtension` inside a directory called `github-unity-test` next to your local clone. For instance, if the repository is located at `c:\Projects\Unity` the test project will be at `c:\Projects\github-unity-test\GitHubExtension`. You can use this project to test binary builds of the extension in a clean environment (all needed DLLs will be copied to it every time you build).

Note: some files might be locked by Unity if have one of the build output projects open when you compile from VS or the command line. This is expected and shouldn't cause issues with your builds.

## Solution organization

The `GitHub.Unity.sln` solution includes several projects:

- packaging: empty projects with build rules that copy DLLs to various locations for testing
- Tests: unit and integration test projects
- GitHub.Logging: A logging helper library
- GitHub.Api: The core of the extension. This project is C#6 and includes async/await threading and other features that Unity cannot currently compile.
- GitHub.Unity: Unity-specific code. This project is compilable by Unity
