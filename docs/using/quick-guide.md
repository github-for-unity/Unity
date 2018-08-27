# Quick Guide

## More resources

These documents contain more details on how to use the GitHub for Unity plugin:
- **[Installing and Updating the GitHub for Unity package](https://github.com/github-for-unity/Unity/blob/master/docs/using/how-to-install-and-update.md)**
- **[Getting Started with the GitHub for Unity package](https://github.com/github-for-unity/Unity/blob/master/docs/using/getting-started.md)**
- **[Authenticating to GitHub](https://github.com/github-for-unity/Unity/blob/master/docs/using/authenticating-to-github.md)**
- **[Managing Branches](https://github.com/github-for-unity/Unity/blob/master/docs/using/managing-branches.md)**
- **[Locking Files](https://github.com/github-for-unity/Unity/blob/master/docs/using/locking-files.md)**
- **[Working with Changes](https://github.com/github-for-unity/Unity/blob/master/docs/using/working-with-changes.md)**

## Table of Contents

[Installing GitHub for Unity](#installing-github-for-unity)

-   [Requirements](#requirements)
    -   [Git on macOS](#git-on-macos)
    -   [Git on Windows](#git-on-windows)
-   [Installation](#installation)
-   [Log files](#log-files)
    -   [Windows](#windows)
    -   [macOS](#macos)

[Quick Guide to GitHub for Unity](#quick-guide-to-github-for-unity)

-   [Opening the GitHub window](#opening-the-github-window)
-   [Initialize Repository](#initialize-repository)
-   [Authentication](#authentication)
-   [Publish a new repository](#publish-a-new-repository)
-   [Commiting your work - Changes tab](#commiting-your-work---changes-tab)
-   [Pushing/pulling your work - History tab](#pushingpulling-your-work---history-tab)
-   [Branches tab](#branches-tab)
-   [Settings tab](#settings-tab)

## Installing GitHub for Unity

### Requirements

-   Unity 5.4 or higher
    -   There's currently an blocker issue opened for 5.3 support, so we know it doesn't run there. Personal edition is fine.
-   Git and Git LFS 2.x

#### Git on macOS

The current release has limited macOS support. macOS users will need to install the latest [Git](https://git-scm.com/downloads) and [Git LFS](https://git-lfs.github.com/) manually, and make sure these are on the path. You can configure the Git location in the Settings tab on the GitHub window.

The easiest way of installing git and git lfs is to install [Homebrew](https://brew.sh/) and then do `brew install git git-lfs`.

Make sure a Git user and email address are set in the `~/.gitconfig` file before you initialize a repository for the first time. You can set these values by opening your `~/.gitconfig` file and adding the following section, if it doesn't exist yet:

```
[user]
  name = Your Name
  email = Your Email
```

#### Git on Windows

The GitHub for Unity extension ships with a bundle of Git and Git LFS, to ensure that you have the correct version. These will be installed into `%LOCALAPPDATA%\GitHubUnity` when the extension runs for the first time.

Make sure a Git user and email address are set in the `%HOME%\.gitconfig` file before you initialize a repository for the first time. You can set these values by opening your `%HOME%\.gitconfig` file and adding the following section, if it doesn't exist yet:

```
[user]
  name = Your Name
  email = Your Email
```

Once the extension is installed, you can open a command line with the same Git and Git LFS version that the extension uses by going to `Window` -> `GitHub Command Line` in Unity.

### Installation

This extensions needs to be installed (and updated) for each Unity project that you want to version control.
First step is to download the latest package from [the releases page](https://github.com/github-for-unity/Unity/releases);
it will be saved as a file with the extension `.unitypackage`.
To install it, open Unity, then open the project you want to version control, and then double click on the downloaded package.
Alternatively, import the package by clicking Assets, Import Package, Custom Package, then select the downloaded package.

#### Log files

##### macOS

The extension log file can be found at `~/Library/Logs/GitHubUnity/github-unity.log`

##### Windows

The extension log file can be found at `%LOCALAPPDATA%\GitHubUnity\github-unity.log`

## I have a problem with GitHub for Unity

First, please search the [open issues](https://github.com/github-for-unity/Unity/issues?q=is%3Aopen)
and [closed issues](https://github.com/github-for-unity/Unity/issues?q=is%3Aclosed)
to see if your issue hasn't already been reported (it may also be fixed).

If you can't find an issue that matches what you're seeing, open a [new issue](https://github.com/github-for-unity/Unity/issues/new)
and fill out the template to provide us with enough information to investigate
further.

## Quick Guide to GitHub for Unity

### Opening the GitHub window

You can access the GitHub window by going to Windows -> GitHub. The window opens by default next to the Inspector window.

### Initialize Repository

![Initialize repository screenshot](https://user-images.githubusercontent.com/10103121/37807041-bb4446a6-2e19-11e8-9fff-a431309b8515.png)

If the current Unity project is not in a Git repository, the GitHub for Unity extension will offer to initialize the repository for you. This will:

-   Initialize a git repository at the Unity project root via `git init`
-   Initialize git-lfs via `git lfs install`
-   Set up a `.gitignore` file at the Unity project root.
-   Set up a `.gitattributes` file at the Unity project root with a large list of known binary filetypes (images, audio, etc) that should be tracked by LFS
-   Configure the project to serialize meta files as text
-   Create an initial commit with the `.gitignore` and `.gitattributes` file.

### Authentication

To set up credentials in Git so you can push and pull, you can sign in to GitHub by going to `Window` -> `GitHub` -> `Account` -> `Sign in`. You only have to sign in successfully once, your credentials will remain on the system for all Git operations in Unity and outside of it. If you've already signed in once but the Account dropdown still says `Sign in`, ignore it, it's a bug.

![Authentication screenshot](https://user-images.githubusercontent.com/121322/27644895-8f22f904-5bd9-11e7-8a93-e6bfe0c24a74.png)

For more information on Authentication: - **[Authenticating to GitHub](https://github.com/github-for-unity/Unity/blob/master/docs/using/authenticating-to-github.md)**

### Publish a new repository

1.  Go to [github.com](https://github.com) and create a new empty repository - do not add a license, readme or other files during the creation process.
2.  Copy the **https** URL shown in the creation page
3.  In Unity, go to `Windows` -> `GitHub` -> `Settings` and paste the url into the `Remote` textbox.
4.  Click `Save repository`.
5.  Go to the `History` tab and click `Push`.

### Commiting your work - Changes tab

You can see which files have been changed and commit them through the Changes tab. `.meta` files will show up in relation to their files on the tree, so you can select a file for comitting and automatically have their `.meta`

![Changes tab screenshot](https://user-images.githubusercontent.com/121322/27644933-ab00af72-5bd9-11e7-84c3-edec495f87f5.png)

For more information on working with changes: - **[Working with Changes](https://github.com/github-for-unity/Unity/blob/master/docs/using/working-with-changes.md#commit-changes)**

### Pushing/pulling your work - History tab

The history tab includes a `Push` button to push your work to the server. Make sure you have a remote url configured in the `Settings` tab so that you can push and pull your work.

To receive updates from the server by clicking on the `Pull` button. You cannot pull if you have local changes, so commit your changes before pulling.

![History tab screenshot](https://user-images.githubusercontent.com/121322/27644965-c1109bba-5bd9-11e7-9257-4fa38f5c67d1.png)


For more information on working with changes: - **[Working with Changes](https://github.com/github-for-unity/Unity/blob/master/docs/using/working-with-changes.md#pulling-changes)**

### Branches tab

![Branches tab screenshot](https://user-images.githubusercontent.com/121322/27644978-cd3c5622-5bd9-11e7-9dcb-6ae5d5c7dc8a.png)

### Settings tab

You can configure your user data in the Settings tab, along with the path to the Git installation.

Locked files will appear in a list in the Settings tab. You can see who has locked a file and release file locks after you've pushed your work.

![Settings tab screenshot](https://user-images.githubusercontent.com/121322/27644993-d9d325a0-5bd9-11e7-86f5-beee00e9e8b8.png)