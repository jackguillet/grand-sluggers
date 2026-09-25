using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The pursuit stick at the couch (the Unity pass of #718; a real director since #1042). The sim keeps each seat's
    /// calibration, arming and gates (<see cref="PursuitStick"/>); <see cref="PursuitReadiness"/> decides when a
    /// released-stick window is sampled. This director owns that readiness and Call time's Reset stick card: it feeds the
    /// seated devices once a frame on the unscaled input clock, runs the card, and draws what a seat is told. Everything
    /// it reads from the match flow is handed in, so it never reaches into <see cref="MatchDirector"/>.
    /// </summary>
    public sealed class PursuitSeatDirector
    {
        /// <summary>Unscaled seconds the card shows STICK RESET before it closes itself.</summary>
        const float ResetHoldSec = 0.8f;

        readonly PursuitReadiness _pursuit = new PursuitReadiness();
        readonly List<PursuitReadiness.SeatDevice> _devices = new List<PursuitReadiness.SeatDevice>(PursuitReadiness.SeatCount);
        readonly List<(string Text, float Progress)> _resetLines = new List<(string Text, float Progress)>(PursuitReadiness.SeatCount);
        float _resetShown;
        // The frame's inputs, handed in by Tick before anything reads or draws.
        Match _match;
        bool _bound, _toldBeat, _training;
        Seats _seats;

        /// <summary>Call time's Reset stick card is open.</summary>
        public bool ResetOpen { get; private set; }

        static FieldStickRules Rules(Match match) => match.Rules.Fielding.Stick;

        /// <summary>Call time offers Reset stick: a match with its seats bound and a seated controller connected.</summary>
        public bool OffersReset => _match != null && _bound && PursuitReadiness.Offered(Rules(_match), Devices());

        /// <summary>
        /// Every frame, before any director: take the frame's match, seats and beat, bind the seated devices to their
        /// sticks, and sample a window for a seat that owes one only outside live baseball and only while the seat is being
        /// told to let go — on a <paramref name="toldBeat"/> (SET and the result beat, see <see cref="DrawTells"/>) or on the
        /// Reset stick card. Never while the ball is live, never unannounced.
        /// </summary>
        public void Tick(Match match, bool seatsBound, bool toldBeat, Seats seats, bool training)
        {
            (_match, _bound, _toldBeat, _seats, _training) = (match, seatsBound, toldBeat, seats, training);
            if (match == null || !seatsBound) return;
            var told = ResetOpen || (!match.Paused && toldBeat);
            _pursuit.Tick(match.LivePlay, Rules(match), told, Time.unscaledTimeAsDouble, Devices());
        }

        IReadOnlyList<PursuitReadiness.SeatDevice> Devices()
        {
            _devices.Clear();
            for (var i = 0; i < PursuitReadiness.SeatCount; i++)
            {
                var seat = i == 1 ? LineupSeat.Pad2 : LineupSeat.Pad1;
                var human = _training ? i == 0 : _seats.Home == seat || _seats.Away == seat;
                var pad = i == 1 ? Controls.Pad2 : Controls.Pad1;
                _devices.Add(new PursuitReadiness.SeatDevice(
                    human, Controls.SeatDeviceId(i), pad.Present, pad.PursuitX, pad.PursuitY));
            }
            return _devices;
        }

        /// <summary>Call time's Reset stick: every seated controller samples a window while the card says to let go. False when nothing opened.</summary>
        public bool OpenReset()
        {
            if (_match == null || !_pursuit.Request(_match.LivePlay, Rules(_match), Devices())) return false;
            ResetOpen = true;
            _resetShown = 0f;
            return true;
        }

        /// <summary>
        /// The card, inside Call time: <paramref name="dismissed"/> (back out) keeps the old centre; a full set of adopted
        /// windows closes it after the hold. Null while the card stays open; otherwise whether it closed recalibrated.
        /// </summary>
        public bool? TickReset(bool dismissed)
        {
            var calibrated = _pursuit.Recalibrated;
            if (calibrated)
            {
                _resetShown += Time.unscaledDeltaTime;
                if (_resetShown < ResetHoldSec) return null;
            }
            else if (!dismissed)
                return null;
            _pursuit.Close();
            ResetOpen = false;
            return calibrated;
        }

        /// <summary>A seat's tell outside live play: let go while it has no profile, on the told beat — where it is sampled.</summary>
        public void DrawTells()
        {
            var match = _match;
            if (match == null || !_bound || match.Paused || !_toldBeat) return;
            for (var i = 0; i < PursuitReadiness.SeatCount; i++)
            {
                var tell = _pursuit.TellFor(i, match.LivePlay, Rules(match));
                if (tell != PursuitReadiness.Tell.LetGo) continue;
                HudView.StickTell(BroadcastHud.StickLine(tell, i, _seats.Count > 1),
                    (float)_pursuit.Progress(i, Rules(match), Time.unscaledTimeAsDouble));
                return;
            }
        }

        /// <summary>The live ball's tell: the fielding seat has not been seen at rest since it took the field.</summary>
        public static void DrawUnready(Match match, bool humanFields)
        {
            if (match == null || !humanFields || !match.LivePlay.PursuitUnready) return;
            HudView.StickTell(BroadcastHud.UnreadyTell, 0f);
        }

        /// <summary>Call time's Reset stick card: each seated controller's line and its window's progress.</summary>
        public void DrawReset()
        {
            var match = _match;
            _resetLines.Clear();
            if (match == null) return;
            for (var i = 0; i < PursuitReadiness.SeatCount; i++)
            {
                var tell = _pursuit.TellFor(i, match.LivePlay, Rules(match));
                var text = BroadcastHud.StickLine(tell, i, _seats.Count > 1);
                if (string.IsNullOrEmpty(text)) continue;
                _resetLines.Add((text, tell == PursuitReadiness.Tell.Reset ? 1f : (float)_pursuit.Progress(i, Rules(match), Time.unscaledTimeAsDouble)));
            }
            HudView.StickReset(_resetLines);
        }
    }
}
