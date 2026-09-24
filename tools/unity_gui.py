#!/usr/bin/env python3
"""One GUI Unity user on this Mac at a time, and the by-PID way to front or click an editor.

Personal Unity cannot -batchmode, so captures, player builds and deliveries all drive a GUI editor, and Play only
advances in the frontmost editor. Several sessions share Jack's one Mac. A session takes this lock before it
launches, fronts or clicks an editor and before it delivers a player, and releases it on exit. A live holder
refuses the next session by name. A holder that is gone leaves a stale lock, and the next acquire clears it.

Front an editor by its PID (front / menu below). Never activate Unity by name. With no Unity running, that
starts a bare, projectless editor; with two running, it fronts either one (docs/editor-startup.md)."""
import argparse
import contextlib
import datetime
import fcntl
import json
import os
from pathlib import Path
import re
import subprocess
import sys

HELD = 75  # EX_TEMPFAIL: another session holds the lock, or Jack's window is open; try again when it is free.
EDITOR = re.compile(r'/Unity\.app/Contents/MacOS/Unity(?=\s|$)')
PROJECT = re.compile(r'(?:^|\s)-projectpath\s+(.+?)(?=\s+-[A-Za-z]|\s*$)', re.IGNORECASE)
BATCH = re.compile(r'(?:^|\s)-batchmode(?=\s|$)', re.IGNORECASE)
PLAYER = '/GrandSluggers.app/Contents/MacOS/Grand Sluggers'
# Where Unity Hub installs editors, one folder per version. UNITY_HUB_EDITORS moves it (tools/unity-compile.sh too).
HUB_EDITORS = '/Applications/Unity/Hub/Editor'


def project_version(project):
    """The editor version a Unity project pins: m_EditorVersion in ProjectSettings/ProjectVersion.txt."""
    path = Path(project) / 'ProjectSettings' / 'ProjectVersion.txt'
    for line in path.read_text().splitlines():
        if line.startswith('m_EditorVersion:'):
            version = line.split(':', 1)[1].strip()
            if version:
                return version
    raise ValueError(str(path) + ' does not pin m_EditorVersion')


def installed_editors():
    """The versions installed under the Hub editor folder, sorted."""
    hub = Path(os.environ.get('UNITY_HUB_EDITORS', HUB_EDITORS))
    return sorted(p.name for p in hub.iterdir() if (p / 'Unity.app').is_dir()) if hub.is_dir() else []


def editor_binary(version):
    """The Unity executable for one version. A missing editor is an error that names the version, where it was
    looked for and what is installed, so an upgrade never runs a build in the old editor."""
    hub = Path(os.environ.get('UNITY_HUB_EDITORS', HUB_EDITORS))
    binary = hub / version / 'Unity.app' / 'Contents' / 'MacOS' / 'Unity'
    if not binary.exists():
        raise RuntimeError('Unity ' + version + ' (unity/ProjectSettings/ProjectVersion.txt) is not installed at '
                           + str(binary.parents[2]) + '; installed: ' + (', '.join(installed_editors()) or 'none')
                           + '. Install it with Unity Hub, or set UNITY_HUB_EDITORS to the folder that holds it.')
    return binary


class LockHeld(RuntimeError):
    """The GUI Unity lock belongs to another live session, cannot be read, or Jack's window would lose focus."""


def support_dir():
    """~/Library/Application Support/Grand Sluggers, shared with tools/local-player.py. GS_SUPPORT_DIR moves it for tests."""
    override = os.environ.get('GS_SUPPORT_DIR')
    return Path(override) if override else Path.home() / 'Library/Application Support/Grand Sluggers'


def lock_path():
    return support_dir() / 'unity-gui.lock'


def _utc(now=None):
    return (now or datetime.datetime.now(datetime.timezone.utc)).strftime('%Y-%m-%dT%H:%M:%SZ')


def _since(stamp, now=None):
    try:
        then = datetime.datetime.strptime(stamp, '%Y-%m-%dT%H:%M:%SZ').replace(tzinfo=datetime.timezone.utc)
    except (TypeError, ValueError):
        return ''
    minutes = int(((now or datetime.datetime.now(datetime.timezone.utc)) - then).total_seconds() // 60)
    if minutes < 0:
        return ''
    return ' (' + (str(minutes // 60) + ' h ' if minutes >= 60 else '') + str(minutes % 60) + ' min ago)'


def _stat_start(pid):
    """(state, start) as ps prints them, ('', '') when ps names no such process, None when ps cannot answer."""
    try:
        reply = subprocess.run(['ps', '-o', 'stat=', '-o', 'lstart=', '-p', str(int(pid))],
                               capture_output=True, text=True, timeout=10)
    except (OSError, subprocess.TimeoutExpired):
        return None
    fields = reply.stdout.split()
    if not fields:
        return ('', '')
    return (fields[0], ' '.join(fields[1:]))


def _command(pid):
    try:
        reply = subprocess.run(['ps', '-o', 'command=', '-p', str(int(pid))], capture_output=True, text=True, timeout=10)
    except (OSError, subprocess.TimeoutExpired):
        return ''
    return reply.stdout.strip()


def running(pid, start=None):
    """Whether pid is still the process that took the lock: alive, not a zombie, and started when the lock says."""
    try:
        pid = int(pid)
    except (TypeError, ValueError):
        return False
    if pid <= 0:
        return False
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        pass  # alive, owned by another user
    seen = _stat_start(pid)
    if seen is None:
        return True  # ps could not answer; the kernel says the PID exists
    state, started = seen
    if not state or state.startswith('Z'):
        return False
    # A PID the kernel handed to a new process is not the holder.
    return not (start and started and started != start)


@contextlib.contextmanager
def _guard(path):
    """Serialize read-decide-write on the lock. flock dies with its process, so the guard itself can never go stale."""
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(str(path) + '.guard', 'a') as guard:
        fcntl.flock(guard, fcntl.LOCK_EX)
        yield


def _read(path):
    """(holder, problem): holder is the lock's record, or None when the lock is free; problem says why it cannot be read."""
    try:
        text = path.read_text()
    except FileNotFoundError:
        return None, None
    except OSError as error:
        return None, str(error)
    try:
        holder = json.loads(text)
    except ValueError as error:
        return None, 'not JSON: ' + str(error)
    if not isinstance(holder, dict) or not isinstance(holder.get('pid'), int):
        return None, 'no holder PID'
    return holder, None


def _write(path, record):
    staged = path.parent / (path.name + '.' + str(os.getpid()) + '.tmp')
    staged.write_text(json.dumps(record, indent=2) + '\n')
    os.replace(str(staged), str(path))


def describe(holder, now=None):
    text = str(holder.get('purpose') or 'an unnamed GUI Unity use') + ' (pid ' + str(holder.get('pid'))
    if holder.get('worktree'):
        text += ', worktree ' + str(holder['worktree'])
    if holder.get('started'):
        text += ', since ' + str(holder['started']) + _since(holder['started'], now)
    return text + ')'


def acquire(purpose, pid, worktree=None, command=None):
    """Take the lock for pid. Returns (record, notes, fresh). Raises LockHeld naming a live holder.

    fresh is False when pid already held it, so a nested hold does not release its caller's lock."""
    pid = int(pid)
    started = _stat_start(pid)
    if not running(pid):
        raise RuntimeError('pid ' + str(pid) + ' is not running; pass the PID of the process that holds the lock ($$ in a shell).')
    path = lock_path()
    with _guard(path):
        holder, problem = _read(path)
        if problem:
            raise LockHeld('Cannot read the GUI Unity lock ' + str(path) + ' (' + problem + '). Remove it by hand only '
                           'when no Unity capture, player build or delivery is running.')
        notes = []
        if holder is not None:
            if running(holder['pid'], holder.get('processStart')):
                if holder['pid'] == pid:
                    return holder, notes, False
                raise LockHeld('GUI Unity is in use by ' + describe(holder) + '. One session drives a GUI editor on '
                               'this Mac at a time: wait for it to finish, or ask that session. Lock: ' + str(path))
            notes.append('Cleared a stale GUI Unity lock: ' + describe(holder) + ' is no longer running.')
        record = dict(pid=pid, processStart=started[1] if started else None, purpose=purpose,
                      worktree=str(worktree) if worktree else os.getcwd(), command=command or _command(pid), started=_utc())
        _write(path, record)
        return record, notes, True


def release(pid):
    """Drop the lock if pid holds it. Returns whether it did; a lock someone else holds is left alone."""
    path = lock_path()
    with _guard(path):
        holder, problem = _read(path)
        if problem or holder is None or holder['pid'] != int(pid):
            return False
        path.unlink()
        return True


@contextlib.contextmanager
def hold(purpose, pid=None, worktree=None, command=None, log=print):
    """Hold the lock for this process while the block runs; release it however the block ends."""
    pid = os.getpid() if pid is None else pid
    record, notes, fresh = acquire(purpose, pid, worktree=worktree, command=command)
    for note in notes:
        log(note)
    try:
        yield record
    finally:
        if fresh:
            release(pid)


def holder():
    """The current lock record and whether its holder still runs: (record, alive), (None, False) when free."""
    record, problem = _read(lock_path())
    if problem:
        raise LockHeld('Cannot read the GUI Unity lock ' + str(lock_path()) + ' (' + problem + ').')
    if record is None:
        return None, False
    return record, running(record['pid'], record.get('processStart'))


def _ps():
    return subprocess.run(['ps', '-A', '-o', 'pid=', '-o', 'lstart=', '-o', 'command='],
                          capture_output=True, text=True, check=True).stdout


def _rows(listing):
    for line in listing.splitlines():
        fields = line.split(None, 6)
        if len(fields) == 7 and fields[0].isdigit():
            yield int(fields[0]), ' '.join(fields[1:6]), fields[6]


def _norm(path):
    return os.path.realpath(os.path.expanduser(path.strip().rstrip('/') or '/'))


def editors(listing=None):
    """Every GUI Unity editor: pid, start, and the project it opened (None for a bare, projectless editor).

    Asset import workers and batch runs share the editor binary; they pass -batchMode and are not GUI editors."""
    found = []
    for pid, start, command in _rows(_ps() if listing is None else listing):
        binary = EDITOR.search(command)
        if not binary or ' -' in command[:binary.start()]:
            continue  # the Hub, or the editor's path appearing as some other program's argument
        arguments = command[binary.end():]
        if BATCH.search(arguments):
            continue
        project = PROJECT.search(arguments)
        found.append(dict(pid=pid, start=start, project=_norm(project.group(1)) if project else None))
    return found


def editor_for(project, listing=None):
    """The GUI editor open on this Unity project, or None. Unity allows one editor per project."""
    wanted = _norm(str(project))
    return next((e for e in editors(listing) if e['project'] == wanted), None)


def _checkout(worktree):
    """The primary checkout behind a worktree (Git's common directory's parent), or None outside Git."""
    try:
        common = subprocess.run(['git', '-C', str(worktree), 'rev-parse', '--path-format=absolute', '--git-common-dir'],
                                capture_output=True, text=True, timeout=30)
    except (OSError, subprocess.TimeoutExpired):
        return None
    return Path(common.stdout.strip()).parent if common.returncode == 0 and common.stdout.strip() else None


def _json(path):
    try:
        value = json.loads(path.read_text())
    except (OSError, ValueError):
        return {}
    return value if isinstance(value, dict) else {}


def players(checkout=None, listing=None):
    """Game windows tools/local-player.py delivered, plus a player built into the primary checkout, that still run.

    Each names its pid, revision, kind (main / preview), data profile (shipped or a trial) and delivery time."""
    releases = str(support_dir() / 'local-player' / 'releases') + '/'
    built = str(Path(checkout) / 'unity/Builds/osx/GrandSluggers.app/Contents/MacOS/Grand Sluggers') if checkout else None
    found = []
    for pid, start, command in _rows(_ps() if listing is None else listing):
        if built and (command == built or command.startswith(built + ' ')):
            found.append(dict(pid=pid, start=start, kind='checkout', revision=None, dataProfile=None,
                              app=built[:-len('/Contents/MacOS/Grand Sluggers')], delivered=None))
        elif command.startswith(releases) and PLAYER in command:
            release = Path(command[:command.index(PLAYER)])
            info = _json(release / 'revision.json')
            found.append(dict(pid=pid, start=start, kind=info.get('kind') or 'release', revision=info.get('revision'),
                              dataProfile=info.get('dataProfile') or 'shipped', app=str(release / 'GrandSluggers.app'),
                              delivered=_json(release / 'launch-evidence.json').get('utc')))
    return found


def describe_player(player):
    if player.get('kind') == 'checkout':
        return 'a player built in the primary checkout (' + str(player['app']) + ', pid ' + str(player['pid']) + ')'
    profile = player.get('dataProfile') or 'shipped'
    text = (str(player.get('kind') or 'release') + ' ' + (str(player.get('revision') or '')[:10] or 'of unknown revision')
            + ' on ' + ('the shipped data' if profile == 'shipped' else profile) + ' (pid ' + str(player['pid']))
    if player.get('delivered'):
        text += ', delivered ' + str(player['delivered'])
    return text + ')'


def _owned(owner):
    record, alive = holder()
    if record is None or not alive or record['pid'] != int(owner):
        raise LockHeld('Only the GUI Unity lock holder fronts or clicks an editor. '
                       + ('The lock is held by ' + describe(record) + '.' if record is not None and alive else
                          'Take it first: python3 tools/unity_gui.py acquire --pid $$ --purpose "<what>".'))


def _editor(pid):
    if not any(e['pid'] == int(pid) for e in editors()):
        raise RuntimeError('pid ' + str(pid) + ' is not a GUI Unity editor. Front the editor you launched, or the one '
                           '`python3 tools/unity_gui.py editor <project>` names.')


def _quote(text):
    return '"' + str(text).replace('\\', '\\\\').replace('"', '\\"') + '"'


def _osascript(script):
    subprocess.run(['osascript'], input=script, text=True, capture_output=True, check=True, timeout=30)


def front(pid, owner):
    """Bring exactly this editor process frontmost. Only the lock holder may."""
    _owned(owner)
    _editor(pid)
    _osascript('tell application "System Events" to set frontmost of (first process whose unix id is '
               + str(int(pid)) + ') to true\n')


def menu(pid, owner, menu_name, item):
    """Front exactly this editor and click one of its menu items. Only the lock holder may."""
    _owned(owner)
    _editor(pid)
    _osascript('tell application "System Events"\n'
               '  tell (first process whose unix id is ' + str(int(pid)) + ')\n'
               '    set frontmost to true\n'
               '    delay 0.4\n'
               '    click menu item ' + _quote(item) + ' of menu ' + _quote(menu_name) + ' of menu bar 1\n'
               '  end tell\n'
               'end tell\n')


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


def quit_editor(pid, owner):
    """Ask exactly this editor to quit normally, as Quit does; never a signal (docs/editor-startup.md). Holder only."""
    _owned(owner)
    _editor(pid)
    if not request_normal_quit(pid):
        raise RuntimeError('Could not ask editor ' + str(pid) + ' to quit; quit it from its menu.')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest='command', required=True)
    take = commands.add_parser('acquire', help='Take the lock for a PID; exit 75 naming the holder if it is held.')
    take.add_argument('--pid', type=int, required=True, help='The process that holds the lock: $$ in a shell script.')
    take.add_argument('--purpose', required=True, help='What the lock is for, as the next session will read it.')
    take.add_argument('--worktree', help='The worktree doing the work (default: the current directory).')
    take.add_argument('--focus', action='store_true',
                      help='This use brings an editor frontmost: refuse while a delivered game window is open.')
    take.add_argument('--player-open-ok', action='store_true',
                      help='With --focus: Jack said the machine is free, so taking focus from his window is fine.')
    drop = commands.add_parser('release', help='Drop the lock if this PID holds it.')
    drop.add_argument('--pid', type=int, required=True)
    commands.add_parser('status', help='Print the lock holder, every GUI editor and every delivered game window.')
    find = commands.add_parser('editor', help='Print the PID of the GUI editor open on a Unity project; exit 1 if none.')
    find.add_argument('project')
    fronting = commands.add_parser('front', help='Bring one editor PID frontmost (lock holder only).')
    fronting.add_argument('--owner', type=int, required=True, help='The lock holder PID ($$).')
    fronting.add_argument('pid', type=int)
    clicking = commands.add_parser('menu', help='Front one editor PID and click a menu item (lock holder only).')
    clicking.add_argument('--owner', type=int, required=True, help='The lock holder PID ($$).')
    clicking.add_argument('pid', type=int)
    clicking.add_argument('menu')
    clicking.add_argument('item')
    quitting = commands.add_parser('quit', help='Ask one editor PID to quit normally, as Quit does (lock holder only).')
    quitting.add_argument('--owner', type=int, required=True, help='The lock holder PID ($$).')
    quitting.add_argument('pid', type=int)
    args = parser.parse_args(argv)
    try:
        if args.command == 'acquire':
            worktree = Path(args.worktree).resolve() if args.worktree else Path.cwd()
            if args.focus and not args.player_open_ok:
                open_players = players(checkout=_checkout(worktree))
                if open_players:
                    raise LockHeld("Jack's game window is open: " + '; '.join(describe_player(p) for p in open_players)
                                   + '. ' + args.purpose + ' brings a Unity editor frontmost and would take focus from '
                                   'it. Ask Jack whether the machine is free, then re-run with --player-open-ok.')
            record, notes, fresh = acquire(args.purpose, args.pid, worktree=worktree)
            for note in notes:
                print(note, file=sys.stderr)
            print(('Took' if fresh else 'Already hold') + ' the GUI Unity lock: ' + describe(record), file=sys.stderr)
        elif args.command == 'release':
            release(args.pid)
        elif args.command == 'status':
            record, alive = holder()
            print('GUI Unity lock: ' + ('free' if record is None else ('held by ' if alive else 'stale, ')
                                        + describe(record) + ('' if alive else ' is gone; the next acquire clears it')))
            for editor in editors():
                print('Editor pid ' + str(editor['pid']) + ' since ' + editor['start'] + ': '
                      + (editor['project'] or 'no project. A bare editor, likely started by activating Unity by name; '
                         'quit it normally if no session owns it'))
            for player in players(checkout=_checkout(Path.cwd())):
                print('Game window: ' + describe_player(player))
        elif args.command == 'editor':
            found = editor_for(args.project)
            if found is None:
                return 1
            print(found['pid'])
        elif args.command == 'front':
            front(args.pid, args.owner)
        elif args.command == 'menu':
            menu(args.pid, args.owner, args.menu, args.item)
        elif args.command == 'quit':
            quit_editor(args.pid, args.owner)
    except LockHeld as error:
        print('unity_gui: ' + str(error), file=sys.stderr)
        return HELD
    except (RuntimeError, OSError, subprocess.SubprocessError) as error:
        print('unity_gui: ' + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
