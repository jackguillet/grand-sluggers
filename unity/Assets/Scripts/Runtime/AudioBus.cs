using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// In-memory SFX. Original tones, never Nintendo samples. Silence is the fallback.
    /// </summary>
    public sealed class AudioBus : MonoBehaviour
    {
        const int Rate = 22050;

        AudioSource _sfx;
        AudioSource _crowd;
        AudioSource _vo;
        AudioClip _crackPerfect, _crackSolid, _crackCheap;
        AudioClip _glove, _throwPop;
        AudioClip _crowdBed, _crowdSwell;
        AudioClip _banana, _rocket, _pow;
        AudioClip _rio, _vale, _zig, _brondo, _konga, _ashlord, _guest;
        float _swell;

        public void Build()
        {
            _sfx = Src("Sfx", 0.9f, false);
            _crowd = Src("Crowd", 0.1f, true);
            _vo = Src("Vo", 0.85f, false);

            _crackPerfect = Crack("BatPerfect", 2400f, 0.55f, 1f, 0.09f);
            _crackSolid = Crack("BatSolid", 1400f, 0.4f, 0.78f, 0.08f);
            _crackCheap = Crack("BatCheap", 280f, 0.22f, 0.5f, 0.07f);
            _glove = Pop("Glove", 190f, 0.045f, 0.7f);
            _throwPop = Pop("Throw", 420f, 0.055f, 0.62f);
            _crowdBed = Crowd("CrowdBed", 1.6f, 0.28f, 0);
            _crowdSwell = Crowd("CrowdSwell", 1.1f, 0.55f, 1);
            _banana = Slap("Banana", 210f);
            _rocket = Whoosh("Rocket");
            _pow = Pop("Pow", 90f, 0.08f, 0.95f);
            _rio = Chord("RioVo", 880f, 1108f, 0.22f);
            _vale = Chord("ValeVo", 659f, 784f, 0.28f);
            _zig = Arp("ZigVo", 988f, 1174f, 1568f, 0.2f);
            _brondo = Chord("BrondoVo", 196f, 294f, 0.3f);
            _konga = Drum("KongaVo");
            _ashlord = Chord("AshlordVo", 233f, 329f, 0.26f);
            _guest = Chord("GuestVo", 440f, 554f, 0.18f);
        }

        public void Tick(float dt)
        {
            _swell = Mathf.MoveTowards(_swell, 0f, dt * 0.42f);
            if (_crowd != null && _crowd.isPlaying)
                _crowd.volume = 0.09f + _swell * 0.32f;
        }

        public void CrowdBed(bool on)
        {
            if (_crowd == null || _crowdBed == null) return;
            if (!on)
            {
                _crowd.Stop();
                _swell = 0;
                return;
            }
            if (_crowd.isPlaying && _crowd.clip == _crowdBed) return;
            _crowd.clip = _crowdBed;
            _crowd.loop = true;
            _crowd.volume = 0.09f;
            _crowd.Play();
        }

        public void Swell()
        {
            _swell = 1f;
            if (_sfx != null && _crowdSwell != null)
                _sfx.PlayOneShot(_crowdSwell, 0.55f);
        }

        public void Bat(ContactQuality quality)
        {
            if (_sfx == null) return;
            if (quality == ContactQuality.Perfect) _sfx.PlayOneShot(_crackPerfect, 1f);
            else if (quality == ContactQuality.Solid) _sfx.PlayOneShot(_crackSolid, 0.85f);
            else if (quality == ContactQuality.Cheap) _sfx.PlayOneShot(_crackCheap, 0.7f);
        }

        public void Glove()
        {
            if (_sfx != null && _glove != null) _sfx.PlayOneShot(_glove, 0.8f);
        }

        public void ThrowPop()
        {
            if (_sfx != null && _throwPop != null) _sfx.PlayOneShot(_throwPop, 0.75f);
        }

        public void CaptainVo(string id)
        {
            if (_vo == null) return;
            var clip = id switch
            {
                "rio" => _rio,
                "vale" => _vale,
                "zig" => _zig,
                "brondo" => _brondo,
                "konga" => _konga,
                "ashlord" => _ashlord,
                _ => _guest
            };
            if (clip != null) _vo.PlayOneShot(clip, 0.9f);
        }

        public void Item(string id)
        {
            if (_sfx == null) return;
            if (id == "banana") _sfx.PlayOneShot(_banana, 0.85f);
            else if (id == "rocket") _sfx.PlayOneShot(_rocket, 0.9f);
            else if (id == "pow") _sfx.PlayOneShot(_pow, 1f);
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
