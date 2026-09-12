# Fenn character gate failure at 49212a698d

These are the unedited 1920×1080 outputs from the combined standalone preview at
`49212a698ddcd5deccd0a932a1087c62846b7261`, captured with Unity 6000.5.9f1 by
the existing `StillCapture` file-drop path on 2026-09-10.

Both images fail the visual contract in `docs/screenshot-gate.md`: the rest body
is collapsed at ground level, and the posed body separates into displaced masses
instead of showing a recognizable captain with one limb flexed. A successful
`gs-still-done.json` only established that the PNG files were written.

Tracked by #528. These files are failure evidence and must not be cited as a
human look-gate pass.
