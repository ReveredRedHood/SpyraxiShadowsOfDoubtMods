# Spyraxi's Shadows of Doubt Mods

The full READMEs for each mod can be found by clicking on their subfolder in the "dist" directory.

## Working with the code

### Building the packages locally

The build scripts are written using python, allowing for cross-platform development[^1].
Set up python:

* Install python
* Install build script deps from `./build/requirements.txt` using e.g. `pip install -r ./build/requirements.txt`. You can also install the deps inside a virtual environment beforehand with `pip install virtualenv`, `virtualenv venv`, `source venv/bin/activate` (for bash, or use the corresponding command for your shell).

When building and testing locally, run the `cli.py` script with `python ./build/cli.py`. The script automatically updates all versions within the `dist` directory including those in `manifest.json` (where the plugin gets packaged) to match the ones set in the `.csproj` files. When you are releasing new versions of your plugin(s), you'll only need to bump the version number in the `.csproj` file(s) prior to committing.

Note that you will need to manually go into your mod manager and use the "Import Local Mod" button in the settings menu of the mod manager for it to recognize that the file is there. You only need to do this once, after which the mod will appear in the list and be auto-updated by running `cli.py`. Edit `config.py` to change the path if you aren't using r2modman.

## Contributing

I do not plan to accept or review pull requests for this repo (due to time limitations), but feel free to submit issues or fork this repo if you wish to base your own mods off of it. You are especially welcome to fork the repo if I am inactive and not responding to requests.

## License

All code in this repo is distributed under the MIT License. Feel free to use, modify, and distribute as needed.
