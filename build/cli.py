import click

from prepare_pub import prepare_pub
from build_mods import build_mods
from test import test_sod_common, test
from config import primary_profile_name


@click.group()
def cli():
    pass


@click.command()
def build():
    build_mods("all", None, None)


@click.command()
@click.option("--plugins", multiple=True)
@click.option("--profile", default=primary_profile_name)
def run(plugins, profile):
    test(plugins, None, profile)


@click.command()
def test_utils():
    test_sod_common()


@click.command()
@click.argument("plugin")
@click.option("--check-ts", type=click.BOOL, default=True)
def prepare(plugin, check_ts):
    prepare_pub(plugin, check_ts)


cli.add_command(build)
cli.add_command(run)
cli.add_command(test_utils)
cli.add_command(prepare)
if __name__ == "__main__":
    cli()
