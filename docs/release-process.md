# Release process

## Release information

### Tag name format

***Alpha releases***: `v[major].[minor]-alpha`

***Beta releases***: `v[major].[minor]-beta`

***Releases***: `v[major].[minor]`

## The process

#### TL;DR

1. Dev branches to a release branch every week, create draft release with release notes
2. QA tests release branch for up to a week
3. Dev continues work on master
4. Dev fixes shipblockers on release branch and updates build on draft release page, and makes sure fixes are also sent to master
5. QA publishes release by tagging the release branch and hitting "Publish"
6. Goto 1

#### The long explanation

Every week:

1. Create a release branch from master with the name `release/[Major].[Minor]`, where `[Major].[Minor]` are the current major and minor parts of the version set on master.
1. Bump the version on master
1. Create a draft release with the release notes, following the format below
1. Upload a build created from the release branch
1. QA tests the release and logs any issues found in it. Issues are fixed on the next release unless they are related to the issues that are reported as fixed in the current release, and they are shipblockers.
1. If the release requires fixes:
   1. If the fix is reverting a PR, that doesn't have to be done on master, only on the release branch. Otherwise:
   1. Create a branch from the release branch fork point (the commit that the release branch is based on)
   1. Push the fix to this branch and create a PR targetting the release branch
   1. Upload a new build from the release branch
   1. Create another branch from master, merge the fix branch into it and create a PR targetting master
1. QA approves release by:
   1. Filling out the tag name, in the format shown above
   1. Selecting the release branch corresponding to the build
   1. Clicking `Publish release`


## Template for release notes

The https://github.com/github-for-unity/unity-release-notes node project generates draft release notes by outputting all issues labeled `Feature`, `Enhancement` and `Bug` closed since the last release in the correct format for release notes, and also outputs all PRs closed since the last release, in case some issues might not have been correctly labeled and/or created.

The markdown format for release notes is below. This is what the project above generates (more or less).

```
# Release notes

[any special notes and/or funny/relevant image for the release here, if needed]

## Features

- #XXX - Title of issue (adjust for user consumption if needed)

## Enhancements

- #XXX - Title of issue (adjust for user consumption if needed)

## Fixes

- #XXX - Title of issue (adjust for user consumption if needed)

```
