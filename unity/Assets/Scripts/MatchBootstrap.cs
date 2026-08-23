#if UNITY_5_3_OR_NEWER
using System.IO;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Drop on an empty GameObject once the Unity 6 URP project is opened.
    /// The playable slice today is <c>dotnet run --project src/GrandSluggers.Play</c>;
    /// this script is the same match loop waiting for a Unity renderer.
    /// </summary>
    public sealed class MatchBootstrap : MonoBehaviour
    {
        public int Seed = 7;
        public int Innings = 3;

        Match _match;

        void Start()
        {
            var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            var content = ContentCatalog.Load(data);
            _match = Match.Slice(content, Innings, Seed);
            Debug.Log($"Grand Sluggers  {_match.Away.Name} at {_match.Home.Name}  stars {_match.AwayStars:0}/{_match.HomeStars:0}");
        }

        void Update()
        {
            if (_match is null || _match.Over) return;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var ev = _match.AutoPlay();
                Debug.Log($"{ev.Kind}: {ev.Caption}  {_match.AwayScore}-{_match.HomeScore}");
                if (_match.Over)
                {
                    var mvp = _match.Mvp();
                    Debug.Log($"FINAL  MVP {mvp.Who.Name} ({mvp.Points})");
                }
            }
        }
    }
}
#endif
