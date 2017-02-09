# GitHub for Unity

## How to install

### Requirements

- [Unity 5.4 or higher](https://store.unity.com/download). Personal edition is fine.

### Installation

- Create a new Unity project.
- Download the latest release from the [releases page](https://github.com/github/UnityInternal/releases) and double-click the unitypackage file. The package will install itself into the project currently opened in Unity, and Unity will automatically load it once all the files are in the project.

For actually seeing things working, you'll need to initialize the directory where you created the Unity project manually with git. Open a command line, head to that directory, and do `git init`.

### Where is the UI

Go to the "Window" top level menu and select "GitHub".

### Known issues

- Authentication is not plugged in to git yet, so push/pull doesn't work
- lfs locking is not plugged in yet
- Branches list sometimes doesn't show all branches
- Settings view is under construction
- Resizing the history view into wide-mode isn't working right
- Scrolling the changes view hides UI elements that should be always visible (the commit box, the top bar)
- Initializing the project as a git repo is not plugged in yet
