# Debug protocol — promoted-row narratives

> **Historical.** The full cause and fix each debug-protocol row carried before it was cut to one line once its signature was promoted to a test. The live row is `data/agent/debug-protocol.json`; the test it names is the contract.

## brim-in-plate-lens

**Cause.** CarnivalFront.cs:67 SelectLookY; :104 SelectCamIsTheToy — a look at Y=4.4 / Z=4 is Ashlord's brim. Gameplay cameras that aim at the hat are a framing bug, not a scale bug.

**Fix.** Look at the chest (SelectLookY = 2.6). SelectCamIsTheToy rejects brim and berm. Tune the shot; do not shrink the hat to save a camera.

## boxes-kiss-plate

**Cause.** HomeSet.cs:35 BoxGap; :98 BoxesClearThePlate — an invented or Minkowski gap instead of OBR Diagram 2's 6 inches.

**Fix.** BoxGap is 6 inches; BoxesClearThePlate encodes that gap. Cartoon fat is allowed; wrong topology is not.

## nice-hit-on-whiff

**Cause.** PlayStamp.cs:110-120 — the tell fired on the charge release at MAX, before the bat reached the ball. Spec §5.1–§5.3: MAX is a charge tell; PERFECT / NICE / SOUR are contact tells.

**Fix.** ContactTell reads only the typed contact zone. A miss shows STRIKE through the stamp; the release tell (MAX) never claims a hit.

## cpu-defense-waits-for-batter

**Cause.** LivePlaySystem.Field.cs:35-42 — PlayerFields was true whenever you pitched, so the batting human's stick owned the glove (spec §0.4).

**Fix.** LiveSeats.For(seats, top): seat ownership is (half, side, pads). The batting human never owns a glove. A press from any other controller is dead.

## bat-through-head

**Cause.** SwingPresentation.cs:60-74 HeadClearance; data/art/swing-takes.json charge windup key — barrel direction nearly straight up ran through the head. The DCC validator checked hands on the handle, not bat versus skull.

**Fix.** BatHeadClearance contract. The DCC bake refuses a take that misses it; sim samples both takes and the whole charge-up. Do not pose the bat in C#.

## tall-captain-turntable-crop

**Cause.** StillPose.cs CharCamY=3.4 CharLookY=2.5 — a Rio-sized turntable. Ashlord Height 1.44 and Konga 1.30 leave the frustum.

**Fix.** CharFraming looks at the rig chest height scaled by Silhouette, pulls back by extra height, then fits both head and feet with the same margin. Head dimensions share SwingPresentation landmarks; revision-2 chest height matches rig.json. Do not raise a constant until the tallest fits.

## esc-opens-book-h-calls-time

**Cause.** HowToPlay.cs contents KeyLines and ControlDiagram.KeysCallouts lumped H and Esc. docs/how-to-play.md intro and the face table said Esc opens Call time. Scheme and Controls.HowTo already split them (#348): H is Call time, Esc is the book.

**Fix.** H / Start is Call time. Esc is How to play, including at first pitch. Name each verb in the book, the hardware diagram, and how-to-play.md. Do not change the shortcut.

## bat-behind-head-at-ready

**Cause.** SwingPresentation ready key barrel (0.09, 0.87, −0.49) sat inside the plate camera's head disk (−1.3°). Tuning the plate shot cannot pull it out inside the SET constraints (FeelInfraTests.PlateIsBehindHomeLookingAtThePitcher).

**Fix.** Lean the ready barrel out beside the head — the same language as #623. PlateLoadedBesideDeg is the falsifier. Do not shrink the head. Do not retune the plate camera to hide a load that sits in the skull.

## changeup-reads-as-fastball

**Cause.** PitchFlight.Changeup hangRate 0.72 interpolated Y almost with the fastball (u=0.5 sat 0.16 ft below it); dumpRate 1.55 was a two-piece fade. ChangeupMph 72 was 0.837× the 86 meat, not 0.80×, so air time was ~0.19 s extra instead of ~0.25. Tests allowed hang >= fb - 0.2 and S-06 only sampled plate Y.

**Fix.** changeupMph is 0.80× the meat (68.8). hangRate keeps Y at or above the fastball until hangUntil; dumpRate is the rest of the drop to changeupDropFt and finishes in flight. HUD-off test: air time, crossing Y, hang then dump (not a fade). CHANGE stays a card, not the product tell.

## liner-in-air-is-a-scoop

**Cause.** FlyCatch.Under is the landing ring (§8.3 fly). A liner is intercepted on the live ball above catch.inAirMinY before the bounce (§7.6). Once hitT >= hang or ballY <= 0.75, InAir is false and TouchScoop treats the same meeting as a pickup.

**Fix.** InPosition for a liner is the live ball above inAirMinY before the bounce, or the plant before hang. AutoCatch and South use it. A ball that has touched the dirt is a scoop. Do not award the out as a caption.

## swing-lead-too-late

**Cause.** batting.json window.leadSec 0.10 — square press is plate − 0.10; the late edge of the 9-frame slap window is plate − 0.025, so release and the ball on the plate are the same beat. D13's warp still puts Contact on the ball; the authored lead was too small (#670).

**Fix.** Raise batting.window.leadSec to 0.18. Square is plate − 0.18; late edge plate − 0.105. The window stays 9 / 7; the take still warps Contact onto the plate. Do not revert to press + 0.30 as the judgment.

## rim-catch-is-stand-up

**Cause.** FlyCatch.AutoCatch is stand-up under CatchWindow (radius + windowPadFt 4). CPU never sets CatchDive. LandingMark.RadiusFt floored to MinRadiusFt 16, so the ring was a generous out from well off the plant (#669).

**Fix.** Stand-up / the yellow ring is CatchRadius (StandUpCatchFt). At the rim (past the ring, inside diveReachFt, ballY < diveMaxBallY) AutoDive / PlayerDiveCatch: CatchDive, DiveT, lunge. Past that is a drop. windowPadFt stays dirt scoop slack. Do not retune S-29 by shrinking until one seed passes.

## liner-shares-fly-shot

**Cause.** PlayCamera.cs Shot: Beat.Line grouped with Fly/Homer/Wall onto diamond-fly. A liner is a rope (flight.linerTimeScale 1.0), not a stretched fly; from the fly pull-back a 10-ft rope sits on the dirt like a hopper. diamond-line had been killed as an unused 3/4 (#610).

**Fix.** Beat.Line is diamond-line: same 45° family, between diamond and diamond-fly, blend 0. Never diamond-grounder. Tune the shot; do not caption the class; do not fork 1P vs 2P.

## of-glove-nearest-at-bounce

**Cause.** FieldingPursuit.cs:42-47 Plan: while InAir the route is FlyCatch.ChaseTarget — the landing, not the roll after the hop. A liner/hopper that bounces in RC is still InAir at contact (!Grounder && hitT < hang && ballY > 0.75), so RF wins the bounce. D17 then keeps RF while his route meets a sample, even if CF reaches the wall first. PlayGlove nearest-OF is only the no-path fallback.

**Fix.** Plan uses the plant for a fly, and for a liner this body can take in the air. A liner still up that this body cannot catch uses Rolling — first reachable point on the roll. Choose among LF/CF/RF at the grass hand-off is that route. Do not re-pick per frame (D16).

## blender-no-metal-device-at-startup

**Cause.** Restricted macOS background process sees no Metal device. Blender 5.2.0 GPU startup reaches strstr with a null GPU-name string; the latest crash is a Codex child, not the long-running interactive window.

**Fix.** Use tools/blender-run.sh, which checks Metal availability before Blender starts. Run the intended bake with approved GPU access outside the sandbox. Do not repeatedly retry the restricted launch or terminate the user window.

## unity-imports-dotnet-generated-assemblies

**Cause.** Sim is both a .NET project and Unity local package. Default bin/obj output places generated C# and DLLs inside the Unity import tree. Debug plus Release exposes duplicate attributes.

**Fix.** Directory.Build.props moves all .NET outputs to root .artifacts. Move legacy Sim bin/obj to an external backup before refreshing Unity. Quit validation editors normally and preserve scene recovery data.

## unity-recovery-after-delivery-termination

**Cause.** tools/local-player.py previously called process.terminate in its finally block after a successful build; agent SIGTERM cleanup similarly bypassed a normal editor quit. The prompt alone does not establish a Unity crash.

**Fix.** Owned build editors write GS_BUILD_EVIDENCE outside Temp and opt into GS_BUILD_QUIT_WHEN_DONE, then PlayerBuildGate requests EditorApplication.Exit. The supervisor waits without terminating; preserve recovery scenes and quit stuck editors normally.

## title-captain-arms-up-x

**Cause.** ActorDirector.PlaceSelectRoster placed CurrentPick().Yours at FeaturedTitleZ and played Cheer. data/art/clips.json cheer (#628) is arms-up. CarnivalFront.TitlePoster required a toy in the title look.

**Fix.** TitleShowsCaptain is false. Title is wordmark + dirt + UI. No featured hero. Captains wait for select. Do not pose cheer in C#; do not shrink the body to hide the X.

## title-wordmark-mirrored

**Cause.** LogoToy.Show used Quaternion.LookRotation(camera − logo). Unity LookRotation then puts local +X on the camera's left. Glyphs sat at local +Z, so the letters faced the lens but read mirrored.

**Fix.** TitleLogoForward looks into the park with the title camera. Ink and glyphs sit on local −Z (CarnivalFront.TitleLogoGlyphZ). Do not LookRotation(toCam). Do not flip a texture to hide the facing.

## protocol-promotion-assumes-csharp

**Cause.** The promotion reference check searched only C# test sources although the protocol permits tests and validators. Editor startup rails introduced Python tests and a shell entry point.

**Fix.** Resolve C# and Python Type.Method references and repository-relative tools shell entry points. Reject missing methods, types, files, and prose instead of clearing the promotion.

## select-bobs-through-dirt

**Cause.** ActorDirector.PlaceSelectRoster played Cheer (home pick) and StealLead (away pick) on CarnivalFront.CaptainSpot at Y=0. Cheer #628 bobs root lift 0→0.35; stealLead lift −0.35. Look.md had Highlight + cheer.

**Fix.** SelectPose is Idle. Featured is the step-forward + highlight. SelectDirtY is the plant. SelectExtraMinY ≥ dirt for every captain extra. Cheer stays a field take. Do not shrink a body or per-captain Y-hack.

## harbor-bags-paint-brown

**Cause.** HarborKit.PaintKit mapped unknown FBX material names to wood (0.42, 0.26, 0.12) or dirt. Unity BasedOnTextureName drops Blender chalk/navy; DressBag still called paint: true. Primitive fallback was already Colors.Chalk.

**Fix.** HarborKitPaint.For(slot, material). Chalk meshes (bag, home-plate) paint chalk, navy piping stays navy. Unknown names never fall through to wood/dirt. PrimitiveBag is chalk. Do not shrink the bag.

## throw-destination-laser

**Cause.** SpecialFx.cs throw-trail-good / throw-trail-bad: a gold LineRenderer from release to the bag for the whole throw. Spec §15: the camera follows the ball; a predicted throw laser is not a Super Sluggers tell.

**Fix.** SpecialFx builds no LineRenderer or TrailRenderer and never loads throw-slot prefabs. Remove ability beams too: Laser and lick-catch previously bypassed ThrowTrail.DestinationLine and connected fielders to the ball. Chemistry tints only the moving ball trail (ThrowTrail.BallRgb / BallView.SetTrailColor after Place). Keep the catalog slots as metadata.

## extras-are-geometry-junk

**Cause.** data/art/skins.json attached extras: Brondo cube-chest+brick-jaw, Konga snout+belly, Ashlord horns+cape+ember-eyes. extras.fbx from #628 is larger; a foot-parented extra reads as a circle on the dirt; Ashlord's cape is the orange back plate.

**Fix.** Skins list no extras. Identity is palette + Silhouette.Proportions. extras.json slots stay. Do not shrink Ashlord to hide the cape. Do not bring caps back as geometry (#557).

## hold-ball-scales-up

**Cause.** HeroActor.TickVisual: scale × 1.45 if _grow, × 1.18 if _lit, × 1.12 if _hint. ActorDirector SetGrow on the grow ability when highlighted and lights the YOU / ball body. Holding the ball is enough to look like a size-up.

**Fix.** BodyScale: rest unless GrowOn (grow ability, play glove, not holding). YOU / hint / hold stay rest. Grow is the field verb; do not scale the toy to sell the glove.

## one-stamp-at-time

**Cause.** PlayStamp.Label(PlayEvent) at Time (Complete). LiveTell only SAFE and ERROR. Catch, tag, and score wait for the final PlayKind. HudView.PlayStamp sits at screen center (0.50, 0.40).

**Fix.** One tell per live event when it happens, at a named BroadcastHud anchor (glove, bag, plate, dirt). Retire raises StampOut; a runner who scores raises StampScore. ShowsAtTime skips FlyOut / GroundOut / CaughtStealing / live ERROR. Do not overlay a center card.

## catch-re-read-as-drop

**Cause.** LivePlaySystem.FlyStateNow reads HoldsBall (Caught || Buddy), which is possession of ANY ball, and pairs it with the latched _call. A relay that sails (SailThrow), a lob nobody covers (DropThrowAtBag), or an item that foils the holder (Foil) all clear Caught, so the batted ball reads Dropped; the next glove re-enters Caught. That second Caught edge re-fires UpdateFly retouch block, which calls MarkLeftEarly() on a body that legally tagged; TryForce then converts the LeftEarly flag into a force back at the bag he came from. The force itself is NOT revived - Retire clears it at the catch (LivePlaySystem.cs:688, Forces.AfterOutAt) - so the retouch edge is the whole mechanism.

**Fix.** UpdateFly: Caught is terminal for the play. The batted ball has one fate (spec §9.5, §10.5) — losing the ball later is possession, not the batted ball coming down. Fly is the one shared read of that fate (LivePlaySnapshot.Fly), so it is latched at the state rather than at one consumer. A narrower gate on the retouch edge alone also removes the harm; it was not taken because it leaves every other reader seeing a caught ball as dropped.

## delivery-build-editor-idles-open

**Cause.** PlayerBuildGate queued EditorApplication.Exit on EditorApplication.delayCall, which runs on a later editor update. A background delivery editor can stop updating after the build, so the exit never ran (delivery of eae4aff37a, 2026-09-18). A probe with the editor hidden at the end of the build reproduced it. tell application "Unity" to activate does not wake it; a quit Apple Event does.

**Fix.** After a good build the gate calls EditorApplication.Exit(0) in the same update; the shutdown starts on the next log line. The supervisor backs it up (an older revision's gate still idles): 20 s after good evidence it sends one quit Apple Event (NSRunningApplication terminate) to the exact PID it launched and waits up to 120 s. No signals. Failed and unfinished builds stay open for inspection.

## fumble-holds-a-bat

**Cause.** ActorDirector mapped LivePlaySystem.Bobbling to Motion.Verb.Miss. Motion.UsesBattingHand(Miss) is true, so HeroActor turned the bat on, the glove off and played the batting hand's miss take on a fielder. Found in the #718-#721 Unity pass audit, not a sitting.

**Fix.** A body's debt picks its take in one place, FielderTells.Verb, per body (the ring can leave it): the fumbler's stun on StunPos plays the spin take, never a batting verb (3e retired the whole-tick fumble). Poses stay the takes'; the director only chooses.

## tutorial-manual-credit-after-assisted-chase

**Cause.** TutorialSession kept the first human movement in _manualGloves for the entire attempt and did not distinguish later assisted route movement from a player-positioned glove waiting still.

**Fix.** Observe the actual assisted ChaseGlove step separately from passive brake/coast. Clear manual pickup ownership when that route moves the glove after the last human steering step; retaking it can restore ownership. Advance T-F01's lesson revision.

## tutorial-neutral-west-jump-uncredited

**Cause.** TutorialSession required PursuitManual on the West frame even though jump ownership is a button action that the live sim accepts with a neutral stick.

**Fix.** Record a human West press only when the live jump arm/takeoff accepts it (or the same tick completes a Jump catch), and pair that selected glove with the completed geometric Jump out. Manual steering is not required on the press.

## tutorial-hides-live-readouts

**Cause.** HudView.Draw returned from TrainingPlay before Play could draw the pitcher/batter cards, scorebug and bases.

**Fix.** Draw the ordinary match readouts during training and put coaching in BroadcastHud.TutorialCoach, clear of the score and player cards. Tutorial choices remain readable through the same live HUD used by Exhibition.

## tutorial-loose-chase-latch

**Cause.** TutorialSession.Advanced latched _looseHumanChase without checking who finished the pickup after later assistance.

**Fix.** Require actual human steering and pursuit ownership at recovery and exclude assistance since the last manual command. Ordinary C80 bobbles use the same ownership evidence plus real Bobble, LooseBall and Possession trace marks.

## tutorial-attempt-menu-interruption

**Cause.** The presentation treated every feedback phase as a blocking menu; retry skipped the briefing only on successful attempts and only after confirmation.

**Fix.** Save progress then rebuild and begin the same lesson on the next presentation tick below 3/3, for both gameplay and guided lessons. Skip play input on the reset frame and retain failed-attempt coaching without a modal.

## unity-only-positional-sim-call

**Cause.** The solution does not include unity/, so nothing in the .NET build sees a unity/ call site. Removing a parameter from a sim method silently re-aims every argument after it at a unity/ caller that passed them positionally — StillCapture.cs:780 kept PitchFlight.Point(..., 0, false, 0, from) when the changeup bool went away (#810).

**Fix.** When a sim signature loses or gains a parameter, grep unity/ for the method name and read the call sites, not only the named-argument spellings, and run tools/unity-compile.sh before opening the PR. A positional call in unity/ is only compiled by that gate.

## stored-double-pins-the-platform

**Cause.** Those libm functions are not correctly rounded and differ by about one ULP between macOS libm and glibc; IEEE only pins +, -, *, / and sqrt. PitchFlight.BreakShiftFt calls Math.Sin(u * PI) and the heatball/prismball/charmball wobbles call Math.Sin(u * hz), so the golden's X under break stored a macOS bit pattern (#810). The same class of drift is why the compact report is compared field by field (#736).

**Fix.** Store only the parts a golden can own: values reached by +, -, *, / and sqrt alone. For a value a libm call touches, store the libm-free part plus the inputs the change under test actually decides, and recompose the rest on the running platform in the production operation order, then assert bit equality against the real call. Exactness survives and the platform drops out, because both sides call the same libm.

## role-test-measures-the-wrong-statistic

**Cause.** The statistic was a proxy, and the flattest row wins any proxy about staying near the fastball by never doing anything. The slider's whole role is a small drop, so the last u at which the height gap stays under a tolerance is largest for it, not for the sinker whose role is to ride and then dip (#818, S-109).

**Fix.** State a role as two numbers that cannot both be won by inaction - how far it strays before the window (the ride) and how much further it goes after it (the dip) - and before trusting a single-threshold proxy, ask which row would win it by doing nothing. Change the measure, not the proposed number: the number is the thing under judgment.

## star-shape-moves-the-crossing

**Cause.** PitchFlight.Point applies the Star Pitch's own shape (heat/prism/charm/phony wobble in X, caskball's rise in Y) after the family, the sweep and the stick, so a Star crossing is not the family's crossing. Any test that samples CpuPitch, AutoPlay or a captain's delivery will eventually draw one: Match.CpuPitch rolls Star on a captain with a star available (#823, S-115).

**Fix.** Say which pitches the claim is about. A statement about the family's own geometry exempts pitch.Star and asserts that a Star was in fact drawn, so the exemption is not silently covering an empty set; a statement about the rubber solve does not need the exemption, because the wobble is in the rubber-0 probe too and cancels. Do not widen the tolerance to swallow a star.

## float-const-widens-when-it-moves-into-data

**Cause.** (double)4.2f is 4.19999980926514, not 4.2. A rules table stores doubles, so authoring the same decimal in JSON widens the number and moves the geometry that stood on it. HarborWall.HipHeight was a float const and FieldBounds.FoulWallHeightFt read it as a double, so every foul segment of the clip polygon has always stood at the narrowed value (#826).

**Fix.** Read the C# type before moving a constant into data/rules. If it is float and a double consumer reads it, author the decimal in the table and narrow exactly once, in a named member at the point the geometry stands on it (ParkBoundary.RailTopFt), with a test pinning the narrowed value. Widening then becomes a decision someone owns with parity evidence behind it, instead of a refactor's 2e-7. A float const with only float consumers stays float.

## book-copy-overflows-the-measured-band

**Cause.** Every couch page is measured, not counted. BookletLayout.BestFlow reflows a paragraph page into two columns when one does not fit, so an extra paragraph or ~30 more characters can push one column past the band; ControlDiagram.PageCallouts splits the callout list into three fixed pages, so adding a callout can put a fourth two-action card on a page and the stack no longer fits even at BookLineMinPt. HowToPlay.KidLineMax and RoleTables.MaxRows pass while the pixels do not, and the smaller supported window is the one that binds (#825).

**Fix.** Fit the copy to the band, do not widen the band or drop the assertion. Measure before writing: ApproxWidth is length * pt * 0.55 and a block is ceil(width/columnW) lines of pt * 1.2, so a paragraph page's budget is a character count you can compute. Replace a row rather than adding one (RoleTables.MaxRows is a page count, not the limit that bites), put a new hardware callout on a lozenge that already exists rather than adding a card, and move copy that will not fit to the spread that owns that verb instead of deleting the rule.

## derived-table-copy-drops-a-section

**Cause.** RulesTable is a class with init-only properties, so a copy cannot be a `with` expression and every section is named in the object initializer (Rules.cs AtLevel / AtPark). #826 added `boundary` to the table and to AtLevel while #827's AtPark was open; the rebase merged cleanly and left AtPark one section short.

**Fix.** Walk the sections by reflection in a test rather than trusting the initializer: every section of a derived table is ReferenceEquals the source's except the one the derivation owns. A reference check, not a value check - the dropped section's defaults print identically to the loaded ones, so comparing values passes. The same row also pins the section count, so a section that appears in neither branch's copy is a failure and not a silent pass.

## trial-cohort-reads-the-process-wide-table

**Cause.** AtBatResolver.PitchInZone, StrikeZoneGeometry.Contains(PitchCommand, string?) and the PitchFlight.Point overload behind them take no RulesTable, so the family is resolved against Rules.Default — the process-wide table — instead of against Match.Rules. Match.AutoPlay (Match.cs:1550) is the in-zone call on the auto-play path. With GRAND_SLUGGERS_TRIAL set the process-wide table IS the overlay, so a CLI cohort is correct and only an in-process overlay catalog fails; on the shipped root the two tables are the same object, so nothing has ever failed. Same family as derived-table-copy-drops-a-section and the Diamond reads: a static that answers from the process root while the caller believes it asked the match.

**Fix.** REPAIRED by #855: AtBatResolver.PitchInZone, StrikeZoneGeometry.Contains(PitchCommand, string?, RulesTable?) and PitchFlight.Point take an optional RulesTable (defaulting to today's resolution) and Match.AutoPlay passes Match.Rules, so an overlay catalog's families fly on the auto-play path (S-127 holds it: a catalog whose curveball drops further than the process's is read by its own table at every hop, and plays a game in process; until #887 it ran the S-29 cohort on the stick switch's off path). If this signature fires again, a new static read has appeared between the match and the flight: find the hop that is handed no table and thread the caller's table through it (an optional RulesTable defaulting to the process-wide one keeps every other caller identical). Do not conclude the overlay is broken, do not author the missing row, and do not set GRAND_SLUGGERS_TRIAL to paper over it in a test. Still process-wide, reported not repaired because their owners are other children: SetTells.InZone, Training (the practice pitch), the Raylib debug client (src/GrandSluggers.Play/Game.cs) and Unity AtBatDirector's CpuSwing in-zone read; each matches on the shipped root and under GRAND_SLUGGERS_TRIAL, where the process-wide table is the overlay.

## gui-editor-focus-fight

**Cause.** Sessions share one Mac and one GUI editor (Personal Unity cannot -batchmode), and Play advances only in the frontmost editor, so a capture loop fronted its editor every 10 s with nothing telling it the Mac was busy (2026-09-22). Fronting by name (tell application "Unity" to activate) starts a fresh projectless editor when none runs and fronts either one when two run; still-gate.sh clicked the menu of whichever process was named Unity, not the editor on its own worktree. local-player.py SIGTERMed every delivered player without saying what it closed.

**Fix.** One machine-wide GUI Unity lock (tools/unity_gui.py, ~/Library/Application Support/Grand Sluggers/unity-gui.lock). Take it before launching, fronting or clicking an editor and before delivering: `acquire --pid $$ --purpose ...` with a release in an EXIT trap, or unity_gui.hold in Python. A live holder refuses the next session by name (exit 75); a dead, zombie or reused holder PID leaves a stale lock the next acquire clears. Front, click and quit an editor by PID through unity_gui.py front / menu / quit, which only the holder may call. A capture that fronts an editor refuses while a delivered window is open until Jack says the machine is free (--player-open-ok). local-player.py names the revision and trial of the window it would close and stops before building unless --replace. Never activate or pkill Unity by name.

## hazard-on-a-base-path

**Cause.** The placement rule (ContentValidation.cs HazardPlacement, #862) measures every hazard's own disc against the lanes (ParkDiamond.PathWidth), the pads (BagPadR), the mound (MoundR) and the plate area (HomePackedR) of the root's own infield.json. A park that authors a disc there is refused; so is a root that moves the bags without its hazards, because the shipped parks then stand on the smaller diamond (an 80-ft infield under the shipped Canopy puts its (6, 102) barrel on the first-second lane and second base's pad). Before #862 no validator checked a hazard's place, and eight status volumes sat on a lane or a pad unnoticed, because no body touches a hazard yet.

**Fix.** Do not weaken the rule, add a per-park exception, count or drop reachPadFt, or move the hazard yourself: FD-19 makes a refused hazard a finding for Jack, and only a decision like FD-19-R1 (outward along its own bearing, radius kept, the trial twin the migration of the moved row) moves one. A trial that moves the bags carries the parks whose hazards stand near them, migrated by the accepted zone rule, as trials/c80 does; a test fixture that only needs an infield table changes fields no hazard stands on (baselineFt, innerHalfFt, backArcFt).

## sim-uses-a-bcl-api-unity-lacks

**Cause.** GrandSluggers.Sim is also a Unity local package. The solution builds it for net10.0, but Unity compiles the same sources against netstandard 2.1 (tools/unity-compile.sh: NetStandard/ref/2.1.0 plus the BCL extensions), so a framework helper added after netstandard 2.1 - ArgumentNullException.ThrowIfNull, and its kind - exists for dotnet and not for the game. Nothing in the .NET build or the tests can see the difference (#856, GroundZones.cs WallMaterial.OfSegment).

**Fix.** Write the netstandard 2.1 form in src/GrandSluggers.Sim (if (x is null) throw new ArgumentNullException(nameof(x))) and run tools/unity-compile.sh before opening any PR that adds framework calls to the sim, not only one that changes a signature. The language version is not the limit (the gate compiles with /langversion:latest); the framework surface is.

## dress-backstop-inside-kit

**Cause.** F6-a (#859) moved the diamond, the foul rail and the backstop into the one field kit, which draws them from the geometry owner at every park, but each park's ParkView dress method still built its pre-kit backstop (a back panel at z -24, two side panels at x +-20 beside the batter and, at Crystal, Rooftop and Ember, a ledge along z -36 on the kit's own backstop line) and its pre-kit dugout (a bench at (+-42, 22), with an awning over it at Funfair, Rooftop and Canopy) in foul ground across the span Harbor's dugouts fill. Nothing tied a dress piece's place to the kit's geometry, so only Harbor, whose dress never drew these, looked right.

**Fix.** REPAIRED by #881: the pieces are gone, and two source rows read every spot ParkView places a piece at (a builder handed a literal spot, Look.Prim under the park root, a group root that parents primitives, a literal spot list, a Hazard written in the view). FieldKitSourceTests.NoDressPieceStandsInsideTheKitsBackstop refuses one inside the kit's backstop wall: the radius of the drawn loop's wrap around the plate (HarborWall.Loop, from ParkBoundary) plus half the wall FieldKit draws. FieldKitSourceTests.NoDressPieceStandsInADugout refuses one in a dugout: HarborDugout.AlongHome to AlongBag along either foul line, from the chalk to the pit's back wall (the rail's offset plus 2 x HalfDeep). The dress stands beside the kit, never in it. Do not move a refused piece to just past the line, thin it, or add it to an allowlist: the kit owns the backstop, Harbor's dugouts are HarborKit dress, and a park's own backstop or dugout look is a kit slot (F6-b), not a dress piece. When F6-d retires the dress methods into greybox builders, the rails move with them.

## drawn-top-recomputes-the-flight

**Cause.** The drawing chose its heights by its own rule instead of reading the flight's. HarborWall.Height called a vertex outfield if it lay within half a degree of the foul line, which also took in the first one or two rail vertices past each pole where the rail flares into the line, so the last 31-34 ft of rail (11-12 ft on trials/c80) was drawn at full fence height before a smoothstep ramp from 95 ft out; and FieldKit.Wall drew each span from its two end vertices' heights, so a vertex shared by a fence span and a rail span turned the step at it into a ramp over the rail span (#873).

**Fix.** Read a drawn top from the flight segment the drawn span lies on (HarborWall.FlightSpan / SpanTops, from FieldBounds.Of(park), at each end of the span), take a vertex's top as the tallest segment that meets there, and draw each span from its own tops rather than from its end vertices. Classify a vertex as fence by the flight segment's kind, not by an angle with slack. Check the tops span by span, not only the positions vertex by vertex.

## lineup-navigation-edits-team

**Cause.** LineupScreens.StickDefense used navigation to rotate the batting order and nudge gloves, while TeamSheet placed a fixed-width card over the right diamond.

**Fix.** Use explicit source/destination selection per seat. Navigation and hover only inspect. One shared LineupLayout reserves non-overlapping batting lists, diamonds and a card area and supplies pointer targets; completed swaps alone credit the guided tutorial.

## steal-restarts-at-catcher-possession

**Cause.** The old armed-steal transition reconstructed a head start from the full pitch duration and reset runner orders when beginning the live play.

**Fix.** Advance the same runner bodies through PitchSetupSystem, rebase their event clocks at possession, and preserve their position, direction and hold in Runner.BeginPlay. Never derive runner position from a pitch animation or an armed timestamp. A dead foul explicitly restores the origin bag.

## race-camera-retreats-behind-home-board

**Cause.** Whole-race bounds overrode the authored camera position by dollying backward, so changing shot height or viewport margin could not guarantee an opening position inside the home scoreboard.

**Fix.** Live steals use the ordinary fielding camera from catcher possession. The pre-pitch inset keeps its authored eye and fits subjects with its lens; fitting never retreats behind the board.

## disabled-match-resource-still-on-hud

**Cause.** HudView.Scorebug drew the meters and unavailable-special tell without reading Match.StarsEnabled. An empty pool alone cannot distinguish a disabled resource from an enabled but depleted one.

**Fix.** Carry the match setting in BroadcastHud.Scorebug and use it for both team meters and unavailable feedback. Enabled matches retain empty meters, independent of captain, half or seat count.

## hit-label-selects-ball-physics

**Cause.** Launch was clamped to +3 degrees, ordinary sour contact selected a separate launch band, FlightRules selected a class clock, and catches used class/landing XZ without an upper reach bound.

**Fix.** Use signed continuous ordinary contact, one flight clock, incoming-impact surface response and live 3D catch reach. First surface contact decides catch versus pickup; descriptions do not.

## pitcher-read-does-not-block-possession

**Cause.** LivePlaySystem.GloveMayTake did not read pitcher readiness; CPU dive commitment added a large radial lunge and catch envelope.

**Fix.** One pitcher recovery readiness drives planning, movement and possession, with ordinary bunt response. Only human East initiates a lateral dive; catch and pickup use the reduced shared reach.

## contact-effect-blanks-live-hud

**Cause.** BroadcastHud.MutePlay tied every play overlay to spectacle, smash and freeze timers; HudView.Draw returned before drawing any match information.

**Fix.** BroadcastHud.Mode follows the plate/live phase with a compact runner diamond and outs during play. Effect clocks do not control HUD visibility. Only explicit capture/debug mute hides play overlays.

## star-runner-input-collision

**Cause.** Role and phase-dependent hardware meanings shared the Star bumper and the batter movement stick with runner orders.

**Fix.** ControllerLayout keeps LT Star, RT baseball and LB/RB runner orders independent across SET, flight and contact. Right-stick selection is separate from left-stick movement; automatic catches never create a throw request.

## captain-setup-overlap

**Cause.** Independent HudView.Select pixel rectangles and SetupSheet.CaptainFocus rows were drawn over the same selection screen.

**Fix.** CaptainSheet owns a scaled board with two team panels, a separate portrait row and footer; match options live on stadium setup. The layout test checks all panel and portrait pairs, and the GUI gate captures every captain.

## buddy-jump-without-two-arrivals

**Cause.** TickBuddyPartner interpolated from the starting position to the wall by hang time; human buddy reach checked only the selected glove against a 26-ft plant radius.

**Fix.** Plan reachable routes for both good-chem outfielders, step the partner through ordinary pursuit, and require both live bodies within 4 ft of the plant and live ball XZ inside the window. No snap grants arrival.

## infielder-runs-to-the-rest-point

**Cause.** FieldingPursuit.Rolling fell back to the last legal sample, the ball's resting place 250-270 ft out, when no pickup was reachable. A slowing roll also gives that point the smallest miss.

**Fix.** An unreachable infield route plays the ball where it passes closest to the body while the ball is still coming (FieldingPursuit.Crossing, cutOff). Outfielders keep chasing the roll. A tutorial fixture that relied on the deep run needs a new ball.

## catch-on-the-contact-frame

**Cause.** The batted ball starts at the plate, inside the catcher's standing reach (C stands 15 ft back, the catch radius is 10+ ft); nothing kept the glove off the ball until it had left the bat.

**Fix.** Off the bat (FlyCatch.OffTheBat, fielding.catch.offTheBatFt): LivePlaySystem.GloveMayTake refuses the flying batted ball until it has been 10 ft from the contact point or reached the ground. Straight into the mitt is a foul tip, not a catch.

## runner-stranded-short-of-an-occupied-bag

**Cause.** RunnerSystem.Tick applied the no-pass gap (running.bagSec.noPassFt) to the runner ahead's progress even when that runner stood on the next bag, and the spec had no rule for two runners on one bag.

**Fix.** The gap applies between bags only; a body standing on a bag can be run up to. RunnerSystem.EntitledOn / Protects / Unentitled decide who the bag protects (OBR 5.06(a)(2): the lead unless forced off it). Every tag reads Protects, Time waits while two share a bag, the CPU runner gives the bag back when the bag behind is free, and a CPU glove runs at the other body.

## force-runs-down-the-chain

**Cause.** The runner tick and RunnerAi read a runner as forced whenever Runner.Forced and the force at his next bag stood, so once he reached the bag he was forced to, the next runner's force carried him on.

**Fix.** RunnerSystem.ForcedOff: forced only while still on the start bag, toward FromBag + 1, and not after a catch. The tick, RunnerAi, the rundown read and the stray-body read all use it.
