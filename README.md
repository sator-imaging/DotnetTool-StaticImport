[![img.shields.io](https://img.shields.io/nuget/v/SatorImaging.DotnetTool.StaticImport)](https://www.nuget.org/packages/SatorImaging.DotnetTool.StaticImport/)
[![nuget-publish.yml](https://github.com/sator-imaging/DotnetTool-StaticImport/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/sator-imaging/DotnetTool-StaticImport/actions/workflows/nuget-publish.yml)
&nbsp;
<sup>[🇯🇵 日本語]()</sup>


`static-import` is a dotnet cli tool to migrate file(s) from another project, github or public website.





# Installation

```sh
dotnet tool install SatorImaging.DotnetTool.StaticImport -g --prerelease
```

&nbsp; * `--prerelease` is required at this moment.





# Basic Usage

Copy files to current folder with `Import_` prefix:

```sh
static-import -o "." -op "Import_" --timeout 3000 -i \
    "local-file.cs" \
    "github:user@repo/BRANCH_TAG_OR_COMMIT/path/to/file.cs" \
    "https://inter.net/path/to/file.cs"
```



## GitHub Options

`static-import` has dedicated scheme for GitHub files.

In contrast to `https:`, it retrieves correct `Last-Modified` value from repository.

```
github:<USER_NAME>@<REPO_NAME>/<REF>/path/to/file.ext
```
- REF: branch name, tag or commit hash


If the environment variable `GITHUB_TOKEN` is defined, it is used for accesss to GitHub.

Here shows how to setup the variable in `.yml` file.

```yaml
env:
  GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```



## C# Script Options

There are options to modify C# script on copying. (original file doesn't change)

- `--internal`
    - make declared type visibility to `internal`.
    - NOTE: doesn't affect on nested types.
- `--namespace <NAME>`
    - change namespace.
    - NOTE: nested namespace syntaxes leave untouched.



# License

MIT License
