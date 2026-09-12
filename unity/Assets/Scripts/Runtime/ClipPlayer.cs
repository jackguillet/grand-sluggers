using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Two-slot clip mixer in manual update. The sim owns every clock: each
    /// tick sets the current clip's time explicitly and evaluates once, so a
    /// still-gate snap and a live frame sample the same way. A clip change
    /// crossfades over <c>fade</c> seconds; a zero fade cuts.
    /// </summary>
    public sealed class ClipPlayer : System.IDisposable
    {
        PlayableGraph _graph;
        AnimationMixerPlayable _mixer;
        readonly AnimationClipPlayable[] _slots = new AnimationClipPlayable[2];
        readonly AnimationClip[] _clips = new AnimationClip[2];
        int _current = -1;
        float _fadeLeft;
        float _fadeDur;

        public ClipPlayer(Animator animator)
        {
            _graph = PlayableGraph.Create(animator.gameObject.name + "-clips");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            var output = AnimationPlayableOutput.Create(_graph, "body", animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }

        public AnimationClip Current => _current < 0 ? null : _clips[_current];

        /// <summary>Show <paramref name="clip"/> at <paramref name="time"/> seconds.</summary>
        public void Play(AnimationClip clip, float time, float fade)
        {
            if (clip == null) return;
            if (_current < 0 || _clips[_current] != clip)
            {
                var next = _current < 0 ? 0 : 1 - _current;
                Bind(next, clip);
                if (_current >= 0 && fade > 0f)
                {
                    _fadeDur = fade;
                    _fadeLeft = fade;
                }
                else _fadeLeft = 0f;
                _current = next;
            }
            if (fade <= 0f) _fadeLeft = 0f;
            _slots[_current].SetTime(time);
        }

        public void Evaluate(float dt)
        {
            if (_current < 0) return;
            var weight = 1f;
            if (_fadeLeft > 0f)
            {
                _fadeLeft = Mathf.Max(0f, _fadeLeft - Mathf.Max(0f, dt));
                weight = _fadeDur <= 0f ? 1f : Mathf.Clamp01(1f - _fadeLeft / _fadeDur);
            }
            for (var i = 0; i < 2; i++)
            {
                if (!_slots[i].IsValid()) continue;
                _mixer.SetInputWeight(i, i == _current ? weight : 1f - weight);
            }
            _graph.Evaluate(0f);
        }

        void Bind(int slot, AnimationClip clip)
        {
            if (_slots[slot].IsValid())
            {
                _graph.Disconnect(_mixer, slot);
                _slots[slot].Destroy();
            }
            // Never Pause(): a paused playable is not processed, and the mixer
            // then outputs the bind pose. In manual update mode time only
            // advances through Evaluate(0), so nothing plays on its own.
            var playable = AnimationClipPlayable.Create(_graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            playable.SetDuration(clip.length);
            _graph.Connect(playable, 0, _mixer, slot);
            _mixer.SetInputWeight(slot, 0f);
            _slots[slot] = playable;
            _clips[slot] = clip;
        }

        public void Dispose()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }
    }
}
