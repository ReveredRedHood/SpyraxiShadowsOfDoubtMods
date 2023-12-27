import os
from pathlib import Path
import shutil
import subprocess
import sys
from jinja2 import Environment, FileSystemLoader, select_autoescape
import click
from config import script_dir


def render_and_write(fnames, dest_from_root, plugin_name, env, dict):
    for fname in fnames:
        dest_dir = Path(f"{script_dir}/../{dest_from_root}").resolve()
        if not os.path.exists(dest_dir):
            os.mkdir(dest_dir)

        if fname in ["icon.png", "NuGet.Config"]:
            shutil.copyfile(f"{script_dir}/templates/{fname}", f"{dest_dir}/{fname}")
            continue

        if fname == "README.md" and plugin_name.endswith("Tests"):
            result_text = f"""
            # {plugin_name} Tests

            This folder contains test code, and is not intended for release.

            ## License

            All code in this repo is distributed under the [MIT License](https://bitbucket.org/shadows-of-doubt-mods/mods/src/main/LICENSE). Feel free to use, modify, and distribute as needed.
            """
        else:
            template = env.get_template(fname)
            result_text = template.render(dict)

        if fname.endswith(".csproj"):
            fname = fname.replace("Plugin", plugin_name)
        if fname == "PluginTests.cs":
            fname = "Plugin.cs"
        with open(f"{dest_dir}/{fname}", "w") as file:
            file.write(result_text)


@click.command()
@click.option(
    "--name",
    prompt="Plugin name (no spaces)",
    help="The name of the plugin, without spaces.",
)
@click.option(
    "--nice-name",
    prompt="Plugin name (with spaces)",
    help="The name of the plugin, with spaces.",
)
@click.option(
    "--desc",
    prompt="Plugin description",
    help="A short description of what the plugin does.",
)
@click.option(
    "--headline",
    prompt="Plugin readme headline",
    help="A one-sentence description of what the plugin does, no punctuation.",
)
@click.option(
    "--usage",
    prompt="Plugin usage instructions",
    help="A short description of how to use the plugin.",
)
def run(name, nice_name, desc, headline, usage):
    """Scaffolds out a plugin for this repo"""

    features = []
    features.append(
        {
            "heading": "Heading",
            "desc": "Description",
        }
    )
    dict = {
        "namespace": name,
        "tests_namespace": f"{name}Tests",
        "plugin_name": name,
        "plugin_name_nice": nice_name,
        "plugin_desc": desc,
        "manifest_name": name,
        "deps": "",
        "readme_headline": headline,
        "readme_desc": desc,
        "usage": usage,
        "features": features,
    }
    click.echo(dict)

    env = Environment(
        loader=FileSystemLoader(Path(f"{script_dir}/templates")),
        autoescape=select_autoescape(),
    )
    click.echo(env.list_templates())

    # In dist/{ plugin_name }/ and dist/{ plugin_name }Tests/:
    files_dist = ["icon.png", "manifest.json", "README.md"]
    render_and_write(files_dist, f"dist/{name}", name, env, dict)
    render_and_write(files_dist, f"dist/{name}Tests", name, env, dict)

    # add the plugins folders under the dist dir
    plugins_path = Path(f"{script_dir}/../dist/{name}/plugins").resolve()
    if not os.path.exists(plugins_path):
        os.mkdir(plugins_path)
    plugins_path = Path(f"{script_dir}/../dist/{name}Tests/plugins").resolve()
    if not os.path.exists(plugins_path):
        os.mkdir(plugins_path)

    # In { plugin_name }/:
    files_plugin = ["Plugin.cs", "Plugin.csproj", "NuGet.Config"]
    render_and_write(files_plugin, f"{name}", name, env, dict)

    # In { plugin_name }Tests/:
    files_tests = ["PluginTests.cs", "PluginTests.csproj", "NuGet.Config"]
    render_and_write(files_tests, f"{name}Tests", name, env, dict)

    subprocess.run(
        [
            "dotnet",
            "sln",
            Path(f"{script_dir}/../SpyraxiMods.sln").resolve(),
            "add",
            Path(f"{script_dir}/../{name}/{name}.csproj").resolve(),
        ],
        check=True,
        text=True,
    )
    subprocess.run(
        [
            "dotnet",
            "sln",
            Path(f"{script_dir}/../SpyraxiMods.sln").resolve(),
            "add",
            Path(f"{script_dir}/../{name}Tests/{name}Tests.csproj").resolve(),
        ],
        check=True,
        text=True,
    )
    subprocess.run(
        [
            "dotnet",
            "build",
            Path(f"{script_dir}/../SpyraxiMods.sln").resolve(),
        ],
        check=True,
        text=True,
    )


if __name__ == "__main__":
    run()
