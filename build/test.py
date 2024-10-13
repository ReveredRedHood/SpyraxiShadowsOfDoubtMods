import datetime
from pathlib import Path
import subprocess
from config import steam_path, app_id, get_dest_path, get_bepinex_r2modman_path
from build_mods import build_mods, clear_dir


def test(plugin_names, additional_dlls_to_copy, profile_name):
    # Run build on plugins
    build_mods(plugin_names, additional_dlls_to_copy, profile_name)
    # Clear config files
    config_path = Path(get_bepinex_r2modman_path(profile_name)).joinpath("config")
    clear_dir(config_path, "BepInEx")

    # Start the game under the profile
    path_to_dll = f"\"{Path(f"{get_dest_path("_", profile_name)}/../../core/BepInEx.Unity.IL2CPP.dll").resolve()}\""
    subprocess.run(
        f"{steam_path} -applaunch {app_id} --doorstop-enabled true --doorstop-target-assembly {path_to_dll}", check=True, text=True
    )

    print("Completed successfully.")
    now = datetime.datetime.now()
    print(f"Time: {now.time()}")

def test_sod_common():
    test("TestHelper", "SOD.Common", "Modding")
