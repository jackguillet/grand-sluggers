using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// SET's camera (spec §4.1; its own class since #1042): which at-bat shot SET and the pitch's flight look through,
    /// from the seats, the charge and the pitcher's aim (<see cref="AtBatShots.SetShot"/>), and the stick-to-world
    /// scale that shot gives the rubber walk and the pitch's break. The shot is cut, never blended.
    /// </summary>
    internal sealed class SetCamera
    {
        readonly MatchScene _scene;
        readonly PlayState _play;
        readonly SeatPads _pads;
        readonly ISetCameraHost _host;

        public SetCamera(MatchScene scene, PlayState play, SeatPads pads, ISetCameraHost host)
        {
            _scene = scene; _play = play; _pads = pads; _host = host;
        }

        /// <summary>The pitcher's aim the mound shot leans toward; zero at the start of SET.</summary>
        public float AimX { get; private set; }
        public float AimY { get; private set; }

        public void AimAt(float x, float y) { AimX = x; AimY = y; }

        /// <summary>The shot SET or the pitch's flight looks through now.</summary>
        public string Shot() => AtBatShots.SetShot(_pads.HumanPitches, _play.Phase == MatchDirector.Phase.Flight,
            _pads.HumanPitches ? _host.PitchCharge : _play.Charge, AimX, AimY, _pads.TrainingOn, _pads.Live.Count);

        /// <summary>The shot SET opens on, with no charge and no aim: the result beat cuts to it when no live camera holds.</summary>
        public string Rest() => AtBatShots.SetShot(_pads.HumanPitches, false, 0, 0, 0, _pads.TrainingOn, _pads.Live.Count);

        /// <summary>
        /// Cut to <see cref="Shot"/>. Snap: blending SET→flight keeps looking at the dirt while the ball leaves the
        /// hand, so looking strikes land with no baseball (#305).
        /// </summary>
        public void Aim() => _scene.Cam.Cut(Shot());

        /// <summary>A stick's screen X as world feet under the current shot.</summary>
        public float WorldX(float screenX) =>
            (float)AtBatControl.WorldHorizontal(screenX, _scene.Content.Shots.Must(Shot()));

        /// <summary>The live camera and the pitcher's place in it, to the player log.</summary>
        public void Log(string tag)
        {
            var live = Camera.main;
            var rio = _host.PitcherHero();
            var rp = rio != null ? rio.transform.position.ToString("F1") : "null";
            var vp = rio != null && live != null
                ? live.WorldToViewportPoint(rio.transform.position + Vector3.up * 2.2f).ToString("F2")
                : "-";
            var cam = _scene.Cam;
            Debug.Log("GS SET " + tag
                + " shot=" + (cam != null ? cam.Shot : "?")
                + " pos=" + (live != null ? live.transform.position.ToString("F1") : "null")
                + " fwd=" + (live != null ? live.transform.forward.ToString("F2") : "-")
                + " fov=" + (live != null ? live.fieldOfView.ToString("F1") : "-")
                + " fl=" + (live != null ? live.focalLength.ToString("F1") : "-")
                + " phys=" + (live != null && live.usePhysicalProperties)
                + " px=" + (live != null ? live.pixelWidth + "x" + live.pixelHeight : "-")
                + " cams=" + Camera.allCamerasCount
                + " rio=" + rp + " vp=" + vp);
        }
    }

    /// <summary>What <see cref="SetCamera"/> reads from the at-bat: the human pitcher's held charge and the pitcher's body.</summary>
    internal interface ISetCameraHost
    {
        float PitchCharge { get; }
        HeroActor PitcherHero();
    }
}
