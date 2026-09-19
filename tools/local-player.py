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


def request_normal_quit(pid):
    """Ask the app with this exact PID to quit normally: the quit Apple Event that Quit sends, not a signal."""
    script = ('ObjC.import("AppKit");'
              'var app = $.NSRunningApplication.runningApplicationWithProcessIdentifier(' + str(int(pid)) + ');'
              'app.isNil() ? "missing" : (app.terminate, "asked")')
    try:
        reply = subprocess.run(['osascript', '-l', 'JavaScript', '-e', script],
                               capture_output=True, text=True, timeout=30)
    except (OSError, subprocess.TimeoutExpired):
        return False
    # The caller's wait decides whether Unity quit, not terminate's return value.
    return reply.stdout.strip() == 'asked'


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
    """The trial overlay the window plays, as the game names it (trials/c80), or None for the shipped data.

    It must be a folder under trials/ in the revision being built: the game refuses a named overlay it cannot find,
    and a window that quietly played the shipped table under a trial's name would be worse."""
    if not name:
        return None
    relative = Path(name)
    if relative.is_absolute() or '..' in relative.parts or len(relative.parts) < 2 or relative.parts[0] != 'trials':
        raise RuntimeError('Name a trial overlay under trials/, such as trials/c80: ' + name)
    if not (source / relative).is_dir():
        raise RuntimeError('Revision has no trial overlay ' + name + '; nothing was restarted.')
    return relative.as_posix()


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
        trial = trial_overlay(source, args.trial)

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
                    raise RuntimeError('Unity exited before finishing. See ' + str(build_log))
                if time.monotonic() >= deadline:
                    raise RuntimeError('Build timed out; existing game is unchanged. See ' + str(build_log))
                if time.monotonic() >= next_status:
                    log('Unity is still building; current game remains available. Log: ' + str(build_log))
                    next_status += 30
                time.sleep(1)
            result = json.loads(done.read_text())
            build_ok = result.get('ok') is True
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
        shutil.copytree(source / 'data', release / 'data')
        if trial:
            # A trial overlay resolves beside data/ (GRAND_SLUGGERS_TRIAL=trials/c80 names <release>/trials/c80).
            shutil.copytree(source / trial, release / trial)
        profile = trial or 'shipped'
        (release / 'revision.json').write_text(json.dumps(dict(revision=revision, kind=label, source=str(source),
                                                               dataProfile=profile), indent=2))
        (release / 'build-evidence.json').write_text(json.dumps(result, indent=2))
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
        log('Build worktree retained for diagnostics: ' + str(source))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--preview', metavar='WORKTREE', help='Build a committed worktree without updating main.')
    parser.add_argument('--timeout', type=int, default=900, help='Build timeout in seconds (default: 900).')
    parser.add_argument('--trial', metavar='OVERLAY',
                        help='Play a trial overlay over the shipped data, e.g. trials/c80 (GRAND_SLUGGERS_TRIAL).')
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
