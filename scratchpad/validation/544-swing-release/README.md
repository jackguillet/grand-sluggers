# Launch-frame release (#544)

At `df4721c`, all eight actual Controls → SET/Flight checks pass in GUI Unity 6000.5.9f1. New CPU-launch and simultaneous two-seat release cases preserve the release-frame processed spray, bunt, launch and box intent exactly once.

`7982b82-failed.json` exposed an incorrect fixture comparison against raw gamepad numbers. Unity applies a stick processor and Controls applies a deadzone; the final fixture compares against the exact processed Controls input frame and requires meaningful nonzero axes. The gameplay release was retained in the failed run too.

Virtual-device automation does not pass physical controller or human gameplay acceptance.
