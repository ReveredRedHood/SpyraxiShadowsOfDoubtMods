import os
import sys

script_dir = os.path.dirname(os.path.abspath(sys.argv[0]))


steam_path = "C:\\Program Files (x86)\\Steam\\steam.exe"
app_id = "986130"
interop_dlls = (
    "Assembly-CSharp",
    "Il2Cppmscorlib",
    "UnityEngine.CoreModule",
    "UnityEngine.PhysicsModule",
)
team_name = "TeamSpyraxi"
primary_profile_name = "Modding"


def get_bepinex_r2modman_path(profile_name):
    return f"{os.getenv('APPDATA')}/r2modmanPlus-local/ShadowsofDoubt/profiles/{profile_name}/BepInEx"


def get_dest_path(folder_name, profile_name):
    return (
        f"{get_bepinex_r2modman_path(profile_name)}/plugins/{folder_name}-{folder_name}"
    )


# change var in str depending on mono vs. il2cpp
def get_source_dir(profile_name):
    return f"{get_bepinex_r2modman_path(profile_name)}/interop"
