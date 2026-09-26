using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Catalog audio buses. Authored wavs in data/art/audio-clips fill the slot.
    /// Missing file keeps the generated tone. Never Nintendo samples.
    /// </summary>
    public sealed class AudioBus : MonoBehaviour
    {
        const int Rate = 22050;
        // A park never plays the same sound twice. One wav fired identically on
        // every pitch is the tell, so every sfx one-shot takes its own voice and
        // its own small detune. Not per event: every play type gets it.
        const int Shots = 6;
        const float Jitter = 0.045f;

        AudioSource[] _shots;
        int _shot;
        AudioSource _sfx;
        AudioSource _crowd;
        AudioSource _vo;
        readonly Dictionary<string, AudioClip> _tone = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, AudioClip> _authored = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
        float _swell;

        public void Build(string dataRoot)
        {
            _sfx = Src("Sfx", 0.9f, false);
            _crowd = Src("Crowd", 0.1f, true);
            _vo = Src("Vo", 0.85f, false);
            _shots = new AudioSource[Shots];
            for (var i = 0; i < Shots; i++) _shots[i] = Src("Shot" + i, 0.9f, false);

            Tone("bat-perfect", Crack("BatPerfect", 2400f, 0.55f, 1f, 0.09f));
            Tone("bat-solid", Crack("BatSolid", 1400f, 0.4f, 0.78f, 0.08f));
            Tone("bat-cheap", Crack("BatCheap", 280f, 0.22f, 0.5f, 0.07f));
            Tone("glove", Pop("Glove", 190f, 0.045f, 0.7f));
            Tone("throw", Pop("Throw", 420f, 0.055f, 0.62f));
            Tone("crowd-bed", Crowd("CrowdBed", 1.6f, 0.28f, 0));
            Tone("crowd-swell", Crowd("CrowdSwell", 1.1f, 0.55f, 1));
            Tone("banana", Slap("Banana", 210f));
            Tone("rocket", Whoosh("Rocket"));
            Tone("pow", Pop("Pow", 90f, 0.08f, 0.95f));
            Tone("vo-rio", Chord("RioVo", 880f, 1108f, 0.22f));
            Tone("vo-vale", Chord("ValeVo", 659f, 784f, 0.28f));
            Tone("vo-zig", Arp("ZigVo", 988f, 1174f, 1568f, 0.2f));
            Tone("vo-brondo", Chord("BrondoVo", 196f, 294f, 0.3f));
            Tone("vo-konga", Drum("KongaVo"));
            Tone("vo-ashlord", Chord("AshlordVo", 233f, 329f, 0.26f));
            Tone("vo-guest", Chord("GuestVo", 440f, 554f, 0.18f));
            LoadAuthored(dataRoot);
        }

        public void Play(string eventId, float volume = 1f)
        {
            var clip = Resolve(eventId);
            var src = SourceFor(eventId);
            if (clip == null || src == null) return;
            if (src == _sfx && _shots != null)
            {
                // Round robin so a quick catch never cuts off the crack.
                src = _shots[_shot];
                _shot = (_shot + 1) % _shots.Length;
                src.pitch = 1f + Random.Range(-Jitter, Jitter);
                volume *= 1f - Random.Range(0f, Jitter * 2f);
            }
            src.PlayOneShot(clip, volume);
        }

        public void Tick(float dt)
        {
            _swell = Mathf.MoveTowards(_swell, 0f, dt * 0.42f);
            if (_crowd != null && _crowd.isPlaying)
                _crowd.volume = 0.09f + _swell * 0.32f;
        }

        public void CrowdBed(bool on)
        {
            if (_crowd == null) return;
            if (!on)
            {
                _crowd.Stop();
                _swell = 0;
                return;
            }
            var clip = Resolve("crowd-bed");
            if (clip == null) return;
            if (_crowd.isPlaying && _crowd.clip == clip) return;
            _crowd.clip = clip;
            _crowd.loop = true;
            _crowd.volume = 0.09f;
            _crowd.Play();
        }

        public void Swell()
        {
            _swell = 1f;
            Play("crowd-swell", 0.55f);
        }

        public void Bat(ContactQuality quality)
        {
            if (quality == ContactQuality.Perfect) Play("bat-perfect", 1f);
            else if (quality == ContactQuality.Nice) Play("bat-solid", 0.85f);
            else if (quality == ContactQuality.Sour) Play("bat-cheap", 0.7f);
        }

        public void Glove() => Play("glove", 0.8f);

        public void ThrowPop() => Play("throw", 0.75f);

        public void CaptainVo(string id)
        {
            var ev = "vo-" + (id ?? "");
            if (_tone.ContainsKey(ev) || ArtBinder.AudioPath(ev).Length > 0)
                Play(ev, 0.9f);
            else
                Play("vo-guest", 0.9f);
        }

        public void Item(string id)
        {
            if (id == "banana") Play("banana", 0.85f);
            else if (id == "rocket") Play("rocket", 0.9f);
            else if (id == "pow") Play("pow", 1f);
        }

        /// <summary>
        /// A special's tell cue (<c>vfx.json</c> <c>cue</c>, a slot in <c>audio.json</c>). Until its wav is authored the stand-in is
        /// the generated tone its last word names (whistle, clack, clang, thunder, hiss, bell, croak): a new cue with one of those
        /// words sounds at once, and an authored file replaces it without code.
        /// </summary>
        public void Cue(string cueId, float volume = 0.8f)
        {
            if (string.IsNullOrEmpty(cueId)) return;
            if (Resolve(cueId) == null)
            {
                var word = cueId.Substring(cueId.LastIndexOf('-') + 1);
                var clip = StandIn(word);
                if (clip == null) return;
                _tone[cueId] = clip;
            }
            Play(cueId, volume);
        }

        static AudioClip StandIn(string word)
        {
            switch (word)
            {
                case "whistle": return Whistle("Whistle");
                case "clack": return Clack("Clack");
                case "clang": return Ring("Clang", 523f, 1307f, 2213f, 0.9f, 4f);
                case "bell": return Ring("Bell", 880f, 2200f, 3520f, 0.8f, 3f);
                case "thunder": return Thunder("Thunder");
                case "hiss": return Hiss("Hiss");
                case "croak": return Croak("Croak");
                default: return null;
            }
        }

        static AudioClip Whistle(string name)
        {
            const float dur = 0.45f;
            var phase = 0f;
            return Clip(name, dur, (i, t) =>
            {
                var f = 900f + 1300f * (t / dur) + 40f * Mathf.Sin(t * 60f);
                phase += 2f * Mathf.PI * f / Rate;
                var env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur));
                return Mathf.Clamp(0.5f * env * Mathf.Sin(phase), -1f, 1f);
            });
        }

        static AudioClip Clack(string name)
        {
            return Clip(name, 0.14f, (i, t) =>
            {
                var a = Mathf.Exp(-t * 90f);
                var b = t > 0.07f ? Mathf.Exp(-(t - 0.07f) * 90f) : 0f;
                return Mathf.Clamp(0.8f * (a + b) * (Hash(i) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.4f), -1f, 1f);
            });
        }

        static AudioClip Ring(string name, float a, float b, float c, float dur, float decay)
        {
            return Clip(name, dur, (i, t) =>
            {
                var env = Mathf.Exp(-t * decay);
                var mix = Mathf.Sin(2f * Mathf.PI * a * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * b * t) * 0.3f
                    + Mathf.Sin(2f * Mathf.PI * c * t) * 0.2f * Mathf.Exp(-t * decay * 2f);
                var strike = t < 0.006f ? Hash(i) * 0.5f : 0f;
                return Mathf.Clamp(0.7f * env * mix + strike, -1f, 1f);
            });
        }

        static AudioClip Thunder(string name)
        {
            const float dur = 1.2f;
            var low = 0f;
            return Clip(name, dur, (i, t) =>
            {
                low += (Hash(i) - low) * 0.02f;
                var env = Mathf.Min(1f, t / 0.05f) * Mathf.Exp(-t * 2.2f) * (0.7f + 0.3f * Mathf.Sin(t * 17f));
                return Mathf.Clamp(6f * low * env, -1f, 1f);
            });
        }

        static AudioClip Hiss(string name)
        {
            const float dur = 0.5f;
            var prev = 0f;
            return Clip(name, dur, (i, t) =>
            {
                var n = Hash(i);
                var high = n - prev;
                prev = n;
                var env = Mathf.Min(1f, t / 0.03f) * (1f - t / dur);
                return Mathf.Clamp(0.35f * env * high, -1f, 1f);
            });
        }

        static AudioClip Croak(string name)
        {
            return Clip(name, 0.32f, (i, t) =>
            {
                var pulse = Mathf.Sin(2f * Mathf.PI * 28f * t) > 0.2f ? 1f : 0.15f;
                var tone = Mathf.Sin(2f * Mathf.PI * 115f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 230f * t);
                var env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.32f));
                return Mathf.Clamp(0.55f * env * pulse * tone, -1f, 1f);
            });
        }

        AudioClip Resolve(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;
            var file = ArtBinder.LoadAudio(eventId);
            if (file != null) return file;
            if (_authored.TryGetValue(eventId, out var wav)) return wav;
            _tone.TryGetValue(eventId, out var tone);
            return tone;
        }

        AudioSource SourceFor(string eventId)
        {
            if (string.Equals(eventId, "crowd-swell", System.StringComparison.OrdinalIgnoreCase))
                return _sfx;
            var bus = ArtBinder.AudioBusOf(eventId);
            if (string.Equals(bus, "crowd", System.StringComparison.OrdinalIgnoreCase))
                return _crowd != null ? _crowd : _sfx;
            if (string.Equals(bus, "vo", System.StringComparison.OrdinalIgnoreCase))
                return _vo != null ? _vo : _sfx;
            return _sfx;
        }

        void LoadAuthored(string dataRoot)
        {
            if (string.IsNullOrEmpty(dataRoot)) return;
            foreach (var id in AuthoredAudio.Ids(dataRoot))
            {
                if (!AuthoredAudio.TryLoad(dataRoot, id, out var pcm, out var rate)) continue;
                if (pcm == null || pcm.Length < 8 || rate < 8000) continue;
                var clip = AudioClip.Create(id, pcm.Length, 1, rate, false);
                clip.SetData(pcm, 0);
                _authored[id] = clip;
            }
        }

        void Tone(string id, AudioClip clip)
        {
            if (clip != null) _tone[id] = clip;
        }

        AudioSource Src(string name, float vol, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.loop = loop;
            a.volume = vol;
            a.spatialBlend = 0f;
            return a;
        }

        static AudioClip Crack(string name, float freq, float noise, float amp, float dur)
        {
            return Clip(name, dur, (i, t) =>
            {
                var env = Mathf.Exp(-t * 32f);
                var click = t < 0.004f ? (1f - t / 0.004f) * Hash(i) * 0.7f : 0f;
                var tone = Mathf.Sin(2f * Mathf.PI * freq * t * (1f + t * 0.4f));
                return Mathf.Clamp(amp * env * (tone * (1f - noise) + Hash(i + 3) * noise) + click, -1f, 1f);
            });
        }

        static AudioClip Pop(string name, float freq, float dur, float amp)
        {
            return Clip(name, dur, (i, t) =>
            {
                var env = Mathf.Exp(-t * 48f);
                return Mathf.Clamp(amp * env * (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Hash(i) * 0.3f), -1f, 1f);
            });
        }

        static AudioClip Slap(string name, float freq)
        {
            return Clip(name, 0.12f, (i, t) =>
            {
                var env = Mathf.Exp(-t * 22f);
                return Mathf.Clamp(0.7f * env * (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f + Hash(i) * 0.6f), -1f, 1f);
            });
        }

        static AudioClip Whoosh(string name)
        {
            return Clip(name, 0.22f, (i, t) =>
            {
                var env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.22f));
                var f = 400f + t * 1600f;
                return Mathf.Clamp(0.55f * env * (Mathf.Sin(2f * Mathf.PI * f * t) * 0.25f + Hash(i) * 0.75f), -1f, 1f);
            });
        }

        static AudioClip Crowd(string name, float dur, float amp, int seed)
        {
            return Clip(name, dur, (i, t) =>
            {
                var n = Hash(i + seed * 97) * 0.55f + Hash(i * 3 + 11 + seed) * 0.3f;
                var wobble = 0.65f + 0.35f * Mathf.Sin(t * 7.4f + Hash((int)(t * 12f) + seed) * 0.8f);
                return Mathf.Clamp(amp * n * wobble * 0.45f, -1f, 1f);
            });
        }

        static AudioClip Chord(string name, float a, float b, float dur)
        {
            return Clip(name, dur, (i, t) =>
            {
                var env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur));
                var mix = Mathf.Sin(2f * Mathf.PI * a * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * b * t) * 0.5f;
                return Mathf.Clamp(0.7f * env * mix, -1f, 1f);
            });
        }

        static AudioClip Arp(string name, float a, float b, float c, float dur)
        {
            return Clip(name, dur, (i, t) =>
            {
                var u = Mathf.Clamp01(t / dur);
                var f = u < 0.33f ? a : u < 0.66f ? b : c;
                var env = Mathf.Sin(Mathf.PI * ((u * 3f) % 1f)) * (1f - u * 0.25f);
                return Mathf.Clamp(0.72f * env * Mathf.Sin(2f * Mathf.PI * f * t), -1f, 1f);
            });
        }

        static AudioClip Drum(string name)
        {
            return Clip(name, 0.24f, (i, t) =>
            {
                var env = Mathf.Exp(-t * 18f);
                var tone = Mathf.Sin(2f * Mathf.PI * (90f + t * 40f) * t);
                var click = t < 0.01f ? Hash(i) * 0.8f : 0f;
                return Mathf.Clamp(0.85f * env * tone + click * 0.4f, -1f, 1f);
            });
        }

        static AudioClip Clip(string name, float seconds, System.Func<int, float, float> wave)
        {
            var n = Mathf.Max(64, (int)(Rate * seconds));
            var data = new float[n];
            for (var i = 0; i < n; i++)
                data[i] = wave(i, i / (float)Rate);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Hash(int i)
        {
            var n = (uint)(i * 16777619);
            n ^= n >> 13;
            n *= 1274126177u;
            return (n & 0xFFFF) / 32768f - 1f;
        }
    }
}
