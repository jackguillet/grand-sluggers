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
        public TutorialSession Tutorial { get; private set; }
        public void BeginTutorial(ContentCatalog content, TutorialCatalog catalog, string id, TutorialProgress progress)
        {
            Stop();
            Tutorial = new TutorialSession(content, catalog, id, progress);
            Session = Training.Start(content);
            var setup = System.Array.Find(catalog.Setups, s => s.Id == Tutorial.Lesson.Setup);
            Session.Choose(Tutorial.IsOffenseLesson ? PracticeLesson.Running
                : Tutorial.IsDefenseLesson ? PracticeLesson.Fielding
                : setup.Policy is "cpu-take" or "pickoff" or "pitcher-swap" or "defense-swap" or "game-count" or "game-half" ? PracticeLesson.Pitching : PracticeLesson.Batting);
        }

        public bool Active => Session != null && !Session.Finished;

        public void Begin(ContentCatalog content, PracticeLesson lesson = PracticeLesson.Pitching)
        {
            Session = Training.Start(content);
            Session.Choose(lesson);
            EnsureBoard();
            Refresh(null);
        }

        public void Stop()
        {
            Tutorial?.Exit();
            Tutorial = null;
            Session = null;
            if (_board != null) _board.gameObject.SetActive(false);
        }

        public Match MakeMatch(ContentCatalog content, int seed) =>
            Tutorial != null ? Tutorial.Match : Session != null ? Session.MakeMatch(content, seed) : Match.Exhibition(content, parkId: Training.ParkId, seed: seed);

        public bool PlayerPitches => Active && Session.Lesson == PracticeLesson.Pitching;
        public bool PlayerBats => Active && Session.Lesson == PracticeLesson.Batting;
        public bool PlayerRuns => Active && Session.Lesson == PracticeLesson.Running;
        public bool PlayerFields => Active && Session.Lesson == PracticeLesson.Fielding;

        public void OnPitch(PitchCommand pitch, Match match)
        {
            if (Tutorial != null || Session == null || Session.Lesson != PracticeLesson.Pitching) return;
            Session.RecordPitch(pitch, match);
        }

        public void OnSwing(SwingCommand swing, AtBatResult hit)
        {
            if (Tutorial != null || Session == null || Session.Lesson != PracticeLesson.Batting) return;
            Session.RecordSwing(swing, hit);
        }

        public void OnRun(Match match)
        {
            if (Tutorial != null || Session == null || Session.Lesson != PracticeLesson.Running) return;
            Session.RecordRun(match);
        }

        public void OnField(FieldingResult field, Match match)
        {
            if (Tutorial != null || Session == null) return;
            if (Session.Lesson == PracticeLesson.Fielding)
            {
                if (!Session.RecordFielding(field))
                    Session.RecordGrounder(field);
                if (match.Log.Count > 0)
                    Session.RecordTurnTwo(match.Log[match.Log.Count - 1]);
            }
            else if (Session.Lesson == PracticeLesson.Special)
                Session.RecordChemThrow(field.Throw);
        }

        /// <param name="eastFree">
        /// False while East / G is a press the plate took as the swing cancel (PH-13-R1,
        /// <see cref="PlateButtons.CancelIsFree"/>): the same press never also skips the drill.
        /// </param>
        public void TickSkip(bool eastFree = true)
        {
            if (Tutorial != null || Session == null || Session.Finished) return;
            if (Controls.Skip && eastFree) Session.Skip();
        }

        public void Tick(Camera cam, bool eastFree = true)
        {
            if (Tutorial != null) return;
            TickSkip(eastFree);
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
