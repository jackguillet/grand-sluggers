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


def run(*args, cwd=None):
    return subprocess.check_output(args, cwd=cwd, text=True).strip()


def log(message):
    print(message, flush=True)


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


def deliver(args):
    repo = Path(__file__).resolve().parents[1]
    common = Path(run('git', 'rev-parse', '--path-format=absolute', '--git-common-dir', cwd=repo))
    main = common.parent
    state = Path.home() / 'Library/Application Support/Grand Sluggers/local-player'
    state.mkdir(parents=True, exist_ok=True)
    with (state / 'delivery.lock').open('w') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise RuntimeError('Another local player delivery is running.')
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

        version = (source / 'unity/ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0].split(':', 1)[1].strip()
        editor = Path('/Applications/Unity/Hub/Editor') / version / 'Unity.app/Contents/MacOS/Unity'
        if not editor.exists():
            raise RuntimeError('Install the project Unity version first: ' + version)
        project = source / 'unity'
        # Never take over an editor someone already has open on this worktree.
        processes = run('ps', '-ax', '-o', 'pid=,command=').splitlines()
        for line in processes:
            if '-projectPath ' + str(project) in line and '/Contents/MacOS/Unity ' in line:
                raise RuntimeError('Close the Unity editor on this build worktree first: ' + str(project))
        temp = project / 'Temp'
        temp.mkdir(exist_ok=True)
        done = temp / 'gs-player-done.json'
        done.unlink(missing_ok=True)
        # Unity clears Temp at startup: write the request through executeMethod after load.
        build_log = state / 'build.log'
        log('Building ' + label + ' ' + revision[:10] + ' in an isolated worktree…')
        process = subprocess.Popen([str(editor), '-projectPath', str(project),
                                    '-executeMethod', 'GrandSluggers.EditorTools.PlayerBuildGate.MenuBuildMac',
                                    '-logFile', str(build_log)],
                                   stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + args.timeout
            next_status = time.monotonic() + 30
            while not done.exists():
                if process.poll() is not None:
                    raise RuntimeError('Unity exited before finishing. See ' + str(build_log))
                if time.monotonic() >= deadline:
                    raise RuntimeError('Build timed out; existing game is unchanged. See ' + str(build_log))
                if time.monotonic() >= next_status:
                    log('Unity is still building; current game remains available. Log: ' + str(build_log))
                    next_status += 30
                time.sleep(1)
            result = json.loads(done.read_text())
            if not result.get('ok'):
                raise RuntimeError('Build failed; existing game is unchanged: ' + result.get('error', str(build_log)))
            built = Path(result['exe'])
            if built != project / 'Builds/osx/GrandSluggers.app' or not (built / 'Contents/MacOS/Grand Sluggers').is_file():
                raise RuntimeError('Build result does not contain the expected Mac player.')
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=20)
                except subprocess.TimeoutExpired:
                    log('Build editor is still shutting down; it has not been force-killed.')

        release = state / 'releases' / (label + '-' + revision[:10] + '-' + str(time.time_ns()))
        release.mkdir(parents=True)
        app = release / 'GrandSluggers.app'
        shutil.copytree(built, app, symlinks=True)
        # Application.dataPath is <app>/Contents; the game loads ../../data.
        shutil.copytree(source / 'data', release / 'data')
        (release / 'revision.json').write_text(json.dumps(dict(revision=revision, kind=label, source=str(source)), indent=2))
        # Quit only this project's old standalone player, after the new build/data exist.
        old_main = main / 'unity/Builds/osx/GrandSluggers.app/Contents/MacOS/Grand Sluggers'
        for line in run('ps', '-ax', '-o', 'pid=,command=').splitlines():
            pid, _, command = line.strip().partition(' ')
            command = command.strip()
            recognized = command == str(old_main) or command.startswith(str(old_main) + ' ')
            recognized |= command.startswith(str(state / 'releases') + '/') and '/GrandSluggers.app/Contents/MacOS/Grand Sluggers' in command
            if recognized:
                try:
                    os.kill(int(pid), signal.SIGTERM)
                except ProcessLookupError:
                    continue
                for _ in range(100):
                    try:
                        os.kill(int(pid), 0)
                    except ProcessLookupError:
                        break
                    time.sleep(0.1)
                else:
                    raise RuntimeError('Old game did not quit; not force-killing it. New app: ' + str(app))
        executable = app / 'Contents/MacOS/Grand Sluggers'
        player_log = release / 'player.log'
        child = subprocess.Popen([str(executable), '-screen-fullscreen', '0', '-screen-width', '1280',
                                  '-screen-height', '800', '-logFile', str(player_log)],
                                 stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT, start_new_session=True)
        time.sleep(3)
        if child.poll() is not None:
            raise RuntimeError('New player exited. Previous builds are retained. See ' + str(player_log))
        (state / 'current.json').write_text(json.dumps(dict(revision=revision, kind=label, app=str(app), pid=child.pid, log=str(player_log)), indent=2))
        log('Running ' + label + ' ' + revision[:10] + ' in its own window: ' + str(app))
        log('Build worktree retained for diagnostics: ' + str(source))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--preview', metavar='WORKTREE', help='Build a committed worktree without updating main.')
    parser.add_argument('--timeout', type=int, default=900, help='Build timeout in seconds (default: 900).')
    args = parser.parse_args()
    if sys.platform != 'darwin':
        parser.error('Standalone local delivery currently supports macOS only.')
    try:
        deliver(args)
    except (RuntimeError, subprocess.CalledProcessError, OSError, ValueError) as error:
        print('local-player: ' + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
