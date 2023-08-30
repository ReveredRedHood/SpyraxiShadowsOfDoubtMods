# Spyraxi's Shadows of Doubt Mods (SoD v34.10+)

**_Note:_ These mods only work for BepInEx Bleeding Edge (v667) (same as Thunderstore).**

The full READMEs for each mod can be found at the following locations:

* [DeTESTive](https://bitbucket.org/shadows-of-doubt-mods/mods/src/main/dist/DeTESTive/README.md)
* [DeTESTiveExample](https://bitbucket.org/shadows-of-doubt-mods/mods/src/main/dist/DeTESTiveExample/README.md)

# Working with the Code

## Building the packages locally

The build scripts are written using python, allowing for cross-platform development[^1].
Set up python:

* Install python
* Install build script deps from `./build/requirements.txt` using e.g. `pip install -r ./build/requirements.txt`. You can also install the deps inside a virtual environment beforehand with `pip install virtualenv`, `virtualenv venv`, `source venv/bin/activate` (for bash, or use the corresponding command for your shell).

When building and testing locally, run the `local.py` script with `python ./build/local.py`. The script automatically updates all versions within the `dist` directory including those in `manifest.json` (where the plugin gets packaged) to match the ones set in the `.csproj` files. When you are releasing new versions of your plugin(s), you'll only need to bump the version number in the `.csproj` file(s) prior to committing. Run `ci.py` **after** you commit to update the stored version numbers.

One difference between the `local.py` and `ci.py` scripts is that `local.py` will copy plugin zip files to the Default profile location for Thunderstore Mod Manager. Note that you will need to manually go into Thunderstore Mod Manager and use the "Import Local Mod" button in the settings menu of the mod manager for it to recognize that the file is there. You only need to do this once, after which the mod will appear in the list and be auto-updated by running `local.py`. Edit `local.py` to change the path if you aren't using Thunderstore. I might add a command line arg to `local.py` to allow you to override the path.

[^1]: I can't guarantee `local.py` and `plugin-scaffolding.py` will work on Linux because I haven't tested it, but `ci.py` runs on a .NET SDK 7.0 Linux-based Docker image as part of BitBucket Pipelines.

## Continuous Integration

There is a CI script named `ci.py` which is very similar to `local.py` but it checks to see which plugins have new versions and only packages those. The latest version numbers are stored in the `./build/shelve` directory's files. In the future I might update the CI script so that it interfaces with the Thunderstore Mod Manager API to upload new versions of your mods for you.

You need to add $BITBUCKET_USERNAME (variable) and $BITBUCKET_APP_PASSWORD (secret) to your Pipeline to make the "upload files" step of the bitbucket-pipelines.yml script work. [See the documentation for more info](https://bitbucket.org/atlassian/bitbucket-upload-file/src/master/). You can set up a GitHub Actions script to do something similar if you are using GitHub... I haven't done that because I only host my mods on BitBucket.

## Scaffolding Plugins

You can scaffold a new plugin that will be compatible with the build scripts using `python ./build/plugin-scaffolding.py`. The script will prompt you for inputs. You can also pass command line args instead (see `python ./build/plugin-scaffolding.py --help`).

```
.
├── build => build scripts and templates
├── dependencies => SoD header files submodule
├── dist/
│   ├── { Plugin Name }/
│   │   ├── plugins/
│   │   ├── icon.png
│   │   ├── manifest.json
│   │   └── README.md
│   ├── { Plugin Name }Tests/
│   │   ├── plugins/
│   │   ├── icon.png
│   │   ├── manifest.json
│   │   └── README.md
│   └── ... => local.py puts .zip files here
├── { Plugin Name }/
│   ├── NuGet.Config
│   ├── Plugin.cs
│   └── { Plugin Name }.csproj
├── { Plugin Name }Tests/
│   ├── NuGet.Config
│   ├── Plugin.cs
│   └── { Plugin Name }Tests.csproj
└── SpyraxiMods.sln
```
