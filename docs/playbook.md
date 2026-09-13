# Playbook — how a phase runs

How Phase P (plays like Sluggers) went from "it works but it is raw" to code-complete in one day, 2026-09-12, so the next phase runs the same way instead of rediscovering it. Companion to [AGENTS.md](../AGENTS.md) (the standing order) and [roadmap.md](roadmap.md) (the sequence). Checkpoint tag: `checkpoint-phase-p` on `main`.

## The shape

```
research the reference ─┐
map the code (file:line) ┼─▶ spec with status tags + gap audit + scenario ids
                         │            │
                         │            ▼
                         │   roadmap: serial epics, each owning spec sections + S-ids
                         │            │
                         │            ▼
                         │   file epics with the acceptance shape (observable / files / tests / banned)
                         │            │
                         │            ▼
                         │   one session per epic, own worktree, self-contained prompt
                         │            │
                         │            ▼
                         │   integrate onto main → gates → rebuild the standalone → close issues
                         │            │
                         └───────────▶ sittings (human) → sitting-found children → next phase
```

Every arrow is a hand-off with a written artifact. Nothing is carried in someone's head or a Slack thread.

## 1. Research before code

- **Two maps, made in parallel by read-only agents.** One maps the reference (what the game we are matching actually does, every fact cited or marked UNVERIFIED — [research-sluggers.md](research-sluggers.md) "Mechanics teardown"). The other maps the code (how it works *today*, with `file:line` for every decision, hack, roll, and literal). Neither proposes fixes.
- **Play the headless game first.** `cli match --seed 7` before any change is the baseline; it said "8–0 doubles fest, items on every other hit" before anyone read a line.
- **Corrections come from the reference, not from taste.** The first draft of the spec had lead-offs, a timing-based perfect band, and a random pickoff risk. The reference had none of them. Every such change is a numbered decision (spec §0.1) with the source that decided it.

## 2. The spec is the contract

[gameplay-spec.md](gameplay-spec.md) has four parts that make it executable rather than descriptive:

1. **Status tags on every rule** (✅ / ⚠️ / ❌) checked against a named revision. A rule without a status is an opinion.
2. **A gap audit** (Appendix A) — every ⚠️ / ❌ with its `file:line`, grouped by the epic that will fix it. This is what turns a spec into work.
3. **Scenario ids** (Appendix B, `S-xx`) — each a headless test: state, inputs, expected outcome *and reason*. An epic is closed by its scenario list, not by a description.
4. **Decisions** (§0.1) — where the reference and the shipped game differ, the call and the why. Agents do not relitigate them.

The spec outranks the code. If a session needs a rule the spec lacks, it adds the rule in the same PR.

## 3. Epics own sections; sessions own epics

- The roadmap table maps **epic → spec sections → scenario ids**. Serial by default; parallel only when the files do not overlap (P1 ∥ P2, P8 alongside P7), and the prompts say who owns which files.
- Each epic is a GitHub issue in the acceptance shape: parent, spec sections, **observable**, **files**, **tests (S-ids)**, **human gate**, **order**, **banned**. Older issues that the epic subsumes are folded in with a comment, not closed silently.
- **One session per epic in its own worktree**, launched from a chip whose prompt is self-contained: the setup (worktree command, what to read, what already landed and where), the deliverables copied from the issue, the gates to run, the human gate to leave to Jack, the delivery shape (stacked PRs, attribution, rebuild the standalone after merge). A prompt that needs the conversation to make sense is too short.
- Pick the model for the job (Opus 5 for the at-bat contract and the seat fix); state it in the prompt so the session can flag a mismatch.

## 4. Integrate, then verify main, not the branch

- **Stacked PRs go against `main`**, or the top of the stack is merged into `main` at the end. P1 and P2 merged their parts into their own parent branches; `main` only had part (a) of each until #593 / #594 integrated them. Check with `git merge-base --is-ancestor <merge sha> origin/main` for every merged PR before believing "done".
- After every merge: `dotnet test`, `cli art`, `unity-compile.sh` on `main`, then `python3 tools/local-player.py` so the window Jack plays is the revision the checklist names. Say the revision.
- Close the issues whose PRs shipped; leave epics whose exit is a sitting open and say so.
- A statistics gate that goes red at integration (S-29 when P1's power met P2's fence) is **skipped with a reason naming the epic that reopens it**, never tuned to pass. P7 owned it and turned it green with table edits and one rail.

## 5. Sittings are the exit; findings are children

- Agents do not pass human gates. Each PR body ends with "what a sitting should check". The phase ends with a consolidated checklist on the parent epic (#209) that says what each block closes.
- A sitting note becomes **one issue per finding** with observed / likely cause (from the code map) / observable when fixed / files / tests, filed under the epic that owns the lie. Ten such children were filed from one sitting and routed to P1, P2, P4, P8, and the rig epic; three were closed by the epics without a dedicated PR.
- "Do not silently patch" held: every sitting finding has an issue number and a PR number.

## What it produced (2026-09-12)

| | |
| --- | --- |
| Sessions | 10 (P0–P8 and the seat fix #579), one worktree each |
| PRs merged to `main` | 30, #561 → #604 |
| Issues opened | 23 (9 epics, 10 sitting children, spec/roadmap threads) |
| Rules moved to data | `data/rules/` batting, pitching, flight, fielding, running, stars, cpu, match |
| Tests | ≈ 800 (from ≈ 600), including the scenario harness S-01 … S-92 and S-29 over fifty seeds |
| Headless game | seed 7: 8–0, items on half the hits → 10–3 with walks, a sac fly, a ground-rule double, pickoff beats; fifty seeds 2.5–2.7 runs a side |

## What to keep doing

- Two read-only maps before the spec. Cite or mark UNVERIFIED.
- Status tags, gap audit with lines, scenario ids, numbered decisions.
- Epics own sections; prompts are self-contained; one worktree per session.
- Verify `main`, rebuild the window, name the revision.
- File findings; never patch from a sitting note.

## What to do better

- Say "PRs against `main`" in every prompt (now in the P8 prompt onward).
- Have the session that ships an epic close the sitting children it fixed, with the PR number.
- Keep the fifty-seed check in every epic's gate once it is green, so integration drift shows up in the branch, not on `main`.
