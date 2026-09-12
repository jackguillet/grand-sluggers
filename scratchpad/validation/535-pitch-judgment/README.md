# Pitch judgment evidence (#535)

`d1c6924-passed.json` records 24 actual Harbor launch/flight endpoint cases in GUI Unity 6000.5.9f1. Every rendered endpoint matched its recorded delivered pitch and visible-zone judgment. This revision predates the combined #534 mechanics integration, which must be rechecked separately.

The fixture injects takes and endpoint clocks. It does not prove controller usability, swing appearance, or a completed human half. The editor log also reported an unrelated UnityEditor.Search startup indexing exception after evidence completion; no gameplay exception was observed in the gate.

`367df05-combined-passed.json` repeats all 24 cases after integrating #533 camera mapping, #534 release controls/batter cursor, and #536 pursuit. All passed in GUI Unity 6000.5.9f1.
