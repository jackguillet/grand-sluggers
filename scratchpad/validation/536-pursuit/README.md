# Pursuit regression (#536)

`ed7e581-passed.json` records eight actual Harbor TickLive cases in GUI Unity 6000.5.9f1. All six grounders were picked up and reached Result, the routine fly became FlyOut, and the uncaught wall ball became HomeRun. Per-frame rated running speed checks passed. The gate injects contact/time and does not pass physical-controller or human gameplay acceptance.

The prior home-run lifecycle suite also passed all eight cases after the first pursuit integration at `415c9e6`; evidence is retained in the adjacent `531-home-run` folder. UnityEditor.Search reported a startup indexing exception after the gate; no gameplay exception was observed in these scenarios.
