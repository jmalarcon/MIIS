---
title: NuGet CLI list command
description: Reference for the nuget.exe list command
author: karann-msft
categories: [karann]
date: 2018-01-18
tags: [reference]
---

# list command (NuGet CLI)

**Applies to:** package consumption, publishing &bullet; **Supported versions:** all

Displays a list of packages from a given source. If no sources are specified, all sources defined in the global configuration file, `%AppData%\NuGet\NuGet.Config` (Windows) or `~/.nuget/NuGet/NuGet.Config`, are used. If `NuGet.Config` specifies no sources, then `list` uses the default feed (nuget.org).

## Usage

```cli
nuget list [search terms] [options]
```

where the optional search terms will filter the displayed list. Search terms are applied to the names of packages, tags, and package descriptions just as they are when using them on nuget.org.

## Options

| Option | Description |
| --- | --- |
| AllVersions | List all versions of a package. By default, only the latest package version is displayed. |
| ConfigFile | The NuGet configuration file to apply. If not specified, `%AppData%\NuGet\NuGet.Config` (Windows) or `~/.nuget/NuGet/NuGet.Config` (Mac/Linux) is used.|
| ForceEnglishOutput | *(3.5+)* Forces nuget.exe to run using an invariant, English-based culture. |
| Help | Displays help information for the command. |
| IncludeDelisted | *(3.2+)* Display unlisted packages. |
| NonInteractive | Suppresses prompts for user input or confirmations. |
| PreRelease | Includes prerelease packages in the list. |
| Source | Specifies a list of packages sources to search. |
| Verbosity | Specifies the amount of detail displayed in the output: *normal*, *quiet*, *detailed*. |

Also see [Environment variables](cli-ref-environment-variables.md)

## Examples

```cli
nuget list

nuget list chinese korean -Verbosity detailed

nuget list couchbase -AllVersions
```
