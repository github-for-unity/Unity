# [GitHub for Unity](https://unity.github.com)

![Build Status](https://ci.appveyor.com/api/projects/status/github/github-for-unity/Unity?branch=master&svg=true)

[![Join the chat at https://gitter.im/github/VisualStudio](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/github-for-unity/Unity?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

The GitHub for Unity extension brings [Git](https://git-scm.com/) and GitHub into [Unity](https://unity3d.com/), integrating source control into your work with friendly and accessible tools and workflows.

**Please note:** this software is currently alpha quality. Please refer to the [list of known issues](https://github.com/github-for-unity/Unity/issues?q=is%3Aissue+is%3Aopen+label%3Abug), and make sure you have backups of your work before trying it out.

## Installing GitHub for Unity

### Requirements

- [Unity 5.4 - 5.6](https://store.unity.com/download). Personal edition is fine.

### Installation

To install the extension, download the latest package from [the releases page](https://github.com/github-for-unity/Unity/releases) and double click on it.

#### Opening the GitHub window

You can access the GitHub window by going to Windows -> GitHub. The window opens by default next to the Inspector window.

#### Initialize Repository

![Initialize repository screenshot](https://user-images.githubusercontent.com/121322/27644875-7fc6302a-5bd9-11e7-98d0-c09b2e450503.png)

If the current Unity project is not in a Git repository, the GitHub for Unity extension will offer to initialize the repository for you. This will:

- Initialize a git repository at the Unity project root via `git init`
- Initialize git-lfs via `git lfs install`
- Set up a `.gitignore` file at the Unity project root.
- Set up a `.gitattributes` file at the Unity project root with a large list of known binary filetypes (images, audio, etc) that should be tracked by LFS
- Configure the project to serialize meta files as text
- Create an initial commit with the `.gitignore` and `.gitattributes` file.

#### Authentication

To set up credentials in Git so you can push and pull, you can sign in to GitHub by going to `Window` -> `GitHub` -> `Account` -> `Sign in`. You only have to sign in successfully once, your credentials will remain on the system for all Git operations in Unity and outside of it. If you've already signed in once but the Account dropdown still says `Sign in`, ignore it, it's a bug.

![Authentication screenshot](https://user-images.githubusercontent.com/121322/27644895-8f22f904-5bd9-11e7-8a93-e6bfe0c24a74.png)

#### Publish a new repository

1. Go to [github.com](https://github.com) and create a new empty repository - do not add a license, readme or other files during the creation process.
2. Copy the **https** URL shown in the creation page
3. In Unity, go to `Windows` -> `GitHub` -> `Settings` and paste the url into the `Remote` textbox.
3. Click `Save repository`.
4. Go to the `History` tab and click `Push`.

#### Commiting your work - Changes tab

You can see which files have been changed and commit them through the Changes tab. `.meta` files will show up in relation to their files on the tree, so you can select a file for comitting and automatically have their `.meta` 

![Changes tab screenshot](https://user-images.githubusercontent.com/121322/27644933-ab00af72-5bd9-11e7-84c3-edec495f87f5.png)

#### Pushing/pulling your work - History tab

The history tab includes a `Push` button to push your work to the server. Make sure you have a remote url configured in the `Settings` tab so that you can push and pull your work.

To receive updates from the server by clicking on the `Pull` button. You cannot pull if you have local changes, so commit your changes before pulling.

![History tab screenshot](https://user-images.githubusercontent.com/121322/27644965-c1109bba-5bd9-11e7-9257-4fa38f5c67d1.png)

#### Branches tab

![Branches tab screenshot](https://user-images.githubusercontent.com/121322/27644978-cd3c5622-5bd9-11e7-9dcb-6ae5d5c7dc8a.png)

#### Settings tab

You can configure your user data in the Settings tab, along with the path to the Git installation.

Locked files will appear in a list in the Settings tab. You can see who has locked a file and release file locks after you've pushed your work.

![Settings tab screenshot](https://user-images.githubusercontent.com/121322/27644993-d9d325a0-5bd9-11e7-86f5-beee00e9e8b8.png)

#### Windows

The GitHub for Unity extension ships with a bundle of Git and Git LFS, to ensure that you have the correct version. These will be installed into `%LOCALAPPDATA%\GitHubUnityDebug` when the extension runs for the first time.

You can open a command line with the same Git and Git LFS version that the extension uses by going to the GitHub -> Command line menu.

Make sure a Git user and email address are set in the `%HOME%\.gitconfig` file before you initialize a repository for the first time. You can set these values by opening your `%HOME%\.gitconfig`  file and adding the following section, if it doesn't exist yet:

```
[user]
	name = Your Name
	email = Your Email
```

##### Log files

The extension log file can be found at `%LOCALAPPDATA%\GitHubUnityDebug\github-unity.log`

#### macOS

The current release has limited macOS support. macOS users will need to install the latest [Git](https://git-scm.com/downloads) and [Git LFS](https://git-lfs.github.com/) manually, and make sure these are on the path. You can configure the Git location in the Settings tab on the GitHub window.

Make sure a Git user and email address are set in the `~/.gitconfig` file before you initialize a repository for the first time. You can set these values by opening your `~/.gitconfig` file and adding the following section, if it doesn't exist yet:

```
[user]
	name = Your Name
	email = Your Email
```

##### Log files

The extension log file can be found at `~/.local/share/GitHubUnityDebug/github-unity.log`. This is a temporary location and will be changed in the future.

## I have a problem with GitHub for Unity

First, please search the [open issues](https://github.com/github-for-unity/Unity/issues?q=is%3Aopen)
and [closed issues](https://github.com/github-for-unity/Unity/issues?q=is%3Aclosed)
to see if your issue hasn't already been reported (it may also be fixed).

If you can't find an issue that matches what you're seeing, open a [new issue](https://github.com/github-for-unity/Unity/issues/new)
and fill out the template to provide us with enough information to investigate
further.

## How can I contribute to GitHub for Unity?

The [CONTRIBUTING.md](./CONTRIBUTING.md) document will help you get setup and
familiar with the source. The [documentation](docs/) folder also contains more
resources relevant to the project.

If you're looking for something to work on, check out the [up-for-grabs](https://github.com/github-for-unity/Unity/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs) label.

## More Resources

See [unity.github.com](https://unity.github.com) for more product-oriented
information about GitHub for Unity.

## License

**[MIT](LICENSE)**

The MIT license grant is not for GitHub's trademarks, which include the logo
designs. GitHub reserves all trademark and copyright rights in and to all
GitHub trademarks. GitHub's logos include, for instance, the stylized
Invertocat designs that include "logo" in the file title in the following
folder: [IconsAndLogos](https://github.com/github-for-unity/Unity/tree/master/src/UnityExtension/Assets/Editor/GitHub.Unity/IconsAndLogos).

Copyright 2015 - 2017 GitHub, Inc.
