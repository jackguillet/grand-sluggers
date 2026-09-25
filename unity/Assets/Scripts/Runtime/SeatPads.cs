using GrandSluggers.Sim;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Who holds which seat this half, and which pad speaks for it (spec §0.4, #1042): the seated controllers, the seats a
    /// practice drill or a lesson assigns, the pad that pitches, bats, fields and runs, and the one command a frame the
    /// live ball reads from the field and runner pads. A seat the CPU holds is a dead pad, so the batting human's stick
    /// never takes a glove and their South never gates a CPU throw. The plate's held buttons (<see cref="PlayState"/>)
    /// decide which trigger and East a pad may still spend (PH-14-R6, PH-13-R1).
    /// </summary>
    internal sealed class SeatPads
    {
        readonly PlayState _play;
        readonly MatchSeatLifecycle _matchSeats;
        readonly ISeatHost _host;

        public SeatPads(PlayState play, MatchSeatLifecycle matchSeats, ISeatHost host)
        {
            _play = play; _matchSeats = matchSeats; _host = host;
        }

        TrainingDirector Coach => _host.Coach;
        public bool TrainingOn => Coach != null && Coach.Session != null;
        bool TutorialOn => Coach != null && Coach.Tutorial != null;
        Match Match => _play.Match;

        /// <summary>The seats this match would bind: a lesson's, one pad for practice and Challenge, else the pads and the pick.</summary>
        public Seats Selected =>
            TutorialOn ? (Coach.PlayerBats || Coach.PlayerRuns || Coach.Tutorial.DefendsAsAway ? Seats.AwayOne : Seats.One) : TrainingOn || !_host.Exhibition
                ? Seats.One
                : Seats.FromPads(Controls.PadCount, _host.Pad1Home, versus: _host.VersusWanted);
        /// <summary>The seats bound for this match, or the ones it would bind.</summary>
        public Seats Live => _matchSeats.Current(Selected);
        public bool HumanPitches => TrainingOn
            ? Coach.PlayerPitches
            : Match != null && Live.HumanPitches(Match.Top);
        public bool HumanBats => TrainingOn
            ? (Coach.PlayerBats || Coach.PlayerRuns)
            : Match != null && Live.HumanBats(Match.Top);
        public bool PlayerMustField => TrainingOn && Coach.PlayerFields;

        /// <summary>The sim's seat table for this half. Training seats come from the coach; a match derives them from (half, home/away, pads).</summary>
        public LiveSeats LiveNow() => TrainingOn
            ? new LiveSeats(HumanBats, HumanPitches, PlayerMustField, Versus: false)
            : Match != null ? LiveSeats.For(Live, Match.Top) : LiveSeats.CpuOnly;

        /// <summary>A human sits the defense this half. Mirrors <see cref="LiveSeats.HumanFields"/>.</summary>
        public bool HumanFields => LiveNow().HumanFields;
        public bool HumanOwnsThrow => LiveNow().HumanOwnsThrow;

        public Controls.Pad PitchPad => HumanPitches && Match != null
            ? Controls.Of(Live.Pitching(Match.Top))
            : Controls.Pad1;
        public Controls.Pad BatPad => HumanBats && Match != null
            ? Controls.Of(Live.Batting(Match.Top))
            : Controls.None;
        /// <summary>The glove pad is the controller seated on defense this half; the runner pad is the one on offense.</summary>
        public Controls.Pad FieldPad => !HumanFields ? Controls.None
            : TrainingOn ? Controls.Pad1
            : Controls.Of(Live.Fielding(Match.Top));
        public Controls.Pad RunPad => !HumanBats ? Controls.None
            : TrainingOn ? Controls.Pad1
            : Controls.Of(Live.Running(Match.Top));

        /// <summary>Whether <paramref name="pad"/>'s <paramref name="trigger"/> may mean any verb on this tick (PH-14-R6).</summary>
        public bool TriggerFree(Controls.Pad pad, BuntSide trigger) =>
            pad.Index < 0 || pad.Index != _play.PlateSeat || BuntHold.IsFree(_play.Plate.Bunt, trigger);

        /// <summary>Whether <paramref name="pad"/>'s East / G may mean a dive, a dash or a skip on this tick (PH-13-R1).</summary>
        public bool CancelFree(Controls.Pad pad) =>
            pad.Index < 0 || pad.Index != _play.PlateSeat || PlateButtons.CancelIsFree(_play.Plate);

        /// <summary>The field pad as one live command.</summary>
        public LivePadInput FieldInput()
        {
            var pad = FieldPad;
            // The calibrated radial stick (#718) reads the device coordinate before any dead zone, handed to the sim once.
            var radial = Match != null;
            var eastFree = CancelFree(pad);
            var cancel = eastFree && pad.EastDown && (_play.Phase == MatchDirector.Phase.Flight || Match.LivePlay.CanCancelThrow);
            if (cancel) pad.ClearThrowTarget();
            return new LivePadInput(
                radial ? pad.PursuitX : pad.StickX, radial ? pad.PursuitY : pad.StickY,
                SouthDown: pad.BallDown && pad.ThrowBag > 0, WestDown: pad.JumpDown && TriggerFree(pad, BuntSide.First),
                EastDown: pad.EastDown && eastFree && !cancel, EastHeld: pad.EastHeld && eastFree && !cancel,
                Cutoff: pad.Cutoff, Swap: pad.SwapPitcher,
                Attack: pad.Attack && TriggerFree(pad, BuntSide.Third), KeysBag: pad.ThrowBag,
                Cancel: cancel, Device: pad.Index, ExplicitTarget: true, CloseResponse: pad.SouthDown);
        }

        /// <summary>Selection is a right-stick flick; the movement stick never issues a runner order.</summary>
        public LivePadInput RunInput()
        {
            var pad = RunPad;
            return new LivePadInput(SouthDown: pad.SouthDown,
                WestDown: pad.WestDown && TriggerFree(pad, BuntSide.Third), Orders: pad.RunnerOrders);
        }
    }

    /// <summary>What the seats ask of the flow: the practice coach, and the Exhibition pick's pad choices.</summary>
    internal interface ISeatHost
    {
        TrainingDirector Coach { get; }
        bool Exhibition { get; }
        bool Pad1Home { get; }
        bool VersusWanted { get; }
    }
}
