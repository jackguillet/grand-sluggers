# Common hitting bat and authored swing (#541 / #503 / #545)

At `994b202`, GUI Unity 6000.5.9f1 rendered and verified all 56 combinations of seven captains, normal/MAX charge, and ready/load/contact/follow-through. `994b202-passed.json` preserves every raw result. Selected original PNGs are retained here; the complete local capture is `/private/tmp/gs-541-final-all`.

The checks verify the exact capture-frame pose, visible common `bat-wood`, the actual imported mesh/submesh bounds, model grip at its socket, posed hand volumes against the physical handle, shared-rig authored direction relationships, and barrel-segment intersection with the plate/zone at Contact. Fenn uses his own Generic clips; his physical hand measurements are retained without substituting shared-rig anatomy or pose targets.

The common mesh preserves its authored origin: handle Y [-1,-0.1] radius 0.08, barrel Y [-0.15,1.25] radius 0.12, knob Y [-1.14,-0.96], grip Y -0.85. All non-bat extras were unchanged in the Blender before/after comparison. Both Art and Resources copies are identical. Editor validation and player builds inspect the imported geometry, and the shared-prop importer preserves CPU readability for those checks.

Falsification mattered: earlier captures were overwritten by ordinary actor draws; a contact-only axis fit reversed charge/follow; forearm-tip and static culling-bounds measurements misrepresented visible hands; the exported bat had been recentered away from its declared grip; left-handed synthetic sockets omitted authored translation. Posed hand vertices are baked with scale compensation, and right-handed shared root-space measurements now agree across body proportions. Negative direction evidence remains in `../503-swing/`.

This proves the recorded geometry and automated paths, not Nintendo timing parity or human appearance/gameplay acceptance. Head/hat occlusion in a particular shot remains a human look observation. A complete Harbor half, physical controllers, and human character acceptance remain open.
