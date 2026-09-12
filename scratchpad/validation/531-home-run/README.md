# #531 — home-run lifecycle evidence

Captured by the opt-in `LivePlayLifecycleGate` in the actual Harbor scene using GUI Unity **6000.5.9f1**. JSON files preserve the original output and exact code revisions; this evidence commit does not change those revisions.

- `8fbcc8e-failed.json`: first CPU solo homer remained InPlay after 12.83 seconds. The live clock advanced, but the ball was marked caught and the play still classified HomeRun. This falsified the first completion-only fix: an automatic pickup could reach through Harbor's wall and transfer the stuck play to the human throw owner.
- `d38e966-intermediate.json`: the first eight-case gate passed, but review found its timing assertion incomplete: human homers finished at hang or hang+0.18 while CPU waited until hang+0.35. This output is retained as **intermediate evidence, not final acceptance**. The original cases covered: solo homer, loaded homer, injected wall robbery, and loaded walk-off. Each runs the production `TickLive` to Result and `TickFlow` to the next SET or GameOver, checks score and batter progression, and rejects a duplicate result. The near-wall fixture uses the actual 110 mph / 35° trajectory in Harbor wind: approximately 404.28 feet past its 400-foot center fence.

- `5934756-passed.json`: final stricter gate passes all eight cases. CPU and human uncaught homers both finish at **8.18338 seconds**; the injected late wall catch at hang+0.10 finishes as FlyOut at **7.95005 seconds**, with no score. Loaded walk-offs award four runs and reach GameOver. The gate explicitly rejects an uncaught homer completed before the shared hang+0.35 deadline.

The gate injects contact and, for robbery cases, a catch observation. It exercises the real lifecycle, not physical pad input or a naturally played half. This evidence does **not** pass #346, a human wall-jump timing gate, or a visual acceptance gate. Root owns the separate standalone build and rendered-window check.

Reproduction: see `docs/validation.md`, “Opt-in live-play lifecycle gate.”
