#!/usr/bin/env python3
"""Deliver a merged main revision (or an explicit worktree preview) to a Mac window."""
import argparse
import fcntl
import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import time

sys.path.insert(0, str(Path(__file__).resolve().parent))
import unity_gui  # noqa: E402  (tools/ is not a package)


def run(*args, cwd=None):
    return subprocess.check_output(args, cwd=cwd, text=True).strip()


def log(message):
    print(message, flush=True)


# The quit Apple Event to one exact PID; tools/unity_gui.py quit sends the same one to a capture's editor.
request_normal_quit = unity_gui.request_normal_quit


def wait_for_editor_shutdown(process, build_ok):
    """Owned build editors exit by themselves after a good build; never turn cleanup into a crash.

    A background editor can idle instead of exiting, so after good evidence it gets one normal quit
    request. A failed or unfinished build stays open for inspection."""
    if process.poll() is not None:
        return
    try:
        process.wait(timeout=20)
        return
    except subprocess.TimeoutExpired:
        pass
    if build_ok:
        log('Build editor is still open after a good build; asking it to quit normally…')
        if request_normal_quit(process.pid):
            try:
                process.wait(timeout=120)
                return
            except subprocess.TimeoutExpired:
                pass
    if process.poll() is None:
        log('Build editor is still open for inspection; quit it normally. It has not been terminated.')


def validate_build_evidence(result, revision):
    if not result.get('ok'):
        raise RuntimeError('Build failed; existing game is unchanged: ' + result.get('error', 'unknown error'))
    if result.get('revision') != revision:
        raise RuntimeError('Build evidence revision does not match source revision: '
                           + str(result.get('revision')) + ' != ' + revision)
    if result.get('scene') != 'Assets/Scenes/HarborDiamond.unity':
        raise RuntimeError('Build evidence does not name the Harbor standalone scene.')


def sync_main(main):
    if run('git', 'branch', '--show-current', cwd=main) != 'main':
        raise RuntimeError('The primary checkout is not on main; leave its branch untouched.')
    log('Fetching merged changes and fast-forwarding local main…')
    subprocess.run(['git', 'fetch', 'origin'], cwd=main, check=True)
    # Git refuses overlaps with local edits. Never stash, reset, or force main.
    subprocess.run(['git', 'merge', '--ff-only', 'origin/main'], cwd=main, check=True)
    revision = run('git', 'rev-parse', 'HEAD', cwd=main)
    if revision != run('git', 'rev-parse', 'origin/main', cwd=main):
        raise RuntimeError('Local main contains unpublished commits; refusing to label them as merged.')
    return revision


def trial_overlay(source, name):
    """The trial overlay the window plays, as the game names it (trials/<name>), or None for the shipped data.

    It must be a folder under trials/ in the revision being built: the game refuses a named overlay it cannot find,
    and a window that quietly played the shipped table under a trial's name would be worse."""
    if not name:
        return None
    relative = Path(name)
    if relative.is_absolute() or '..' in relative.parts or len(relative.parts) < 2 or relative.parts[0] != 'trials':
        raise RuntimeError('Name a trial overlay under trials/, such as trials/<name>: ' + name)
    if not (source / relative).is_dir():
        raise RuntimeError('Revision has no trial overlay ' + name + '; nothing was restarted.')
    return relative.as_posix()


def runtime_data_files(data):
    """The files a player build carries, as data/package.json names them (RuntimePackage.Files in the sim)."""
    package = json.loads((data / 'package.json').read_text())
    extensions = {e.lower() for e in package['extensions']}
    files = []
    for folder in package['runtime']:
        for path in (data / folder).rglob('*'):
            if path.is_file() and not path.name.startswith('.') and path.suffix.lower() in extensions:
                files.append(path.relative_to(data).as_posix())
    return sorted(files)


def copy_runtime_data(data, target):
    """Copy the runtime catalogs only: agent ledgers and bake scripts stay in the repository."""
    for relative in runtime_data_files(data):
        destination = target / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(data / relative, destination)


def refuse_unless_replace(players, replace, consequence):
    """Delivery closes the open game window. Name what it is, and stop unless the caller said --replace."""
    if players and not replace:
        raise RuntimeError('A delivered game window is open: ' + '; '.join(unity_gui.describe_player(p) for p in players)
                           + '. Delivery would close it and end any match in it; ' + consequence
                           + ' Re-run with --replace when closing it is yours to do (docs/local-player.md).')


def close_players(players, replacement, app):
    """Quit the delivered windows this delivery replaces, one by one, saying which revision and trial each played."""
    for player in players:
        log('Closing ' + unity_gui.describe_player(player) + ' for ' + replacement + '…')
        pid = player['pid']
        try:
            os.kill(pid, signal.SIGTERM)
        except ProcessLookupError:
            continue
        for _ in range(100):
            try:
                os.kill(pid, 0)
            except ProcessLookupError:
                break
            time.sleep(0.1)
        else:
            raise RuntimeError('Old game did not quit; not force-killing it: ' + unity_gui.describe_player(player)
                               + '. New app: ' + str(app))


def _stamp(path):
    """The time_ns suffix delivery puts on release and build worktree names; newest sorts first."""
    try:
        return int(path.name.rsplit('-', 1)[1])
    except (IndexError, ValueError):
        return 0


def releases_newest_first(state):
    root = state / 'releases'
    if not root.is_dir():
        return []
    return sorted((p for p in root.iterdir() if p.is_dir()), key=_stamp, reverse=True)


def seed_library(project, version, state, listing=None):
    """Clone the Unity Library of the newest good build into a fresh build worktree, so Unity imports only what changed.

    A seed is a Library that built and launched a release (its revision.json names the worktree), on the same Unity
    version, with no editor open on it. The clone is copy-on-write (APFS), so it is fast and costs no disk until written.
    Returns the seed worktree, or None when the build imports from scratch."""
    if (project / 'Library').exists():
        return None
    for release in releases_newest_first(state):
        source = unity_gui._json(release / 'revision.json').get('source')
        if not source:
            continue
        seed = Path(source) / 'unity'
        if not (seed / 'Library').is_dir() or seed == project:
            continue
        try:
            seed_version = unity_gui.project_version(seed)
        except (OSError, ValueError):
            continue
        if seed_version != version or unity_gui.editor_for(seed, listing):
            continue
        try:
            subprocess.run(['cp', '-Rc', str(seed / 'Library'), str(project / 'Library')], check=True,
                           stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
        except (subprocess.CalledProcessError, OSError) as error:
            shutil.rmtree(project / 'Library', ignore_errors=True)
            log('Could not clone the Unity Library from ' + str(seed.parent) + ' (' + str(error) + '); importing from scratch.')
            return None
        return seed.parent
    return None


def seeded_hint(seed):
    if not seed:
        return ''
    return ('. The Unity Library was seeded from ' + str(seed)
            + '; if the log points at the import, re-run with --fresh-library.')


def prune(main, state, keep, listing=None):
    """Remove old releases and build worktrees; keep the newest `keep`, every open window's release, and their sources.

    A build worktree with an editor open on it is never removed. Returns (releases removed, worktrees removed)."""
    releases = releases_newest_first(state)
    in_use = {str(Path(p['app']).parent) for p in unity_gui.players(checkout=main, listing=listing)}
    current = unity_gui._json(state / 'current.json').get('app')
    if current:
        in_use.add(str(Path(current).parent))
    kept = [r for i, r in enumerate(releases) if i < keep or str(r) in in_use]
    removed_releases = 0
    for release in releases:
        if release not in kept:
            shutil.rmtree(release, ignore_errors=True)
            removed_releases += 1

    sources = {unity_gui._json(r / 'revision.json').get('source') for r in kept}
    root = (main.parent / 'scratchpad').resolve()
    listed = run('git', 'worktree', 'list', '--porcelain', cwd=main).splitlines()
    worktrees = sorted((Path(line[len('worktree '):]) for line in listed if line.startswith('worktree ')),
                       key=_stamp, reverse=True)
    builds = [w for w in worktrees if w.resolve().parent == root and w.name.startswith('wt-player-')]
    removed_worktrees = 0
    for i, worktree in enumerate(builds):
        if i < keep or str(worktree) in sources or str(worktree.resolve()) in sources or unity_gui.editor_for(worktree / 'unity', listing):
            continue
        # --force: the Library, Temp and Builds folders are untracked by design.
        done = subprocess.run(['git', 'worktree', 'remove', '--force', str(worktree)], cwd=main,
                              stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
        if done.returncode == 0:
            removed_worktrees += 1
        else:
            log('Kept build worktree ' + str(worktree) + ': ' + done.stderr.strip())
    return removed_releases, removed_worktrees


def prune_only(args):
    repo = Path(__file__).resolve().parents[1]
    main = Path(run('git', 'rev-parse', '--path-format=absolute', '--git-common-dir', cwd=repo)).parent
    state = unity_gui.support_dir() / 'local-player'
    state.mkdir(parents=True, exist_ok=True)
    # The same locks as a delivery: never prune under a build or a capture that may be using a worktree.
    with unity_gui.hold('local-player prune', worktree=repo, command=' '.join(sys.argv), log=log), \
            (state / 'delivery.lock').open('w') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise RuntimeError('A local player delivery is running.')
        removed = prune(main, state, args.keep)
    log('Removed ' + str(removed[0]) + ' releases and ' + str(removed[1]) + ' build worktrees; kept the newest '
        + str(args.keep) + ' and any open window\'s.')


def deliver(args):
    repo = Path(__file__).resolve().parents[1]
    common = Path(run('git', 'rev-parse', '--path-format=absolute', '--git-common-dir', cwd=repo))
    main = common.parent
    state = unity_gui.support_dir() / 'local-player'
    state.mkdir(parents=True, exist_ok=True)
    purpose = ('local-player delivery of ' + ('preview ' + str(args.preview) if args.preview else 'main')
               + (' on ' + args.trial if args.trial else ''))
    # One GUI Unity user on this Mac at a time: a capture or a build in another session refuses this delivery by
    # name, and this delivery refuses them. delivery.lock still excludes copies of this script from before the lock.
    with unity_gui.hold(purpose, worktree=repo, command=' '.join(sys.argv), log=log), \
            (state / 'delivery.lock').open('w') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise RuntimeError('Another local player delivery is running.')
        # Say what this would close before anything is built or main moves.
        refuse_unless_replace(unity_gui.players(checkout=main), args.replace, 'nothing was built and main was not moved.')
        if args.preview:
            source = Path(args.preview).expanduser().resolve()
            if source == main:
                raise RuntimeError('Preview must use a dedicated worktree, not the main checkout.')
            if run('git', 'status', '--porcelain', '--untracked-files=no', cwd=source):
                raise RuntimeError('Commit or preserve tracked preview edits before building a reproducible preview.')
            revision = run('git', 'rev-parse', 'HEAD', cwd=source)
            label = 'preview'
        else:
            revision = sync_main(main)
            label = 'main'

        source = main.parent / 'scratchpad' / ('wt-player-' + revision[:10] + '-' + str(time.time_ns()))
        source.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(['git', 'worktree', 'add', '--detach', str(source), revision], cwd=main, check=True)
        trial = trial_overlay(source, args.trial)

        version = unity_gui.project_version(source / 'unity')
        editor = unity_gui.editor_binary(version)
        project = source / 'unity'
        # Never take over an editor someone already has open on this worktree.
        processes = run('ps', '-ax', '-o', 'pid=,command=').splitlines()
        for line in processes:
            if '-projectPath ' + str(project) in line and '/Contents/MacOS/Unity ' in line:
                raise RuntimeError('Close the Unity editor on this build worktree first: ' + str(project))
        seed = None if args.fresh_library else seed_library(project, version, state)
        log('Unity Library seeded from ' + str(seed) if seed else 'Unity Library: full import (no seed).')
        temp = project / 'Temp'
        temp.mkdir(exist_ok=True)
        # Keep evidence outside Temp: a normal editor exit may remove Temp.
        done = state / 'build-result.json'
        done.unlink(missing_ok=True)
        # Unity clears Temp at startup: write the request through executeMethod after load.
        build_log = state / 'build.log'
        log('Building ' + label + ' ' + revision[:10] + ' in an isolated worktree…')
        build_env = os.environ.copy()
        build_env['GS_BUILD_REVISION'] = revision
        build_env['GS_BUILD_EVIDENCE'] = str(done)
        build_env['GS_BUILD_QUIT_WHEN_DONE'] = '1'
        process = subprocess.Popen([str(editor), '-projectPath', str(project),
                                    '-executeMethod', 'GrandSluggers.EditorTools.PlayerBuildGate.MenuBuildMac',
                                    '-logFile', str(build_log)],
                                   stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT, env=build_env)
        build_ok = False
        try:
            deadline = time.monotonic() + args.timeout
            next_status = time.monotonic() + 30
            while not done.exists():
                if process.poll() is not None:
                    if done.exists():
                        break
                    raise RuntimeError('Unity exited before finishing. See ' + str(build_log) + seeded_hint(seed))
                if time.monotonic() >= deadline:
                    raise RuntimeError('Build timed out; existing game is unchanged. See ' + str(build_log) + seeded_hint(seed))
                if time.monotonic() >= next_status:
                    log('Unity is still building; current game remains available. Log: ' + str(build_log))
                    next_status += 30
                time.sleep(1)
            result = json.loads(done.read_text())
            build_ok = result.get('ok') is True
            if not build_ok and seed:
                result['error'] = str(result.get('error', 'unknown error')) + seeded_hint(seed)
            validate_build_evidence(result, revision)
            built = Path(result['exe'])
            if built != project / 'Builds/osx/GrandSluggers.app' or not (built / 'Contents/MacOS/Grand Sluggers').is_file():
                raise RuntimeError('Build result does not contain the expected Mac player.')
        finally:
            wait_for_editor_shutdown(process, build_ok)

        release = state / 'releases' / (label + '-' + revision[:10] + '-' + str(time.time_ns()))
        release.mkdir(parents=True)
        app = release / 'GrandSluggers.app'
        shutil.copytree(built, app, symlinks=True)
        # Application.dataPath is <app>/Contents; the game loads ../../data.
        copy_runtime_data(source / 'data', release / 'data')
        if trial:
            # A trial overlay resolves beside data/ (GRAND_SLUGGERS_TRIAL=trials/<name> names <release>/trials/<name>).
            shutil.copytree(source / trial, release / trial)
        profile = trial or 'shipped'
        (release / 'revision.json').write_text(json.dumps(dict(revision=revision, kind=label, source=str(source),
                                                               dataProfile=profile,
                                                               librarySeed=str(seed) if seed else None), indent=2))
        (release / 'build-evidence.json').write_text(json.dumps(result, indent=2))
        # Quit only this project's old standalone players, after the new build/data exist. One may have opened
        # during the build, so look again, and still close nothing the caller did not say to replace.
        players = unity_gui.players(checkout=main)
        refuse_unless_replace(players, args.replace, 'the new build is kept at ' + str(app) + '.')
        close_players(players, label + ' ' + revision[:10] + ' on the ' + profile + ' data', app)
        executable = app / 'Contents/MacOS/Grand Sluggers'
        player_log = release / 'player.log'
        player_env = os.environ.copy()
        player_env.pop('GRAND_SLUGGERS_TRIAL', None)
        player_env.pop('GRAND_SLUGGERS_DATA', None)
        if trial:
            player_env['GRAND_SLUGGERS_TRIAL'] = trial
        child = subprocess.Popen([str(executable), '-screen-fullscreen', '0', '-screen-width', '1280',
                                  '-screen-height', '800', '-logFile', str(player_log)],
                                 stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT, start_new_session=True, env=player_env)
        time.sleep(3)
        if child.poll() is not None:
            raise RuntimeError('New player exited. Previous builds are retained. See ' + str(player_log))
        launch = dict(ok=True, kind='launch-only', revision=revision, scene=result['scene'], app=str(app), pid=child.pid,
                     dataProfile=profile, observedSeconds=3, playerLog=str(player_log),
                     utc=time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()))
        launch_path = release / 'launch-evidence.json'
        launch_path.write_text(json.dumps(launch, indent=2))
        (state / 'current.json').write_text(json.dumps(dict(revision=revision, kind=label, app=str(app), pid=child.pid,
                                                            dataProfile=profile, log=str(player_log),
                                                            buildEvidence=str(release / 'build-evidence.json'),
                                                            launchEvidence=str(launch_path)), indent=2))
        log('Running ' + label + ' ' + revision[:10] + ' on the ' + profile + ' data in its own window: ' + str(app))
        removed = prune(main, state, args.keep)
        log('Kept the newest ' + str(args.keep) + ' releases and build worktrees (plus any open window\'s); removed '
            + str(removed[0]) + ' releases and ' + str(removed[1]) + ' build worktrees.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--preview', metavar='WORKTREE', help='Build a committed worktree without updating main.')
    parser.add_argument('--timeout', type=int, default=900, help='Build timeout in seconds (default: 900).')
    parser.add_argument('--trial', metavar='OVERLAY',
                        help='Play a trial overlay over the shipped data, e.g. trials/<name> (GRAND_SLUGGERS_TRIAL).')
    parser.add_argument('--replace', action='store_true',
                        help='Close the delivered game window this replaces. Without it, delivery names the open '
                             "window's revision and trial and stops before building.")
    parser.add_argument('--fresh-library', action='store_true',
                        help='Import from scratch instead of cloning the last good build\'s Unity Library.')
    parser.add_argument('--keep', type=int, default=3,
                        help='Releases and build worktrees to keep after a good delivery (default: 3).')
    parser.add_argument('--prune-only', action='store_true',
                        help='Only remove old releases and build worktrees (same rules as after a delivery).')
    args = parser.parse_args()
    if args.keep < 1:
        parser.error('--keep must be at least 1.')
    if sys.platform != 'darwin':
        parser.error('Standalone local delivery currently supports macOS only.')
    try:
        prune_only(args) if args.prune_only else deliver(args)
    except (RuntimeError, subprocess.CalledProcessError, OSError, ValueError) as error:
        print('local-player: ' + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
