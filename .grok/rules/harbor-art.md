---
description: Harbor park art is authored in Blender MCP, not Unity-only primitives.
globs: tools/blender/harbor_kit.py,unity/Assets/Art/Parks/**,unity/Assets/Scripts/Runtime/HarborKit.cs,src/GrandSluggers.Sim/Harbor*.cs
alwaysApply: false
---

# Harbor art

- Kit meshes live in `tools/blender/harbor_kit.py`. Build and export with **Blender MCP** (`execute_blender_code` / `harbor_kit.py --out`). Blender GUI must have MCP running.
- Walk named DCC stages (`data/agent/dcc-stages.json`, `cli stages`): blocking (diamond / wall ring volumes) → fill (kit slots) → export (`--out`) → still. Harbor skips motion. One-shotting a kit mesh is a patch. Existing flags `--out` and `--clay` still run.
- Server config: project `.grok/config.toml` `[mcp_servers.blender]` and user `~/.grok/config.toml`.
- Fill an existing slot (`wall-ring`, `bag`, `home-plate`, …). Missing file keeps the HarborKit primitive. Do not start a second park pipeline.
- Numbers stay in Sim (`HarborWall`, `ParkDiamond`, `HarborDugout`) and are copied into the Python script. A test should catch drift.
