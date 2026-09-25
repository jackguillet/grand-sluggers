using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Juice by weight (CH-13, CF-6): the hit-stop and each body's anticipation and settle, from its body class's row in
    /// <c>feel.weightJuice</c>. The hit-stop holds the sim for whole frames (<see cref="HitStop"/>): the sim is not stepped,
    /// so its clock, its trace and its plays are the same whatever the table says; presses made while it holds are folded
    /// into the first step after it. The anticipation and settle are scale on the body's presentation wrapper, never a bone.
    /// Every seat, every play type and every captain reads the same rows; nothing here is per pad or per play.
    /// </summary>
    public sealed class JuiceDirector
    {
        readonly HitStop _stop = new HitStop();
        readonly ImpactClock _impacts = new ImpactClock();
        LivePadInput _heldField, _heldRun;

        /// <summary>
        /// One frame: true while the hit-stop holds the sim (the caller must not step it), and <paramref name="dt"/> scaled to
        /// the drawn clocks' creep.
        /// </summary>
        public bool Frame(float unscaledDt, FeelTable feel, ref float dt)
        {
            var creep = feel.WeightJuice.HitStopCreepMul;
            var f = _stop.Frame(unscaledDt, creep);
            if (f.Held) dt *= (float)creep;
            return f.Held;
        }

        /// <summary>A held frame's pads, kept for the first step after the hold.</summary>
        public void Latch(LivePadInput field, LivePadInput run)
        {
            _heldField = HitStop.Latch(_heldField, field);
            _heldRun = HitStop.Latch(_heldRun, run);
        }

        /// <summary>The fielding pad for a live step: this frame's with any held press folded in.</summary>
        public LivePadInput Field(LivePadInput now)
        {
            var pad = HitStop.Latch(_heldField, now);
            _heldField = null;
            return pad;
        }

        /// <summary>The running pad for a live step: this frame's with any held press folded in.</summary>
        public LivePadInput Run(LivePadInput now)
        {
            var pad = HitStop.Latch(_heldRun, now);
            _heldRun = null;
            return pad;
        }

        /// <summary>The batter's own contact: the quality's freeze × its class, and its settle starts.</summary>
        public void Contact(Character batter, double qualityFreezeSec, FeelTable feel)
        {
            _stop.Begin(WeightJuice.HitStopSec(qualityFreezeSec, WeightJuice.Of(batter, feel)));
            if (batter != null) _impacts.Impact(batter.Id);
        }

        /// <summary>A body's own catch (the glove's first touch of a batted ball): its class's hold, and its settle starts.</summary>
        public void Catch(Character who, FeelTable feel)
        {
            if (who == null) return;
            _stop.Begin(WeightJuice.CatchStopSec(WeightJuice.Of(who, feel)));
            _impacts.Impact(who.Id);
        }

        /// <summary>Age the settles on the drawn clock.</summary>
        public void Age(float dt) => _impacts.Age(dt);

        /// <summary>
        /// This frame's weight juice on a body's wrapper: its load <paramref name="secToRelease"/> before a release (NaN when
        /// none is coming) and its settle since its last impact.
        /// </summary>
        public Vector3 Wrapper(Character who, double secToRelease, FeelTable feel)
        {
            var s = WeightJuice.Wrapper(WeightJuice.Of(who, feel), secToRelease, who != null ? _impacts.Since(who.Id) : double.NaN);
            return new Vector3((float)s.X, (float)s.Y, (float)s.Z);
        }

        /// <summary>A staged still starts with no hold, no settle and no held press.</summary>
        public void Clear()
        {
            _stop.Clear();
            _impacts.Clear();
            _heldField = _heldRun = null;
        }
    }
}
