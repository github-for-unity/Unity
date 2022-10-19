# [GitHub for Unity](https://unity.github.com)

## NOTICE OF DEPRECATION

This project is dead y'all! Remove GitHub for Unity from your project, then go to https://github.com/spoiledcat/git-for-unity and install Git for Unity from the instructions there.

# What is it

The GitHub for Unity extension brings [Git](https://git-scm.com/) and GitHub into [Unity](https://unity3d.com/), integrating source control into your work with friendly and accessible tools and workflows.

You can reach the team right here by opening a [new issue](https://github.com/github-for-unity/Unity/issues/new). You can also tweet at [@GitHubUnity](https://twitter.com/GitHubUnity)

[![Build Status](https://ci.appveyor.com/api/projects/status/github/github-for-unity/Unity?branch=master&svg=true)](https://ci.appveyor.com/project/github-windows/unity)

## Notices

Please refer to the [list of known issues](https://github.com/github-for-unity/Unity/issues?q=is%3Aissue+is%3Aopen+label%3Abug), and make sure you have backups of your work before trying it out.

From version 0.19 onwards, the location of the plugin has moved to `Assets/Plugins/GitHub`. If you have version 0.18 or lower, you need to delete the `Assets/Editor/GitHub` folder before you install newer versions. You should exit Unity and delete the folder from Explorer/Finder, as Unity will not unload native libraries while it's running. Also, remember to update your `.gitignore` file.

## Building and Contributing

Please read the [How to Build](docs/contributing/how-to-build.md) document for information on how to build GitHub for Unity.

The [CONTRIBUTING.md](CONTRIBUTING.md) document will help you get setup and familiar with the source. The [documentation](docs/) folder also contains more resources relevant to the project.

If you're looking for something to work on, check out the [up-for-grabs](https://github.com/github-for-unity/Unity/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs) label.

## How to use

The [quick guide to GitHub for Unity](docs/using/quick-guide.md)

More [in-depth information](docs/readme.md)

## License

**[MIT](LICENSE)**

The MIT license grant is not for GitHub's trademarks, which include the logo
designs. GitHub reserves all trademark and copyright rights in and to all
GitHub trademarks. GitHub's logos include, for instance, the stylized
Invertocat designs that include "logo" in the file title in the following
folder: [IconsAndLogos](src/UnityExtension/Assets/Editor/GitHub.Unity/IconsAndLogos).

Copyright 2015 - 2018 GitHub, Inc.
