import glob, shelve
import sys, os, subprocess
from pathlib import Path
import shutil
from zipfile import ZipFile
import semver
import json
from bs4 import BeautifulSoup
from config import script_dir, get_dest_path


def remove_dist_zip_files():
    print("Removing dist zip files")
    files = glob.glob("**/*.zip")
    print(files)
    for file in files:
        if os.path.isdir(file):
            continue
        os.remove(file)


def clear_dir(dir):
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
        os.remove(file)


def get_project_files():
    # get all .csproj files
    print("Project files:")
    paths = []
    for root, _, files in os.walk("."):
        for file in files:
            if file.endswith(".csproj") and (not file.startswith("Plugin") or file.startswith("PluginDataPersistence")):
                result = os.path.join(root, file)
                print(" ", result)
                paths.append(result)
    return paths


def get_dll_paths(folder_name):
    print(f"Plugin files for {folder_name}:")
    paths = []
    for root, _, files in os.walk(
        Path(f"{script_dir}/../{folder_name}/bin/Debug/net6.0").resolve()
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
                # vdb[path] = version  # update db entry
            # Difference from CI: we do not update shelve, and we act as if the
            # version is ne no matter what.
            new_paths.append(
                (path, version, suffix)
            )  # append the path/version/suffix as a tuple
    return new_paths


if __name__ == "__main__":
    # 1: Check for changes to stored mod versions vs. versions in VersionPrefix of .csproj files
    new_versions = find_new_versions()

    # 2: Run dotnet build on everything
    subprocess.run(["dotnet", "build"], check=True, text=True)

    # 3: Delete all local *.zip files, regardless of versioning
    remove_dist_zip_files()

    for path, version, suffix in new_versions:
        # a: Clear plugins folders in dist directories for mods with new versions
        folder_name = Path(path).parts[0]
        print(f"Clearing {folder_name} plugins folder")
        dest_path = f"{script_dir}/../dist/{folder_name}/plugins"
        clear_dir(dest_path)

        # b: Copy mod, CLSS*, UniverseLib*, and M31* dlls to dist plugin directories
        src_paths = get_dll_paths(folder_name)
        for src_path, src_filename in src_paths:
            shutil.copyfile(src_path, Path(f"{dest_path}/{src_filename}").resolve())

        # c: Modify manifest.json file to ensure version number matches new version number
        manifest_path = Path(
            f"{script_dir}/../dist/{folder_name}/manifest.json"
        ).resolve()
        with open(manifest_path, "r") as file:
            data = json.load(file)
            # remove the version_number field and add it back with the new version
            data.pop("version_number")
            data["version_number"] = version
            new_data = json.dumps(data, indent=4)
            print(new_data)
        with open(manifest_path, "w") as file:
            file.write(new_data)

        # d: Package dist files into new .zip files, for the new versions only
        zip_path = Path(
            f"{script_dir}/../dist/{folder_name}-v{version}-{suffix}.zip"
        ).resolve()

        with ZipFile(zip_path, "w") as zip:
            top_level_files = [
                "icon.png",
                "manifest.json",
                "README.md",
                "TestSave.sodb",
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

        dest_path = Path(get_dest_path(folder_name)).resolve()
        if not dest_path.exists():
            os.mkdir(dest_path)
            print(f"mkdir on {dest_path}")
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
