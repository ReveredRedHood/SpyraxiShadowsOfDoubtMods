# 1. After each game update, start the game with BepInEx. This writes the updated dlls to your BepInEx folder. You do not need to run with any mods.
# 2. Update the source_dir variable to match the path to your BepInEx interop folder. Also update the commit_msg variable if needed.
# 3. After the updated dlls are written, run this script, which copies the dlls to the dependencies folder and uses git to commit the changes.
# 4. You will need to manually push the changes from the dependencies folder.

import os
import sys
import shutil
from pathlib import Path
import subprocess

script_dir = os.path.dirname(os.path.abspath(sys.argv[0]))

thunderstore_source_dir = Path(
    f"{os.getenv('APPDATA')}/Thunderstore Mod Manager/DataFolder/ShadowsofDoubt/profiles/Default/BepInEx/interop"
).resolve()

source_dir = thunderstore_source_dir

commit_msg = "chore: update dlls for newest stable game version"

# Copy the dlls from BepInEx interop to the dependencies folder
for root, _, files in os.walk(source_dir):
    for file in files:
        filename = Path(file).name
        if (
            filename.startswith("Assembly-CSharp.dll")
            or filename.startswith("Il2Cppmscorlib")
            or filename.startswith("Rewired_Core")
            or filename.startswith("UnityEngine.CoreModule")
            or filename.startswith("UnityEngine.UI.dll")
        ) and (filename.endswith(".dll") or filename.endswith(".pdb")):
            result = os.path.join(root, file)

            shutil.copyfile(
                result,
                Path(f"{script_dir}/../dependencies/{file}").resolve(),
            )

# Use git to commit the changes for the dependencies submodule
# Stage both modified and untracked files
status = subprocess.run(
    [
        "git",
        "-C",
        "./dependencies/",
        "add",
        "-u",
    ],
    check=True,
    text=True,
)
# Commit changes
status = subprocess.run(
    [
        "git",
        "-C",
        "./dependencies/",
        "commit",
        "-m",
        commit_msg,
    ],
    check=True,
    text=True,
)