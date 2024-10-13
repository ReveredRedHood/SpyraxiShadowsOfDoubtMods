from bs4 import BeautifulSoup
from config import (
    script_dir,
    get_source_dir,
    interop_dlls,
    get_dest_path,
    primary_profile_name,
)
from pathlib import Path
from zipfile import ZipFile
import datetime
import glob
import json
import os
import shutil
import subprocess


def update_interop_dlls():
    source_path = Path(get_source_dir(primary_profile_name)).resolve()
    print(f"Updating interop dlls from: {source_path}")

    # Copy the dlls from BepInEx interop to the dependencies folder
    for root, _, files in os.walk(source_path):
        for file in files:
            filename = Path(file).name
            if (
                filename.startswith(interop_dlls)
                and (filename.endswith(".dll") or filename.endswith(".pdb"))
                and (not filename.endswith("-firstpass.dll"))
            ):
                result = os.path.join(root, file)
                dependencies_file_path = Path(
                    f"{script_dir}/../dependencies/{file}"
                ).resolve()
                print(f"Copy: {dependencies_file_path}")
                shutil.copyfile(
                    result,
                    dependencies_file_path,
                )


def remove_dist_zip_files(plugins_or_all):
    print("Removing dist zip files")
    files = glob.glob("**/*.zip")
    print(files)
    for file in files:
        if os.path.isdir(file):
            continue
        if plugins_or_all != "all" or not file.split("\\")[1].startswith(
            plugins_or_all
        ):
            continue
        os.remove(file)


def clear_dir(dir, skip_prefixes=None):
    print(f"Clearing dir: {dir}")
    # Note that * prefix is the spread operator
    # We are deleting all files in the directory, hidden or not
    path = Path(dir)
    resolved_path = path.resolve()
    print(f"Resolved path: {resolved_path}")
    files = [
        *glob.glob(f"{resolved_path.resolve()}/.*"),
        *glob.glob(f"{resolved_path.resolve()}/*"),
    ]
    print(files)
    for file in files:
        if os.path.isdir(file):
            continue
        if skip_prefixes != None and Path(file).name.startswith(skip_prefixes):
            continue
        os.remove(file)


def get_project_files(plugins_or_all):
    print("Project files:")
    paths = []
    for root, _, files in os.walk("."):
        for file in files:
            if (
                plugins_or_all == "all" or file.startswith(plugins_or_all)
            ) and file.endswith(".csproj"):
                result = os.path.join(root, file)
                print(" ", result)
                paths.append(result)
    return paths


def get_dll_paths(folder_name, additional_dlls_to_copy):
    print(f"Plugin files for {folder_name}:")
    paths = []
    needs_universe_lib = not get_is_reliant_on_util(folder_name)
    for root, _, files in os.walk(
        Path(f"{script_dir}/../mods/{folder_name}/bin/Debug/net6.0").resolve()
    ):
        for file in files:
            filename = Path(file).name
            if (
                filename.startswith(folder_name)
                or (needs_universe_lib and filename.startswith("UniverseLib"))
                or (
                    additional_dlls_to_copy != None
                    and filename.startswith(additional_dlls_to_copy)
                )
                and (filename.endswith(".dll") or filename.endswith(".pdb"))
            ):
                result = os.path.join(root, file)
                print(" ", result)
                paths.append((result, filename))
    return paths


# If the plugin relies on SOD.Common, we don't need to package UniverseLib
def get_is_reliant_on_util(folder_name):
    for _, _, files in os.walk(
        Path(f"{script_dir}/../mods/{folder_name}/bin/Debug/net6.0").resolve()
    ):
        for file in files:
            filename = Path(file).name
            if filename.startswith("SOD.Common") and filename.endswith(".dll"):
                return True
    return False


def create_zip(folder_name, src_paths, zip_path):
    with ZipFile(zip_path, "w") as zip:
        top_level_files = []
        for _, _, files in os.walk(
            Path(f"{script_dir}/../dist/{folder_name}").resolve()
        ):
            for file in files:
                top_level_files.append(file)
        for file in top_level_files:
            if not Path(f"{script_dir}/../dist/{folder_name}/{file}").exists():
                continue
            zip.write(Path(f"{script_dir}/../dist/{folder_name}/{file}"), file)
        for _, src_filename in src_paths:
            zip.write(
                Path(f"{script_dir}/../dist/{folder_name}/plugins/{src_filename}"),
                f"plugins/{src_filename}",
            )
    return top_level_files


def get_plugin_paths(plugins_or_all):
    base_paths = get_project_files(plugins_or_all)
    path_tuples = []
    for path in base_paths:
        with open(path, "r") as file:
            print(f"Reading version from {path}")
            content = file.read()
            soup = BeautifulSoup(content, "lxml")
            version = soup.find("versionprefix").string
            print(" ", version)
            suffix = soup.find("versionsuffix")
            suffix = "" if suffix is None else suffix.string
            print(" suffix: ", suffix)
        path_tuples.append((path, version, suffix))
    return path_tuples


def build_mods(plugins_or_all, additional_dlls_to_copy, profile_name_for_test):
    # Update interop dlls
    update_interop_dlls()

    # Run dotnet build on everything
    subprocess.run(["dotnet", "build"], check=True, text=True)

    # Delete all local *.zip files, regardless of versioning
    remove_dist_zip_files(plugins_or_all)

    for path, version, suffix in get_plugin_paths(plugins_or_all):
        # Clear plugins folders in dist directories
        folder_name = Path(path).parts[1]
        print(f"Clearing {folder_name} plugins folder")
        dest_path = f"{script_dir}/../dist/{folder_name}/plugins"
        clear_dir(dest_path)

        # Copy dlls
        src_paths = get_dll_paths(folder_name, additional_dlls_to_copy)
        for src_path, src_filename in src_paths:
            shutil.copyfile(src_path, Path(f"{dest_path}/{src_filename}").resolve())

        manifest_path = Path(
            f"{script_dir}/../dist/{folder_name}/manifest.json"
        ).resolve()
        with open(manifest_path, "r") as file:
            # Read version number
            data = json.load(file)
            # remove the version_number field and add it back with the new version
            data.pop("version_number")
            data["version_number"] = version
            new_data = json.dumps(data, indent=4)
            print(new_data)
        with open(manifest_path, "w") as file:
            # Modify manifest.json file to ensure version number matches new version number
            file.write(new_data)

        # Package dist files into new .zip files
        zip_path = Path(
            f"{script_dir}/../dist/{folder_name}-v{version}{"-" if suffix != "" else ""}{suffix}.zip"
        ).resolve()
        top_level_files = create_zip(folder_name, src_paths, zip_path)

        if profile_name_for_test == None:
            continue

        dest_path = Path(get_dest_path(folder_name, profile_name_for_test)).resolve()
        if not dest_path.exists():
            raise NotADirectoryError(
                f"You must import {folder_name} locally in r2modman"
            )
        clear_dir(dest_path)
        for file in top_level_files:
            if not Path(f"{script_dir}/../dist/{folder_name}/{file}").exists():
                continue
            shutil.copyfile(
                Path(f"{script_dir}/../dist/{folder_name}/{file}").resolve(),
                f"{dest_path}/{file}",
            )
        for _, src_filename in src_paths:
            shutil.copyfile(
                Path(f"{script_dir}/../dist/{folder_name}/plugins/{src_filename}"),
                f"{dest_path}/{src_filename}",
            )

    print("Completed successfully.")
    now = datetime.datetime.now()
    print(f"Time: {now.time()}")
