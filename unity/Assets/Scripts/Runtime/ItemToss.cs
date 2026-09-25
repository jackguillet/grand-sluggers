using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The on-deck item toss (spec §12; its own class since #1042): offered to the batting human while their batted ball
    /// is live, cycled and aimed on the runner pad, thrown at a fielder, flown for the rules' flight time, and smashed by
    /// a fielder's dash in reach. The sim applies the item (<see cref="LivePlayCommand.ApplyItem"/>); this owns the pick,
    /// the target, the throw in flight and the item's picture and words.
    /// </summary>
    internal sealed class ItemToss
    {
        readonly MatchScene _scene;
        readonly PlayState _play;
        readonly LiveFieldState _live;
        readonly SeatPads _pads;
        readonly IItemHost _host;

        public ItemToss(MatchScene scene, PlayState play, LiveFieldState live, SeatPads pads, IItemHost host)
        {
            _scene = scene; _play = play; _live = live; _pads = pads; _host = host;
        }

        public int Pick { get; private set; }
        /// <summary>The fielder the item is aimed at.</summary>
        public Character Target { get; private set; }
        public bool Thrown { get; private set; }
        /// <summary>The item is in the air; the flight clock and the item thrown.</summary>
        public bool Flying { get; private set; }
        public float Fly { get; private set; }
        public string Id { get; private set; } = "";

        TrainingDirector Coach => _host.Coach;
        bool TrainingOn => Coach != null && Coach.Session != null;
        float FlySec => (float)_scene.Content.Rules.Batting.Items.FlySec;

        /// <summary>The batting human may throw one: their batted ball is live, none thrown yet, and no throw is in flight.</summary>
        public bool Offered =>
            _pads.HumanBats && _play.Pending != null && _play.Pending.ChemistryItemOffered && !Thrown
            && _play.Phase == MatchDirector.Phase.InPlay && !_live.Throwing;

        /// <summary>A new ball: nothing picked or thrown; the target is <paramref name="target"/> (the ball's fielder, or none).</summary>
        public void Reset(Character target = null)
        {
            Thrown = false;
            Flying = false;
            Fly = 0;
            Id = "";
            Pick = 0;
            Target = target;
        }

        /// <summary>The play ended: nothing flies and the item is hidden.</summary>
        public void End()
        {
            Flying = false;
            _scene.Items?.Hide();
        }

        /// <summary>The sim smashed the item in the air.</summary>
        public void Smashed()
        {
            Flying = false;
            Id = "";
            _scene.Items?.Hide();
        }

        /// <summary>One frame: the flight clock, a dash that smashes the item in reach, then the offer's cycle, aim and throw.</summary>
        public void Tick(float dt)
        {
            var match = _play.Match;
            if (Flying)
            {
                Fly += dt;
                if (Fly >= FlySec) Flying = false;
            }
            if (Flying && _pads.FieldPad.Attack)
            {
                var dest = TargetWorld();
                var dist = Diamond.Dist(_live.GloveX, _live.GloveZ, dest.x, dest.z);
                if (FieldDash.DestroysItem(true, true, dist, _scene.Content.Rules))
                {
                    match.LivePlay.Apply(LivePlayCommand.SmashItem(match.LivePlay.Source));
                    Smashed();
                    _host.Sub = BroadcastHud.ItemSmashed;
                    return;
                }
            }
            if (!Offered) return;
            var pad = _pads.RunPad;
            // LT held for a bunt that made contact is no item modifier until it comes up and is pressed (PH-14-R6).
            var ltFree = _pads.TriggerFree(pad, BuntSide.First);
            if (pad.ItemCycle != 0)
                Pick = (Pick + pad.ItemCycle + ErrorItems.All.Length) % ErrorItems.All.Length;
            Aim();
            if (!TrainingOn)
                _host.Sub = BroadcastHud.ItemAim(ErrorItems.All[Pick]);
            if (!pad.ItemConfirmWith(ltFree) || Target == null) return;
            var id = ErrorItems.All[Pick];
            if (Coach != null && Coach.Tutorial != null && Coach.Tutorial.IsItemLesson)
            {
                if (!Coach.Tutorial.Item(id, Target.Id)) return;
            }
            else match.LivePlay.Apply(LivePlayCommand.ApplyItem(id, Target, match.LivePlay.Source));
            Thrown = true;
            Flying = true;
            Fly = 0;
            Id = id;
            _scene.Audio?.Item(id);
            if (!TrainingOn) _host.Sub = "";
        }

        void Aim()
        {
            var map = _play.Match.DefenseMap;
            var play = _live.CpuField != null && _live.CpuField.Fielder != null ? _live.CpuField.Fielder
                : _play.Preview != null ? _play.Preview.Fielder : null;
            var runPad = _pads.RunPad;
            var stick = Mathf.Abs(runPad.StickX) + Mathf.Abs(runPad.StickY);
            if (stick < 0.28f)
            {
                Target = play;
                return;
            }
            var x = runPad.StickX * 160;
            var z = 30 + (runPad.StickY * 0.5f + 0.5f) * 300;
            Target = FieldingResolver.NearestGlove(map, x, z, _live.GloveAt).Fielder;
        }

        /// <summary>Where the item is aimed in the world: the target's body, else the ball's landing, else deep center.</summary>
        public Vector3 TargetWorld()
        {
            if (Target != null && _scene.Heroes.TryGetValue(Target.Id, out var h) && h != null)
                return h.transform.position;
            if (_play.Preview != null)
                return new Vector3((float)_play.Preview.LandingX, 0, (float)_play.Preview.LandingZ);
            return new Vector3(0, 0, 80);
        }

        /// <summary>The scorebug's item word: the pick while offered, the item while it is in play.</summary>
        public string Hud()
        {
            if (Offered) return ErrorItems.All[Pick].ToUpperInvariant();
            if (Flying || Thrown) return Id.ToUpperInvariant();
            return "";
        }

        /// <summary>The item's picture: the offer card, the throw along its arc, the landing.</summary>
        public void Present(float dt)
        {
            var showThrow = Flying || (Thrown && _play.Phase == MatchDirector.Phase.InPlay);
            var flyU = !Flying && Thrown ? 1f : Flying ? Mathf.Clamp01(Fly / FlySec) : 0f;
            _scene.Items?.Present(dt, Offered, Pick, TargetWorld(), showThrow, Id, flyU);
        }
    }

    /// <summary>What the item toss asks of the flow: the practice coach (a lesson may own the throw), and the subtitle.</summary>
    internal interface IItemHost
    {
        TrainingDirector Coach { get; }
        string Sub { set; }
    }
}
