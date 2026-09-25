# 1. Match rules

| Rule | Spec | Status |
| --- | --- | --- |
| Sides | 9 v 9. Positions P, C, 1B, 2B, 3B, SS, LF, CF, RF. | ✅ |
| Innings | 3 (party default), 6, 9. Selected on the match settings page. | ✅ |
| Home / away | Away bats the top. Home bats the bottom. 1P: controller 1 picks HOME or AWAY. | ✅ |
| Walk-off | Bottom of the last inning (or later) ends the moment the home team leads. Bottom is skipped if home leads after the top of the last. | ✅ P3: the take path and the swing path both call `EndIfWalkOff` (S-80) |
| Extra innings | Tied after the last scheduled inning → play full innings until a lead after a complete inning (or a walk-off). Cap at scheduled + 3; a tie at the cap is a tie. | ✅ P3 (`match.extraInningsCap`, S-81) |
| Mercy | Optional (default **on** in Exhibition). 10-run lead after the trailing side has batted in inning 3 (or later) ends the game. Off for 3-inning games. | ✅ P3 (`match.mercy`: `runs` 10, `fromInning` 3, `minScheduledInnings` 6; `Match(mercy:)`; S-82) |
| Designated hitter | None. The pitcher bats. | ✅ |
| Runner selection HUD | A compact label identifies the current runner selection. Control instructions live in How to play, rather than a permanent banner across the field. | ✅ |
| Exhibition setup | See note *Exhibition setup, Spec* below. | ✅ |
| Lineup | See note *Lineup, Spec* below. | ✅ |
| Count | 4 balls = walk. 3 strikes = strikeout. Foul with 2 strikes stays 2 strikes **except a bunt**, which is strike three (§5.8). | ✅ P1 (S-18) |
| Hit by pitch | A pitch that meets the batter's body while the batter does not swing awards first base (§4.6). | ✅ P1 geometry (S-16, S-17); the rubber-walk reach is P1 part b |
| Balk, intentional walk, dropped third strike, check swing, infield fly, appeal plays | Not in the game. | ✅ by omission |
| Ground-rule double | A fair ball that bounces on the field then leaves it over the fence: batter and all runners advance exactly two bases from where they started. | ✅ P2 (`BattedBall.GroundRule`, `PlayOutcome.GroundRuleDouble`; S-59) |
| Ball out of play (foul territory beyond the wall / into the stands) | Foul ball, dead. | ✅ P2 (over a foul wall or the backstop is `SampleEvent.Stands`) |
| Stars | Shared 0–5 per team. §12. | ✅ |
| Ties in geometry | Tie at a bag goes to the runner. | ✅ (`InPlay.ForceOnBag`) |

Notes to the table above:

**Exhibition setup, Spec.** Stadium (night, hazards, player count and P1 home/away) → captains → lineup → positions/order → Match settings. Captain selection has two team cards above the shared portrait row. Each team card shows the highlighted captain's four bars, Bat, Pitch, Field and Run, as §2 derives them (`StatBars`); the same captain shows the same four numbers to either seat. Left/right inspects; South confirms. In 1P, P1 confirms their captain, then the CPU captain. In 1v1 each controller confirms independently; confirmed captains cannot be taken by the other seat. East undoes confirmation before returning. Two-player selection waits for the second controller.
Player 1 sets Stars (ON by default), innings (3/6/9), mercy (ON by default) and CPU skill. Items is unavailable while no source is active. Settings changes clear every human ready state; all human seats must ready again here to start. CPU is always ready. Back preserves order and positions for unchanged rosters. Mercy retains its minimum scheduled-innings rule.

**Lineup, Spec.** Team Setup: South adds the focused pool player, RB fills the acting seat’s team from any focus, West removes a focused roster player. Filling never changes the other seat’s roster. Nine, set in Offense / Defense Setup. Home batting order runs across the top and away across the bottom, with two side-by-side baseball diamonds between them. Navigation only inspects; confirm a source and destination in the same bar or diamond to swap.
Controller focus shows the player card and softly highlights chemistry partners, with no permanent faction frames or chemistry badges. West cancels a pick or withdraws readiness. North marks that seat ready when it has no pending pick; all human seats must be ready to continue to Match settings. Editing clears that seat’s readiness; seat changes clear both. Each team has its own inspection card, cursor and pick, visible simultaneously in 1v1. The inspection card shows the focused player's four derived bars in the same order, captain or role player.
P1 uses gold; P2/CPU uses blue regardless of home/away. A white outline and PICKED label identify a pending swap source; moving the colored cursor only inspects the destination. No timed auto-start. Each seat edits its own team. During SET, players may rearrange their existing nine on defense (§8.1); no roster substitutions except the pitcher swap (§4.7).

**Scoring on the third out.** A run counts if the runner touched home *before* the third out, unless the third out is a force or the batter-runner retired before first. Both timers are already tracked by the live play; the rule is a comparison at `Complete`. ✅ P3 (`Runner.ScoredAt` against the third out's play time; S-78, S-79).
