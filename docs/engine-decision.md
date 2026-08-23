# Engine decision: Unity 6 (URP)

**Decision:** build Grand Sluggers in **Unity 6** with the **Universal Render Pipeline**, C#, and the new Input System. Target PC (macOS + Windows) first. Consoles only after we would actually ship; that is when Unity Pro becomes required.

**Revisit if:** the vertical slice is blocked by Unity itself (not by us), or we decide the game is 2.5D / orthographic and Godot would finish it faster. Do not revisit because of internet arguments.

The baseball rules, chemistry graph, and content live in `src/` + `data/` as ordinary C# / JSON. Unity is the renderer, animator, audio mixer, and input box — not the source of truth for “is this a strike.”

## What this game actually needs from an engine

| Need | Why it matters here |
| --- | --- |
| Stylized 3D, not photoreal | Cartoon roster, readable silhouettes, toon/ramp shading |
| Many characters, one rig | 24–40 bodies sharing animations; swap meshes and materials |
| Arcade ball physics | Custom integration more than “drop a Rigidbody and pray” |
| Timing-based batting/pitching | Input System, not a physics puzzle |
| Local split / same-screen multiplayer | 1–4 gamepads, CPU fill |
| Parks with hazards | Scene variants, day/night, trigger volumes, VFX |
| VFX for star skills | Trails, decals, particles, short cinematics |
| Content pipeline | JSON roster / chemistry / gear that designers can edit |
| Ship path | Steam/itch first; Switch/PlayStation later maybe |

Photoreal Nanite worlds, open-world streaming, and vehicle physics are **not** on this list. That knocks Unreal out as a default.

## The three options

### Unity 6

**Use it.**

- C# is the language the sim is already in. The same `GrandSluggers.Sim` assembly can sit under `Assets/Plugins` or a local package.
- URP + Shader Graph is enough for a cartoon look (toon ramps, outline, saturated parks). HDRP is the wrong pipeline for this game.
- Animator + Humanoid (or a custom cartoon rig) is the proven way to share a walk/run/swing/throw library across a large roster.
- Input System handles gamepads cleanly; motion can be a later control scheme.
- Asset Store still has the deepest pile of *starter* kits (character tools, VFX, UI, audio). We will not ship store baseball as the game, but we will steal weeks on the vertical slice.
- Local multiplayer, UI Toolkit / uGUI, Timeline for star-skill beats — all boring, which is what we want.
- **License (2026):** Personal is free under **$200k USD** trailing-12-month revenue *and* funding. Splash screen optional on Unity 6. Runtime fee (the 2023 scare) is **cancelled**. Pro is **$2,310/seat/year** and is required above the cap, or to *publish* on closed consoles. PC/mac/Linux/iOS/Android/WebGL are fine on Personal.
- Trust: Unity burned people in 2023. Terms are a yearly cycle now, and a given editor version keeps its terms. Pin an LTS and do not live on whatever they announced last week.

**Costs we accept:** closed source, a heavy editor, a company we do not control. For a 3D character-action sports game those costs are cheaper than rebuilding animation, input, and console toolchains.

### Godot 4.x

**The real alternative.** Not a joke, not a downgrade for this *genre* if we were 2D.

- MIT, $0 at every revenue level, engine source in-tree.
- GDScript is fast to write; C# is available if we want one language with the sim.
- Stylized 3D is credible. Forward+ on Vulkan/Metal. It will not look like Unreal. It does not need to.
- Animation / large-roster pipeline, third-person sports cameras, and “gamepad party game” examples are thinner than Unity’s. We would invent more of that.
- Console export is third-party (W4 Games and friends), not first-party. Fine for an itch/Steam game; a tax if we ever want a Switch SKU.
- Editor is small and pleasant. Iteration on systems would be faster. Iteration on *cinematic 3D characters in unique parks* would not obviously be.

**Pick Godot instead if** we shrink the visual ambition to 2.5D / billboard characters / diorama parks, or if Unity’s terms change again in a way that pins an LTS cannot save.

### Unreal Engine 5

**No, unless the look becomes the product.**

- Best renderer on earth. We do not need it.
- C++ / Blueprints. Our content model is data + C#. Blueprints fight a 40-character roster of tiny rule differences.
- 5% royalty over $1M/product is fine. The problem is weight: editor size, cook times, animation cost, “why is this third-person template a military shooter.”
- Nanite/Lumen shine on film-like scenes. Cartoon baseball wants *control* over the look, not GI.

## Why not “custom engine” or web/Three.js

A baseball game is cameras, animation blending, pads, audio buses, and a pile of authored 3D. Writing that stack is a different product. The interesting code is the *baseball* — keep that in `src/` so we are not trapped, and let an engine do the rest.

## How we will use Unity (constraints)

1. **Sim is not in `Update()`.** `GrandSluggers.Sim` ticks in a fixed baseball clock. Unity presents it.
2. **Content is JSON in `data/`**, imported as ScriptableObjects or read at runtime. Adding a character does not require a C# subclass.
3. **One shared rig** for humanoid-ish characters; non-humanoids (big, tiny, extra limbs) get documented exceptions, not a new pipeline each time.
4. **URP only.** No HDRP samples, no mixing pipelines.
5. **Pin an LTS** (Unity 6.0/6.2 LTS when we start the project) and record it in `unity/README.md`.
6. **Do not check in** `Library/`, huge caches, or `.unitypackage` spam.
7. **Playable slice without the editor.** Milestone 1 renders Harbor Diamond with Raylib (`src/GrandSluggers.Play`) so we can ship a 3-inning game before Unity Hub is installed. Unity remains the engine of record; Raylib is a client, not a second engine decision. The match loop does not import Raylib.
8. **Console** is a later license event, not a day-one requirement.

## Decision log

| Date | Decision |
| --- | --- |
| 2026-08-22 | Unity 6 URP chosen. Sim lives in engine-agnostic C#. Godot is the escape hatch. |
| 2026-08-22 | M1 client is Raylib 3D + the same sim. Unity project is a URP shell. |
