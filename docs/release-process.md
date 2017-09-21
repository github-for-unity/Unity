# Release process

## The process

TODO

## Template for release notes

The https://github.com/github-for-unity/unity-release-notes node project generates draft release notes by outputting all issues labeled `Feature`, `Enhancement` and `Bug` closed since the last release in the correct format for release notes, and also outputs all PRs closed since the last release, in case some issues might not have been correctly labeled and/or created.

The markdown format for release notes is below. This is what the project above generates (more or less).

```
# Release notes

[any special notes for the release go here, if needed]

## Features

- #XXX - Title of issue (adjust for user comsumption if needed)

## Enhancements

- #XXX - Title of issue (adjust for user comsumption if needed)

## Fixes

- #XXX - Title of issue (adjust for user comsumption if needed)

```
