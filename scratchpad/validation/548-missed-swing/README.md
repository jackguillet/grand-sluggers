# Missed-swing outcome evidence

The retained report `826b678-passed.json` came from Unity 6000.5.9f1 at
integration revision `826b678dba4235b8a3b73a94cbb950f8856b5ac0`. Its top-level
result is `ok: true` for all 21 cases.

The gate exercised the actual `TickFlight` → `Resolve` → Result and
`DrawActors` path. It covered normal and MAX late whiffs, swinging strikeouts,
and early whiffs for Rio (right-handed shared rig), Zig (left-handed shared
rig), and Fenn (Generic package), plus called strike three for each captain.
Late takes reached the 0.30-second contact and 0.50-second follow-through keys
in Result. Early takes reached both keys in Flight and did not restart in
Result. Called strikeouts never started the swing action.

The nine PNGs are original camera renders copied byte-for-byte from the gate:

- Rio normal late whiff: start, Result contact, and Result follow-through.
- Zig MAX swinging strikeout: start, Result contact, and Result follow-through.
- Fenn MAX late whiff: start, Result contact, and Result follow-through.

These selected frames retain the normal/MAX, right/left-handed, shared/Generic,
ordinary miss/strikeout coverage without committing all 57 gate renders. They
show that the three sampled beats are distinct. They do not pass the stance,
character-look, controller-feel, full-half, or standalone human gates.
