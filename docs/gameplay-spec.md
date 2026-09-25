# Gameplay spec — how every play behaves

This is the **source of truth for baseball behavior** in Grand Sluggers. When the code and this document disagree, the code is wrong. When this document is silent, file a child issue under the play epic and add the rule here in the same PR.

The bar is *Mario Super Sluggers* (Wii, 2008): a play is decided by **where the ball is, where the runner is, and what the player pressed** — never by a dice roll that a caption then narrates. The user always owns the verb. CPU fills the seat the human did not take, and it fills it with the same rules the human plays by.

Authored body/motion/equipment contract: [baseball-motion-spec.md](baseball-motion-spec.md). Animation changes preserve this document’s input, contact, release and outcome rules.

Companion docs: [systems.md](systems.md) (chemistry, stars, gear, parks), [how-to-play.md](how-to-play.md) (couch buttons), [research-sluggers.md](archive/reference/research-sluggers.md) (the reference teardown), [roadmap.md](roadmap.md) (the order we build this in). Feel numbers stay in `data/feel/`. Rule numbers move to `data/rules/` (section 16).

Status tags used throughout. [Appendix A](archive/gameplay-spec-appendix-a.md), now archived, keeps the original gap-audit rows as the record.
A rule says what the game does, not who built it: a behavior change updates its rule in the same PR, with a bare tag and no issue or PR number and no date ([agent-rails.md](agent-rails.md) §1.2). CI rejects an added spec line that carries one (`tools/spec-provenance.py`). Commit history keeps who did what; the provenance the spec used to carry is in [archive/spec-provenance.md](archive/spec-provenance.md).

| Tag | Meaning |
| --- | --- |
| ✅ | Shipped and behaves as written |
| ⚠️ | Exists but diverges (line reference given) |
| ❌ | Missing, or resolved by a roll / caption instead of geometry |

Every ❌ and ⚠️ was collected in the archived Appendix A with the file and line. Every rule that matters has a scenario id (`S-xx`) in [Appendix B](spec/appendix-b-scenarios.md); the scenario list is the acceptance test for the roadmap epics.

## Sections

The spec is one file per section under [spec/](spec/). Read only the sections your change touches. A section number (§N) names the file that starts with that number.

| § | Section | File |
| --- | --- | --- |
| 0 | Principles — the decision register | [spec/00-decisions.md](spec/00-decisions.md) |
| 1 | Match rules | [spec/01-match-rules.md](spec/01-match-rules.md) |
| 2 | Stats, in numbers | [spec/02-stats.md](spec/02-stats.md) |
| 3 | The at-bat state machine | [spec/03-at-bat.md](spec/03-at-bat.md) |
| 4 | Pitching | [spec/04-pitching.md](spec/04-pitching.md) |
| 5 | Batting | [spec/05-batting.md](spec/05-batting.md) |
| 6 | Ball flight and the field | [spec/06-ball-flight.md](spec/06-ball-flight.md) |
| 7 | Play types — what happens on each | [spec/07-play-types.md](spec/07-play-types.md) |
| 8 | Fielding | [spec/08-fielding.md](spec/08-fielding.md) |
| 9 | Baserunning | [spec/09-baserunning.md](spec/09-baserunning.md) |
| 10 | Outs | [spec/10-outs.md](spec/10-outs.md) |
| 11 | Steals and the catcher | [spec/11-steals.md](spec/11-steals.md) |
| 12 | Chemistry, stars, items in play | [spec/12-chemistry-stars-items.md](spec/12-chemistry-stars-items.md) |
| 13 | Star skills — the two-second rule | [spec/13-star-skills.md](spec/13-star-skills.md) |
| 14 | Parks in play | [spec/14-parks.md](spec/14-parks.md) |
| 15 | Presentation contract per play | [spec/15-presentation.md](spec/15-presentation.md) |
| 16 | Data tables (rails) | [spec/16-data-tables.md](spec/16-data-tables.md) |
| B | Appendix B — Scenario matrix (acceptance) | [spec/appendix-b-scenarios.md](spec/appendix-b-scenarios.md) |

Appendix A, the closed gap audit of `f09cad1`, is archived at [archive/gameplay-spec-appendix-a.md](archive/gameplay-spec-appendix-a.md).

Every line in the spec files is at most 600 characters (`SpecLineLengthTests`), so a reader that truncates long lines still sees each rule whole. Wrap prose at a sentence; move a long table cell into a note under its table.
