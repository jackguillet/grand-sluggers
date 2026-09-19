using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The pursuit stick at the couch (the Unity pass of #718). The sim keeps each seat's calibration, arming and gates
    /// (<see cref="PursuitStick"/>); <see cref="PursuitReadiness"/> decides when a released-stick window is sampled. This
    /// partial feeds it the seated devices once a frame on the unscaled input clock, runs Call time's Reset stick card, and
    /// draws what a seat is told. Nothing here runs on the shipped table: its stick reads no calibration.
    /// </summary>
    public sealed partial class MatchDirector
    {
        readonly PursuitReadiness _pursuit = new PursuitReadiness();
        readonly List<PursuitReadiness.SeatDevice> _pursuitDevices = new List<PursuitReadiness.SeatDevice>(PursuitReadiness.SeatCount);
        /// <summary>Call time's Reset stick card is open.</summary>
        bool _stickReset;
        /// <summary>Unscaled seconds the card has shown STICK RESET; it closes itself after <see cref="StickResetHoldSec"/>.</summary>
        float _stickResetShown;
        const float StickResetHoldSec = 0.8f;

        FieldStickRules StickRules => _match.Rules.Fielding.Stick;

        /// <summary>The calibrated radial stick is this match's (the c80 copy); the shipped table's Manhattan gate needs none of this.</summary>
        bool RadialStick => _match != null && StickRules.Radial;

        /// <summary>Call time offers Reset stick: the radial stick with a seated controller connected.</summary>
        bool OffersStickReset => RadialStick && _matchSeats.Bound && PursuitReadiness.Offered(StickRules, PursuitDevices());

        /// <summary>
        /// Every frame, before any director: bind the seated devices to their sticks, and sample a window for a seat that owes
        /// one only outside live baseball and only while the seat is being told to let go — at SET and the result beat
        /// (<see cref="DrawStickTells"/>) or on Call time's Reset stick card. Never while the ball is live, never unannounced.
        /// </summary>
        void TickPursuitSeats()
        {
            if (!RadialStick || !_matchSeats.Bound) return;
            var told = _stickReset || (!_match.Paused && _phase is Phase.Set or Phase.Result);
            _pursuit.Tick(_match.LivePlay, StickRules, told, Time.unscaledTimeAsDouble, PursuitDevices());
        }

        IReadOnlyList<PursuitReadiness.SeatDevice> PursuitDevices()
        {
            _pursuitDevices.Clear();
            var seats = LiveSeats;
            for (var i = 0; i < PursuitReadiness.SeatCount; i++)
            {
                var seat = i == 1 ? LineupSeat.Pad2 : LineupSeat.Pad1;
                var human = TrainingOn ? i == 0 : seats.Home == seat || seats.Away == seat;
                var pad = i == 1 ? Controls.Pad2 : Controls.Pad1;
                _pursuitDevices.Add(new PursuitReadiness.SeatDevice(
                    human, Controls.SeatDeviceId(i), Controls.SeatUsesKeyboard(i), pad.Present, pad.PursuitX, pad.PursuitY));
            }
            return _pursuitDevices;
        }

        /// <summary>Call time's Reset stick: every seated controller samples a window while the card says to let go.</summary>
        void OpenStickReset()
        {
            if (!_pursuit.Request(_match.LivePlay, StickRules, PursuitDevices())) return;
            _stickReset = true;
            _stickResetShown = 0f;
            _t = 0;
        }

        /// <summary>The card, inside Call time: back out keeps the old centre; a full set of adopted windows closes it.</summary>
        void TickStickReset()
        {
            var calibrated = _pursuit.Recalibrated;
            if (_pursuit.Recalibrated)
            {
                _stickResetShown += Time.unscaledDeltaTime;
                if (_stickResetShown < StickResetHoldSec) return;
            }
            else if (!PauseMenu.Dismiss(Controls.EastDown || Controls.CallTime || Controls.MouseBack || Controls.HowTo, _t))
                return;
            _pursuit.Close();
            _stickReset = false;
            _t = 0;
            if (calibrated && GuidedAttempt("T-G06-C")) GuidedObserve(GuidedAction.StickRecalibrated);
        }

        /// <summary>A seat's tell outside live play: let go while it has no profile, at SET and the result beat — where it is sampled.</summary>
        void DrawStickTells()
        {
            if (!RadialStick || !_matchSeats.Bound || _match.Paused || _phase is not (Phase.Set or Phase.Result)) return;
            var devices = PursuitDevices();
            var two = LiveSeats.Count > 1;
            for (var i = 0; i < PursuitReadiness.SeatCount; i++)
            {
                var tell = _pursuit.TellFor(i, _match.LivePlay, StickRules);
                if (tell != PursuitReadiness.Tell.LetGo) continue;
                HudView.StickTell(BroadcastHud.StickLine(tell, i, two, devices[i].Keyboard),
                    (float)_pursuit.Progress(i, StickRules, Time.unscaledTimeAsDouble));
                return;
            }
        }

        /// <summary>The live ball's tell: the fielding seat has not been seen at rest since it took the field.</summary>
        void DrawUnreadyTell()
        {
            if (!RadialStick || !HumanFields || !_match.LivePlay.PursuitUnready) return;
            HudView.StickTell(BroadcastHud.UnreadyTell, 0f);
        }

        /// <summary>Call time's Reset stick card: each seated controller's line and its window's progress.</summary>
        void DrawStickReset()
        {
            var devices = PursuitDevices();
            var two = LiveSeats.Count > 1;
            var lines = new List<(string Text, float Progress)>(PursuitReadiness.SeatCount);
            for (var i = 0; i < PursuitReadiness.SeatCount; i++)
            {
                var tell = _pursuit.TellFor(i, _match.LivePlay, StickRules);
                var text = BroadcastHud.StickLine(tell, i, two, devices[i].Keyboard);
                if (string.IsNullOrEmpty(text)) continue;
                lines.Add((text, tell == PursuitReadiness.Tell.Reset ? 1f : (float)_pursuit.Progress(i, StickRules, Time.unscaledTimeAsDouble)));
            }
            HudView.StickReset(lines);
        }
    }
}
