#!/usr/bin/env python3
"""Claude Code PreToolUse hook for Bash: refuses two commands AGENTS.md bans.

- `git add -A`, `git add --all`, `git add .` (and `./`, `:/`): sessions share this Mac, so a
  sweeping add captures another session's files. Stage explicit paths.
- `dotnet test` without `--filter`: the full suite freezes the shared Mac. Use
  `tools/test-fast.sh <Classes you touched>`; CI runs the breakage suite.

Wired in .claude/settings.json. Reads the hook JSON on stdin; prints a deny decision on stdout.
A command it cannot parse is allowed: the hook guards two mistakes, it is not a sandbox.
"""
import json
import shlex
import sys

SEPARATORS = {'&&', '||', ';', '|', '&', '(', ')', '\n'}
SWEEPING_PATHSPECS = {'.', './', ':/', ':/.'}
SHELLS = {'bash', 'sh', 'zsh'}
# git options that take a separate value before the subcommand.
GIT_VALUE_OPTIONS = {'-C', '-c', '--git-dir', '--work-tree', '--namespace', '--exec-path'}


def segments(command):
    lexer = shlex.shlex(command, posix=True, punctuation_chars=';&|()')
    lexer.whitespace = ' \t\r'
    lexer.whitespace_split = True
    current = []
    for token in lexer:
        if token in SEPARATORS or set(token) <= set(';&|()\n'):
            if current:
                yield current
            current = []
            continue
        current.append(token)
    if current:
        yield current


def strip_env(words):
    # `FOO=1 dotnet test`, `env FOO=1 git add -A`, `sudo git add .`
    i = 0
    while i < len(words):
        w = words[i]
        if w in ('env', 'sudo', 'command', 'exec', 'time', 'nice') or ('=' in w and not w.startswith('-') and w.split('=', 1)[0].isidentifier()):
            i += 1
            continue
        break
    return words[i:]


def git_add_reason(words):
    if not words or words[0].rsplit('/', 1)[-1] != 'git':
        return None
    i = 1
    while i < len(words) and words[i].startswith('-'):
        i += 2 if words[i] in GIT_VALUE_OPTIONS else 1
    if i >= len(words) or words[i] != 'add':
        return None
    for arg in words[i + 1:]:
        if arg == '--':
            continue
        if arg in ('--all', '-A') or (arg.startswith('-') and not arg.startswith('--') and 'A' in arg[1:]):
            return 'git add -A'
        if arg in SWEEPING_PATHSPECS:
            return 'git add ' + arg
    return None


def dotnet_test_reason(words):
    if not words or words[0].rsplit('/', 1)[-1] != 'dotnet':
        return None
    if len(words) < 2 or words[1] != 'test':
        return None
    rest = words[2:]
    if any(w == '--filter' or w.startswith('--filter=') or w == '--list-tests' for w in rest):
        return None
    return 'dotnet test without --filter'


def reasons(command, depth=0):
    found = []
    for words in segments(command):
        words = strip_env(words)
        for check in (git_add_reason, dotnet_test_reason):
            r = check(words)
            if r:
                found.append(r)
        # `bash -c "git add -A"`, `sh -lc '...'`, `eval "..."`: look inside the script, not inside
        # any quoted text (a commit message or an echo may name a banned command).
        if depth < 2:
            for script in inner_scripts(words):
                found.extend(reasons(script, depth + 1))
    return found


def inner_scripts(words):
    if not words:
        return []
    name = words[0].rsplit('/', 1)[-1]
    if name == 'eval':
        return [' '.join(words[1:])]
    if name in SHELLS:
        for i, w in enumerate(words[1:], 1):
            if w.startswith('-') and not w.startswith('--') and 'c' in w[1:] and i + 1 < len(words):
                return [words[i + 1]]
    return []


ADVICE = {
    'git add': 'Sessions share this Mac: stage explicit paths (git add path/one path/two), never a sweeping add.',
    'dotnet test': 'The full suite freezes the shared Mac: run tools/test-fast.sh <Classes you touched>; CI runs the breakage suite.',
}


def decide(payload):
    if payload.get('tool_name') != 'Bash':
        return None
    command = (payload.get('tool_input') or {}).get('command') or ''
    try:
        found = reasons(command)
    except ValueError:
        return None
    if not found:
        return None
    advice = [text for key, text in ADVICE.items() if any(r.startswith(key) for r in found)]
    reason = 'Blocked by the repo Bash guard (AGENTS.md): ' + ', '.join(dict.fromkeys(found)) + '. ' + ' '.join(advice)
    return {
        'hookSpecificOutput': {
            'hookEventName': 'PreToolUse',
            'permissionDecision': 'deny',
            'permissionDecisionReason': reason,
        }
    }


def main():
    try:
        payload = json.load(sys.stdin)
    except ValueError:
        return 0
    decision = decide(payload)
    if decision:
        json.dump(decision, sys.stdout)
    return 0


if __name__ == '__main__':
    sys.exit(main())
