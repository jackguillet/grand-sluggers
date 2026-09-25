using System;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// An empty component an editor tool runs on in Play. Unity attaches a component only if its class lives in a runtime
    /// assembly, so an editor-only tool (the still capture) cannot be one itself: it hangs its per-frame step and its
    /// coroutines on this host instead. The host knows no tool and does nothing on its own.
    /// </summary>
    public sealed class PlayHost : MonoBehaviour
    {
        public event Action Tick;
        public event Action Disabled;

        void Update() => Tick?.Invoke();

        void OnDisable() => Disabled?.Invoke();
    }
}
