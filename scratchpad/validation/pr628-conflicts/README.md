# PR 628 conflict resolution

Merged main `5f727d1` into `2a82ce9` (PR 628). This retains the revision-2 rig and all current-main gameplay changes.

The ready key combines main's #560 beside-head barrel direction and grip with the PR's 0.65-unit vertical offset. Regenerated the C# contract from JSON and rebaked normal/charged swings for both hands, including byte-identical Resources copies. Contact and finish markers remain unchanged.

Main's turntable landmarks described the old body. The turntable now shares head dimensions with the rig's swing contract, uses the revision-2 torso origin, and checks both feet and head against the existing 4% margin. The projection rejection fixture now hides a barrel behind the current head; the old large-head fixture is no longer a valid negative example.

Validation:

- Blender frame-by-frame handedness, grip, socket and head-clearance checks passed.
- Full .NET suite in Release: **982/982 passed** (46 seconds). Also 45 focused rig, swing, still-harness and art-catalog tests passed in Debug.
- The diagnostic Debug run was interrupted by a 90-second inactivity limit during S-29 (fifty CPU games); the complete Release run passed S-29 without that timeout.
- CLI art and CLI match passed. Unity C# compile passed.
- GUI Unity rendered matrix: **140/140 passed**, all seven captains, both hands, normal/charged and five beats. [Measurements](swing-matrix.json).
- Human look/gameplay gate remains pending. No standalone was rebuilt or restarted.

Stills:

- [DCC normal](../../stills/dcc-swing-slap.png), [DCC charged](../../stills/dcc-swing-charge.png)
- [Rio rest](../../stills/char-rio-rest.png), [Rio contact pose](../../stills/char-rio-pose.png)
- [Right ready](../../stills/swing-rio-r-normal-ready.png), [left ready](../../stills/swing-rio-l-normal-ready.png), [right contact](../../stills/swing-rio-r-normal-contact.png)

Read-only look critic: the body and ready barrel survive import, with the full toy inside the turntable. Left ready has weaker grip visibility. Contact appears upright with hands near the pelvis, relating to the screenshot gate's “a stiff upright body” rejection. Ready shoes appear partly sunk into dirt; static sheets cannot establish motion rhythm. These observations are not attributed to the conflict resolution and remain part of the draft PR's human review. No human gate was passed.

The Unity capture completed successfully; its log also contains AnimationEvent receiver warnings and an editor UI ArgumentOutOfRangeException. The rendered matrix is a geometry check, not a claim that the editor log or gameplay acceptance is clean.

## Follow-up after PR 631 merged

Merged main `0b1340b`. Kept PR 628's coroutine runner and four live pitch-motion cases; registered all 13 corrected SET/Flight cases from PR 631 inside that runner. GUI Unity completed **17/17 cases**, including both hands and charge levels. [Input/motion evidence](at-bat-input-after-631.json). Narrow Unity compile passed.
