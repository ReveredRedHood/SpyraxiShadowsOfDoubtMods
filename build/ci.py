import os, subprocess
from pathlib import Path
import shutil
from zipfile import ZipFile
from config import script_dir
from local import get_dll_paths, find_new_versions

if __name__ == "__main__":
    # 1: Check for changes to stored mod versions vs. versions in VersionPrefix of .csproj files
    new_versions = find_new_versions()

    # 2: Run dotnet build on everything
    subprocess.run(["dotnet", "build"], check=True, text=True)

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
