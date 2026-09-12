using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// #223: the slots shipped and the product still sounded generated, so "a wav
/// is in the slot" is not the bar. These measure the clips as sound. An impact
/// has a transient and lives in many bands at once; a beep is one frequency
/// held for a while. Written to fail the *next* regeneration that goes back to
/// sine + decay — the clips #223 filed against fail every threshold here.
/// </summary>
public class AuthoredHitTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    Clip Load(string id)
    {
        Assert.True(AuthoredAudio.TryLoad(_content.Root, id, out var pcm, out var rate), id);
        return new Clip(pcm, rate);
    }

    [Theory]
    [InlineData("bat-perfect")]
    [InlineData("bat-solid")]
    [InlineData("bat-cheap")]
    [InlineData("glove")]
    [InlineData("throw")]
    public void HitsAreImpactsNotTones(string id)
    {
        var clip = Load(id);
        Assert.True(clip.Seconds < 0.35, $"{id} rings for {clip.Seconds:0.00}s");
        Assert.True(clip.AttackMs < 5.0, $"{id} takes {clip.AttackMs:0.0}ms to reach its peak");
        Assert.True(clip.Crest > 6.0,
            $"{id} crest {clip.Crest:0.0}: level all the way through is a tone, not a hit");
        Assert.True(clip.BiggestOctave < 0.65,
            $"{id} puts {clip.BiggestOctave:0.00} of its energy in one octave — that is a beep");
        Assert.True(clip.Pitched < 0.55,
            $"{id} repeats itself at a pitch ({clip.Pitched:0.00}) — that is a beep");
    }

    [Fact]
    public void ContactTiersAreThreeEventsNotThreeVolumes()
    {
        var perfect = Load("bat-perfect");
        var solid = Load("bat-solid");
        var cheap = Load("bat-cheap");

        // Perfect cracks, solid knocks, cheap jams. A player learns the tier by
        // where it sits in the spectrum, so the ladder has to be wide. Weighted
        // by ear: the question is whether a human can tell them apart.
        Assert.True(perfect.Centroid > solid.Centroid * 1.2,
            $"perfect {perfect.Centroid:0} Hz vs solid {solid.Centroid:0} Hz");
        Assert.True(solid.Centroid > cheap.Centroid * 1.2,
            $"solid {solid.Centroid:0} Hz vs cheap {cheap.Centroid:0} Hz");

        // The crack is the energy above 1 kHz. A mishit is mostly dead wood.
        Assert.True(perfect.HighEnergy > 0.5, $"perfect high {perfect.HighEnergy:0.00}");
        Assert.True(cheap.HighEnergy < 0.3, $"cheap high {cheap.HighEnergy:0.00}");
        Assert.True(perfect.Peak > cheap.Peak, "a perfect hit is also the loudest");
    }

    [Fact]
    public void GloveIsAPopNotABoom()
    {
        var glove = Load("glove");
        Assert.True(glove.Seconds < 0.2, $"glove holds for {glove.Seconds:0.00}s");
        Assert.True(glove.AttackMs < 2.0, $"glove takes {glove.AttackMs:0.0}ms to land");
        // The slap has to be in it. All bottom and no slap is a thud in a mitt.
        Assert.True(glove.MidEnergy > 0.3, $"glove mid {glove.MidEnergy:0.00} — no leather");
        Assert.True(glove.Centroid > Load("bat-cheap").Centroid * 0.8,
            "a catch must not read darker than a jam shot");
    }

    [Fact]
    public void CrowdBedIsAParkNotALoopedSine()
    {
        var bed = Load("crowd-bed");
        Assert.True(bed.Seconds >= 4.0,
            $"bed loops every {bed.Seconds:0.0}s — short enough to hear it come round");
        Assert.True(bed.Crest < 5.5, $"bed crest {bed.Crest:0.0}: a bed has no transient");
        Assert.True(bed.Pitched < 0.25, $"bed hums at a pitch ({bed.Pitched:0.00})");
        Assert.True(bed.BiggestOctave < 0.55, $"bed is one octave wide ({bed.BiggestOctave:0.00})");
        // People, not tape hiss and not a rumble.
        Assert.True(bed.MidEnergy > 0.5, $"bed mid {bed.MidEnergy:0.00} — that is not a crowd");
    }

    [Fact]
    public void CrowdBedLoopsWithoutASeam()
    {
        var bed = Load("crowd-bed");
        var pcm = bed.Samples;
        var wrap = Math.Abs(pcm[^1] - pcm[0]);
        var biggest = 0f;
        for (var i = 1; i < pcm.Length; i++)
            biggest = Math.Max(biggest, Math.Abs(pcm[i] - pcm[i - 1]));
        Assert.True(wrap < biggest * 0.5,
            $"the wrap steps {wrap:0.000}, the loudest step inside the loop is {biggest:0.000}");

        var window = bed.Rate / 20;
        double Rms(int from)
        {
            double e = 0;
            for (var i = 0; i < window; i++) e += pcm[from + i] * (double)pcm[from + i];
            return Math.Sqrt(e / window);
        }
        var head = Rms(0);
        var tail = Rms(pcm.Length - window);
        Assert.True(Math.Abs(head - tail) < head * 0.35,
            $"the bed steps in level at the seam: head {head:0.000} tail {tail:0.000}");
    }

    /// <summary>
    /// One clip, measured through an octave filter bank — the same biquads the
    /// bake script uses, so the numbers here mean what they mean there.
    /// </summary>
    sealed class Clip
    {
        static readonly (double Lo, double Hi)[] Octaves =
        {
            (60, 125), (125, 250), (250, 500), (500, 1000),
            (1000, 2000), (2000, 4000), (4000, 8000), (8000, 16000),
        };

        readonly double[] _flat = new double[Octaves.Length];

        public Clip(float[] samples, int rate)
        {
            Samples = samples;
            Rate = rate;

            double energy = 0;
            foreach (var s in samples)
            {
                energy += s * (double)s;
                Peak = Math.Max(Peak, Math.Abs(s));
            }
            var rms = Math.Sqrt(energy / Math.Max(1, samples.Length));
            Crest = rms > 0 ? Peak / rms : 0;

            var at = 0;
            while (at < samples.Length && Math.Abs(samples[at]) < Peak * 0.9) at++;
            AttackMs = at / (double)rate * 1000.0;

            double weighted = 0, moment = 0;
            for (var b = 0; b < Octaves.Length; b++)
            {
                var (lo, hi) = Octaves[b];
                if (hi > rate / 2.0) continue;
                var centre = Math.Sqrt(lo * hi);
                var band = Band(samples, rate, centre, centre / (hi - lo));
                _flat[b] = band;
                // Loudness weighting for "can a player tell these apart"; raw
                // energy for "does one frequency own the waveform".
                var a = AWeight(centre);
                weighted += band * a * a;
                moment += band * a * a * centre;
            }
            Centroid = weighted > 0 ? moment / weighted : 0;

            var total = _flat.Sum();
            BiggestOctave = total > 0 ? _flat.Max() / total : 1;
            HighEnergy = Fraction(1000, 16000);
            MidEnergy = Fraction(250, 2000);
            Pitched = Repeat(samples, rate, energy);
        }

        public float[] Samples { get; }
        public int Rate { get; }
        public double Seconds => Samples.Length / (double)Rate;
        public double Peak { get; }
        public double Crest { get; }
        public double AttackMs { get; }
        public double Centroid { get; }
        public double BiggestOctave { get; }
        public double HighEnergy { get; }
        public double MidEnergy { get; }
        public double Pitched { get; }

        double Fraction(double from, double to)
        {
            double part = 0, all = 0;
            for (var b = 0; b < Octaves.Length; b++)
            {
                all += _flat[b];
                if (Octaves[b].Lo >= from && Octaves[b].Hi <= to) part += _flat[b];
            }
            return all > 0 ? part / all : 0;
        }

        /// <summary>Energy in one octave: an RBJ bandpass run twice for skirts.</summary>
        static double Band(float[] s, int rate, double centre, double q)
        {
            var w = 2.0 * Math.PI * centre / rate;
            var al = Math.Sin(w) / (2.0 * q);
            var c = Math.Cos(w);
            var a0 = 1.0 + al;
            double b0 = al / a0, b2 = -al / a0, a1 = -2.0 * c / a0, a2 = (1.0 - al) / a0;

            var pass = new double[s.Length];
            for (var i = 0; i < s.Length; i++) pass[i] = s[i];
            for (var round = 0; round < 2; round++)
            {
                double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                for (var i = 0; i < pass.Length; i++)
                {
                    var x0 = pass[i];
                    var y = b0 * x0 + b2 * x2 - a1 * y1 - a2 * y2;
                    x2 = x1; x1 = x0;
                    y2 = y1; y1 = y;
                    pass[i] = y;
                }
            }
            double e = 0;
            foreach (var v in pass) e += v * v;
            return e;
        }

        /// <summary>
        /// How much of the clip repeats at some pitch between 60 and 600 Hz.
        /// A held tone scores near 1; an impact does not repeat at all.
        /// </summary>
        static double Repeat(float[] s, int rate, double energy)
        {
            if (energy <= 0) return 0;
            var best = 0.0;
            for (var lag = rate / 600; lag < rate / 60 && lag < s.Length; lag++)
            {
                double sum = 0;
                for (var i = 0; i + lag < s.Length; i++) sum += s[i] * (double)s[i + lag];
                best = Math.Max(best, sum / energy);
            }
            return best;
        }

        static double AWeight(double f)
        {
            var f2 = f * f;
            var ra = 12194.0 * 12194.0 * f2 * f2
                / ((f2 + 20.6 * 20.6)
                   * Math.Sqrt((f2 + 107.7 * 107.7) * (f2 + 737.9 * 737.9))
                   * (f2 + 12194.0 * 12194.0));
            return Math.Pow(10, 2.0 / 20.0) * ra;
        }
    }
}
