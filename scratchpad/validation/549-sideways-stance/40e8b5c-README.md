# Swing matrix on the shin-joint foot landmark (40e8b5c)

Full seven-captain run driven through **Grand Sluggers -> Capture Request File**
on a clean editor launch against this tree:

```json
{"shots":["swing-matrix"],"swingCaptains":["rio","vale","zig","brondo","konga","ashlord","fenn"],"hudOff":true,"width":1920,"height":1080}
```

`ok: true`, 56 rows, all seven captains 8/8.

rio normal ready: feetAlongPitch 0.996, chest 1, eyes 1.
vale normal ready: 1.000 on the same shared clip.

Three states of the same four rio rows, for the record:

| rio foot landmark | feetAlongPitch | gate |
| --- | --- | --- |
| named shoe only (before any fix) | n/a | "visible stance landmarks missing" |
| sneaker centroids (c75c8a4) | 0.877 | "feet do not align along pitch" |
| shin joint (this tree) | 0.996 | pass |

Not covered here: human appearance acceptance, which stays a separate gate.
