# {{ plugin_name_nice }} - {{ readme_headline }}

A BepInEx Bleeding Edge (v667) plugin for Shadows of Doubt

## What is it?

{{ readme_desc }}

## Installation

If you are not using r2modman or Thunderstore for installation, follow these steps:

1. Download BepInEx (build artifact 667 or higher) from the official repository.
2. Extract the downloaded files into the same folder as the "Shadows of Doubt.exe" executable.
3. Launch the game, load the main menu, and then exit the game.
4. Download the latest version of the plugin from the Releases page. Unzip the files and place them in corresponding directories within "Shadows of Doubt\BepInEx...".
5. Start the game.

## Usage

{{ usage }}

### Features

{% for feature in features %}
- **{{ feature.heading }}:** {{ feature.desc }}
{% endfor %}

## License

All code in this repo is distributed under the [MIT License](https://bitbucket.org/shadows-of-doubt-mods/mods/src/main/LICENSE). Feel free to use, modify, and distribute as needed.