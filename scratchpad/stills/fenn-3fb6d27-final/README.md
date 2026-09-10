# Fenn character gate candidate at 3fb6d27

These are the unedited 1920×1080 outputs from the #528 standalone preview at
`3fb6d27425963e8e6ee7c6ed97bf49fd605d80dc`, captured with Unity 6000.5.9f1 by
the existing `StillCapture` file-drop path at `2026-09-10T21:36:56Z`.

- Rest SHA-256: `02b80eccdc4874d9a887f7f7165fe80ff0b4eceb104448c6d224c8255f8b2339`
- Pose SHA-256: `a54a5e7b7a827315c4e7108fe0977267e58cb5f971e1bb6a7a76f0e3355c69d1`

Compared with the preserved `49212a698d` failure, Fenn is upright with grounded
feet in both images. The posed arm moves while the torso and shell retain their
volume. The view still reads primarily as the back of the shell; head and face
readability remains an open #188 human look gate.

`gs-528-negative-evidence.json` is a deliberate negative validation run at
`77ef1557da936686efb558f46b4305ef311e48e1`. For that run only, the committed
fixed body and idle FBXs were replaced in the working tree with the old
`49212a698d` author and player files. Unity rejected both at 0 seconds because
sampled height and center collapsed relative to the rest skin. The fixed files
were restored afterward; the negative run is not evidence for a clean revision.

These files establish the absence of the #528 ground-collapse regression. They
are candidate evidence for human review and do not claim a human look-gate pass.
