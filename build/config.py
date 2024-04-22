import os
import sys

script_dir = os.path.dirname(os.path.abspath(sys.argv[0]))

steam_path = "C:\Program Files (x86)\Steam\steam.exe"
app_id = "986130"

team_name = "TeamSpyraxi"  # can change
profile_name = "Modding"  # can change
copy_these_over = ["TestHelper", "Guns"]  # can change, should just be the mods you are actively testing
copy_sod_common = True # can change

bepinex_r2modman = f"{os.getenv('APPDATA')}/r2modmanPlus-local/ShadowsofDoubt/profiles/{profile_name}/BepInEx/plugins"
bepinex_thunderstore = f"{os.getenv('APPDATA')}/Thunderstore Mod Manager/DataFolder/ShadowsofDoubt/profiles/{profile_name}/BepInEx/plugins"


def dest_r2modman(folder_name):
    return f"{bepinex_r2modman}/{folder_name}-{folder_name}"
    # return f"{bepinex_r2modman}/plugins/{folder_name}-{folder_name}"
    # return f"{bepinex_r2modman}/plugins/{team_name}-{folder_name}"


def dest_thunderstore(folder_name):
    return f"{bepinex_thunderstore}/{folder_name}-{folder_name}"
    # return f"{bepinex_thunderstore}/plugins/{folder_name}-{folder_name}"


def get_dest_path(
    folder_name,
):  # change this depending on the mod manager you are using
    return dest_r2modman(folder_name)


# change var in str depending on the mod manager you are using... also mono vs. il2cpp
source_dir = f"{bepinex_r2modman}/interop"
