[![img.shields.io](https://img.shields.io/nuget/v/SatorImaging.DotnetTool.StaticImport)](https://www.nuget.org/packages/SatorImaging.DotnetTool.StaticImport/)
[![nuget-publish.yml](https://github.com/sator-imaging/DotnetTool-StaticImport/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/sator-imaging/DotnetTool-StaticImport/actions/workflows/nuget-publish.yml)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/sator-imaging/DotnetTool-StaticImport)
&nbsp;
<sup>[🇯🇵 日本語](https://zenn.dev/sator_imaging/articles/7b1df223d17d89)</sup>


`static-import` is a dotnet cli tool to migrate file(s) from another project, github or public website.

In addition, there are features to modify C# script on migration. See [C# Script Options](#c-script-options) for more details.




# Installation

On the command line, enter the following command to install package as a command.

> .NET SDK 8.0+ is required

```sh
dotnet tool install SatorImaging.DotnetTool.StaticImport -g --prerelease
```

&nbsp; * `--prerelease` is required at this moment.





# Basic Usage

Copy files to current folder with `Import_` prefix:

```sh
static-import -o "." -i \
    "local-file.cs" \
    "github:user@repo/BRANCH_TAG_OR_COMMIT/path/to/file.cs" \
    "https://gist.githubusercontent.com/..." \  # use raw url to download from gist
    "https://inter.net/path/to/file.cs"
```

> Type `static-import --help` for more options.

For the use in GitHub Actions, see [.github/workflows/tests.yml](.github/workflows/tests.yml) for details.



## GitHub Options

`static-import` has dedicated scheme for GitHub files.

In contrast to `https:`, it retrieves correct `Last-Modified` value from repository.

```
github:<USER_NAME>@<REPO_NAME>/<REF>/path/to/file.ext
```
- REF: branch name, tag or commit hash

> [!IMPORTANT]
> In GitHub actions, `checkout` won't restore commit date for downloaded files so that `-f/--force-overwrite` is required to migrate files from other repos.


If the environment variable `GH_TOKEN` or `GITHUB_TOKEN` is defined, it is used for accesss to GitHub.

Here shows how to setup the variable in `.yml` file.

```yaml
env:
  GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

> See also: https://docs.github.com/actions/how-tos/security-for-github-actions/security-guides/automatic-token-authentication



## C# Script Options

There are options to modify C# script on copying. (original file doesn't change)

- `--internal`
    - make declared type visibility to `internal`.
    - NOTE: doesn't affect on nested types.
- `--namespace <NAME>`
    - change namespace.
        - if name ending with `.`, it will be prepended (ex. `Foo.` --> `Foo.Original.Namespace`)
    - NOTE: nested namespace *syntaxes* leave untouched.

```cs
namespace FileScoped;  // 👈 changed

namespace Foo.Bar.Baz  // 👈 changed
{
    namespace Quuuuux  // untouched
    {
        public class MyClass  // 👈 changed
        {
            // untouched
            public enum MyEnum { }
            public struct MyStruct { }
            public record MyRecord { }
            public interface IMyInterface { }
        }
    }
}
```



# License

MIT License
