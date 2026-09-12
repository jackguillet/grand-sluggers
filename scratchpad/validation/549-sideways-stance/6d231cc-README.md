# Swing matrix after the shin-joint foot landmark (6d231cc)

`6d231cc-swing-matrix-passed.json` is a full seven-captain run driven through
**Grand Sluggers -> Capture Request File** with:

```json
{"shots":["swing-matrix"],"swingCaptains":["rio","vale","zig","brondo","konga","ashlord","fenn"],"hudOff":true,"width":1920,"height":1080}
```

`ok: true`, 56 rows, all seven captains 8/8.

Before the fix rio failed ready and load at both powers with "visible stance
landmarks missing" -- its sneakers extra hides the lShoe/rShoe renderers the
gate measured. Reading sneaker centroids instead put rio at feetAlongPitch
0.877 against the 0.966 threshold; the shin joint puts it at 0.996, alongside
vale's 1.000 on the same shared clip.

Not covered here: human appearance acceptance, which stays a separate gate.
