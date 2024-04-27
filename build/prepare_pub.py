import datetime
import requests
import json
from config import team_name, script_dir
from pathlib import Path
from build_mods import build_mods


def get_thunderstore_metadata(namespace, folder_name):
    url = f"https://thunderstore.io/api/experimental/package/{namespace}/{folder_name}/"
    r = requests.get(url)
    assert r.status_code == 200
    return {
        "version": r.json()["latest"]["version_number"],
        "dependency_string": r.json()["latest"]["full_name"],
    }


def get_request_args_from_dependency_string(dep_str: str):
    split_result = dep_str.split("-")
    return (split_result[0], split_result[1])


def prepare_pub(plugin_name, exists_on_ts):
    # Run build
    build_mods(plugin_name, None, None)

    # Changelog detection (prevent publishing if the Changelog is not present
    changelog_path = Path(f"{script_dir}/../dist/{plugin_name}/CHANGELOG.md").resolve()
    assert changelog_path.exists()

    if exists_on_ts:
        # If the version matches the current one on Thunderstore, fail w/ warning
        metadata = get_thunderstore_metadata(team_name, plugin_name)
        latest_version = metadata["version"]
        manifest_path = Path(
            f"{script_dir}/../dist/{plugin_name}/manifest.json"
        ).resolve()
        manifest_version = None
        manifest_dependencies = None
        with open(manifest_path, "r") as file:
            data = json.load(file)
            manifest_version = data["version_number"]
            manifest_dependencies = data["dependencies"]
        print(f"Thunderstore version: {latest_version}")
        print(f"New version: {manifest_version}")
        assert latest_version != manifest_version

    # Check the latest dependency string of the manifest mods on Thunderstore using API and fail w/ warning if not a match
    for dep_str in manifest_dependencies:
        dep_metadata = get_thunderstore_metadata(
            *get_request_args_from_dependency_string(dep_str)
        )
        ts_dep_str = dep_metadata["dependency_string"]
        print(f"Thunderstore dependency: {ts_dep_str}")
        print(f"Manifest: {dep_str}")
        assert ts_dep_str == dep_str

    print("Completed successfully.")
    now = datetime.datetime.now()
    print(f"Time: {now.time()}")
    print("Reminders:\n- Update CHANGELOG.md\n- Update dependency strings if needed")
