# SET release input evidence (#630)

`3e9b3ba-passed.json` records the complete At-Bat Input gate in GUI Unity
6000.5.9f1 at revision `3e9b3bab5104fbb182d0c4eb7bf5591db6d4368a`.
All 13 routed `Controls` → `TickSet` / `TickFlight` cases passed.

The boundary cases verify that a South release read in SET does not commit a
swing for either the one-seat CPU pitch transition or the two-seat shared
pitch transition. Separate one-seat and two-seat cases hold South across that
transition and verify that releasing after Flight begins does commit an early
swing, including Player 2's release-frame spray, launch, bunt, and box intent.

This is synthetic Play-mode input coverage, not physical-controller or human
gameplay acceptance.
