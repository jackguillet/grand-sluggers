using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrandSluggers.UnityClient
{
    public static partial class TeamSheet
    {
        public enum PitcherAction { None, Confirm, Cancel }
        static Vector2 _pitcherMouse;
        static readonly Rect PitcherWindow = new Rect(100, 90, 1080, 630);
        static readonly Rect PitcherField = new Rect(150, 250, 500, 380);
        static readonly Rect PitcherCard = new Rect(730, 238, 380, 240);
        static readonly Rect PitcherConfirm = new Rect(850, 650, 260, 44);
        static readonly Rect PitcherCancel = new Rect(730, 650, 108, 44);

        public static void BeginPitcherPick() => _pitcherMouse = Mouse.current?.position.ReadValue() ?? Vector2.zero;

        static Rect PitcherHead(string position)
        {
            var spot = LineupLayout.FieldSpot(position);
            return new Rect(PitcherField.x + (float)spot.X * PitcherField.width - 41,
                PitcherField.y + (float)spot.Y * PitcherField.height - 43, 82, 86);
        }

        // Drawing and pointer targets use the same couch coordinates. Hover only inspects;
        // the explicit button (or Select/R) is the sole commit.
        public static PitcherAction PitcherPointer(PitcherSwapPick pick)
        {
            if (Mouse.current == null) return PitcherAction.None;
            var mouse = Mouse.current.position.ReadValue();
            var moved = (mouse - _pitcherMouse).sqrMagnitude > .01f;
            _pitcherMouse = mouse;
            var clicked = Controls.PointerDown;
            var p = new Vector2(mouse.x * 1280 / Screen.width, (Screen.height - mouse.y) * 800 / Screen.height);
            if (moved || clicked)
                for (var i = 0; i < pick.Candidates.Count; i++)
                    if (PitcherHead(pick.Candidates[i].Pos).Contains(p)) { pick.Inspect(i); break; }
            if (clicked && PitcherConfirm.Contains(p)) return PitcherAction.Confirm;
            if (clicked && PitcherCancel.Contains(p)) return PitcherAction.Cancel;
            return PitcherAction.None;
        }

        public static void DrawPitcherPick(Match match, PitcherSwapPick pick, int seat)
        {
            Ensure();
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 800f, 1));
            Fill(PitcherWindow, Ink);
            Border(PitcherWindow, new Color(.40f, .55f, .53f), 2);
            var keys = Controls.SeatUsesKeyboard(seat);
            Label(138, 114, 900, 24, "P" + (seat + 1) + "  /  DEFENSE  /  ONCE PER HALF", _small);
            Label(138, 145, 960, 42, "Change pitcher", _title);
            Label(138, 192, 980, 26, match.Pitcher.Name + " on the mound  ·  " + BroadcastHud.ArmLine(match.PitcherStamina, match.Rules), _body);
            GUI.DrawTexture(PitcherField, _field);
            var chosen = pick.Current;
            foreach (var candidate in pick.Candidates)
                PitcherPortrait(candidate.Who, candidate.Pos, candidate.Who.Id == chosen.Who.Id,
                    match.Chemistry.Between(chosen.Who, candidate.Who), false);
            PitcherPortrait(match.Pitcher, "P", false, match.Chemistry.Between(chosen.Who, match.Pitcher), true);
            Fill(PitcherCard, new Color(.075f, .115f, .14f));
            Label(PitcherCard.x + 16, PitcherCard.y + 10, 348, 22, chosen.Pos + "  →  PITCHER", _small);
            CardDetails(chosen.Who, PitcherCard);
            Label(746, 492, 350, 28, BroadcastHud.ArmLine(match.StaminaOf(chosen.Who), match.Rules), _heading);
            Label(746, 524, 350, 26, (chosen.Who.Throws == Hand.L ? "Throws left" : "Throws right") + "  ·  "
                + BroadcastHud.PitcherPitches(chosen.Who.Repertoire, match.Rules.Pitching.Families), _body);
            Label(746, 562, 350, 26, match.Pitcher.Name + " moves to " + chosen.Pos, _body);
            Label(746, 589, 350, 26, "Each arm keeps its remaining stamina.", _small);
            Label(138, 650, 550, 24, "CHEMISTRY WITH YOUR PICK  ·  Good / Poor / Neutral", _small);
            Label(138, 677, 550, 24, keys ? "A / D  Browse · Hover to inspect · R  Confirm" : "Left / right  Browse · Select  Confirm · East  Cancel", _body);
            PitcherButton(PitcherCancel, keys ? "G  Cancel" : "Cancel", false);
            PitcherButton(PitcherConfirm, keys ? "R  Put on mound" : "Select  Put on mound", true);
            GUI.matrix = old;
        }

        static void PitcherPortrait(Character who, string pos, bool selected, Chemistry chemistry, bool current)
        {
            var r = PitcherHead(pos);
            if (!selected && chemistry == Chemistry.Good) Fill(r, new Color(.76f, .96f, .88f, .16f));
            if (!selected && chemistry == Chemistry.Bad) Fill(r, new Color(.96f, .58f, .54f, .13f));
            if (selected) { Fill(r, new Color(1, 1, 1, .10f)); Border(r, Gold, 2); }
            Label(r.x, r.y - 2, r.width, 18, current ? "P · CURRENT" : pos, _mark);
            Portrait(who, new Rect(r.x + 10, r.y + 14, r.width - 20, 42));
            Label(r.x - 14, r.y + 54, r.width + 28, 18, who.Name, _mark);
            Label(r.x - 10, r.y + 71, r.width + 20, 16, selected ? "YOUR PICK"
                : chemistry == Chemistry.Good ? "Good" : chemistry == Chemistry.Bad ? "Poor" : "Neutral", _mark);
        }

        static void PitcherButton(Rect r, string text, bool primary)
        {
            Fill(r, primary ? new Color(.20f, .32f, .30f) : new Color(1, 1, 1, .055f));
            Border(r, primary ? new Color(.60f, .79f, .69f) : new Color(1, 1, 1, .14f), 1);
            Label(r.x + 12, r.y, r.width - 24, r.height, text, _body);
        }
    }
}
