import glob, shelve
import sys, os, subprocess
from pathlib import Path
import shutil
from zipfile import ZipFile
import semver
import json
from bs4 import BeautifulSoup
from config import script_dir


def get_project_files():
    # get all .csproj files
    print("Project files:")
    paths = []
    for root, _, files in os.walk("."):
        for file in files:
            if file.endswith(".csproj") and (not file.startswith("Plugin")):
                result = os.path.join(root, file)
                print(" ", result)
                paths.append(result)
    return paths


def get_dll_paths(folder_name):
    print(f"Plugin files for {folder_name}:")
    paths = []
    for root, _, files in os.walk(
        Path(f"{script_dir}/../{folder_name}/bin/Debug/net6.0").absolute()
    ):
        for file in files:
            filename = Path(file).name
            if not (
                filename.startswith("Iced")
                or filename.startswith("Assembly")
                or filename.startswith("Il2Cppmscorlib")
                or filename.startswith("Rewired")
                or filename.startswith("Unhollower")
                or filename.startswith("Unity")
                or filename.startswith("System")
            ) and (filename.endswith(".dll") or filename.endswith(".pdb")):
                result = os.path.join(root, file)
                print(" ", result)
                paths.append((result, filename))
    return paths


def find_new_versions():
    paths = get_project_files()
    new_paths = []
    with shelve.open(f"{script_dir}/shelve/versions") as vdb:  # open db
        for path in paths:
            with open(path, "r") as file:
                print(f"Reading version from {path}")
                content = file.read()
                soup = BeautifulSoup(content, "lxml")
                version = soup.find("versionprefix").string
                print(" ", version)
                suffix = soup.find("versionsuffix").string
                print(" suffix: ", suffix)
            last_version = vdb.get(path)
            if last_version is None or semver.compare(version, last_version) > 0:
                print(f" Detected new version: {last_version} => {version}")
                vdb[path] = version  # update db entry
                new_paths.append(
                    (path, version, suffix)
                )  # append the path/version/suffix as a tuple
    return new_paths


if __name__ == "__main__":
    # 1: Check for changes to stored mod versions vs. versions in VersionPrefix of .csproj files
    new_versions = find_new_versions()

    # 2: Run dotnet build on everything
    subprocess.run(
        ["dotnet", "build", Path(f"{script_dir}/../SpyraxiMods.sln").resolve()],
        check=True,
        text=True,
    )

    # 3: Manage files and package
    for path, version, suffix in new_versions:
        # a: Clear plugins folders in dist directories for mods with new versions
        folder_name = Path(path).parts[0]
        if folder_name.endswith("Tests"):
            continue  # don't release test code
        dest_path = f"dist/{folder_name}/plugins"
        # CI: need to create the plugins folder
        os.mkdir(dest_path)

        # b: Copy mod, CLSS*, UniverseLib*, and M31* dlls to dist plugin directories
        src_paths = get_dll_paths(folder_name)
        for src_path, src_filename in src_paths:
            shutil.copyfile(src_path, Path(f"{dest_path}/{src_filename}").absolute())

        # c: Package dist files into new .zip files, for the new versions only
        zip_path = Path(
            f"{script_dir}/../dist/{folder_name}-v{version}-{suffix}.zip"
        ).absolute()

        with ZipFile(zip_path, "w") as zip:
            top_level_files = [
                "icon.png",
                "manifest.json",
                "README.md",
            ]
            for file in top_level_files:
                if not Path(f"{script_dir}/../dist/{folder_name}/{file}").exists():
                    continue
                zip.write(Path(f"{script_dir}/../dist/{folder_name}/{file}"), file)
            for _, src_filename in src_paths:
                zip.write(
                    Path(f"{script_dir}/../dist/{folder_name}/plugins/{src_filename}"),
                    f"plugins/{src_filename}",
                )
