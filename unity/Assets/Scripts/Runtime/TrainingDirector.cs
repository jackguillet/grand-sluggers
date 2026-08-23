using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Harbor drills on top of the existing at-bat / fielding loop.</summary>
    public sealed class TrainingDirector : MonoBehaviour
    {
        Transform _board;
        TextMesh _title;
        TextMesh _verb;

        public Training Session { get; private set; }
        public bool Active => Session != null && !Session.Finished;

        public void Begin(ContentCatalog content)
        {
            Session = Training.Start(content);
            EnsureBoard();
            Refresh(null);
        }

        public void Stop()
        {
            Session = null;
            if (_board != null) _board.gameObject.SetActive(false);
        }

        public Match MakeMatch(ContentCatalog content, int seed) =>
            Session != null ? Session.MakeMatch(content, seed) : Match.Exhibition(content, parkId: Training.ParkId, seed: seed);

        public bool PlayerPitches => Active && Session.CurrentDrill == 1;
        public bool PlayerBats => Active && Session.CurrentDrill == 2;
        public bool PlayerFields => Active && Session.CurrentDrill is 3 or 4 or 5;

        public void OnPitch(PitchCommand pitch, Match match)
        {
            if (Session == null || Session.CurrentDrill != 1) return;
            Session.RecordPitch(pitch, match);
        }

        public void OnSwing(SwingCommand swing, AtBatResult hit)
        {
            if (Session == null || Session.CurrentDrill != 2) return;
            Session.RecordSwing(swing, hit);
        }

        public void OnField(FieldingResult field, Match match)
        {
            if (Session == null) return;
            if (Session.CurrentDrill == 3)
                Session.RecordFielding(field);
            else if (Session.CurrentDrill == 4)
                Session.RecordGrounder(field);
            else if (Session.CurrentDrill == 5)
                Session.RecordChemThrow(field.Throw);
        }

        public void Tick(Camera cam)
        {
            if (_board == null) return;
            if (Session == null)
            {
                _board.gameObject.SetActive(false);
                return;
            }
            Refresh(cam);
        }

        void EnsureBoard()
        {
            if (_board != null) return;
            var go = new GameObject("TrainingCaption");
            go.transform.SetParent(transform, false);
            _board = go.transform;
            _title = Label(go.transform, "Title", 0.22f, 56, Colors.Gold, new Vector3(0, 0.8f, 0));
            _verb = Label(go.transform, "Verb", 0.12f, 42, Color.white, new Vector3(0, -0.6f, 0));
        }

        static TextMesh Label(Transform parent, string name, float size, int font, Color color, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var mesh = go.AddComponent<TextMesh>();
            mesh.fontSize = font;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            mesh.fontStyle = FontStyle.Bold;
            return mesh;
        }

        void Refresh(Camera cam)
        {
            if (Session == null || _board == null) return;
            _board.gameObject.SetActive(true);
            _board.position = new Vector3(0f, 11f, 28f);
            _title.text = Session.Finished ? Session.Caption : Session.CurrentDrill + "  " + Session.Caption;
            _verb.text = Session.Finished ? Session.Verb : Session.Progress + "\n" + Session.Verb;
            if (cam == null) cam = Camera.main;
            if (cam != null)
                _board.LookAt(_board.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
        }
    }
}
