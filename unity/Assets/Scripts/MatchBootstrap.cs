// Replaced by MatchDirector. Kept so old scene refs don't explode if a leftover object exists.
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    [System.Obsolete("Use MatchDirector.")]
    public sealed class MatchBootstrap : MonoBehaviour
    {
        void Awake()
        {
            if (GetComponent<MatchDirector>() == null)
                gameObject.AddComponent<MatchDirector>();
            enabled = false;
        }
    }
}

