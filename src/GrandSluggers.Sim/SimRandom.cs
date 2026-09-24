namespace GrandSluggers.Sim;

/// <summary>
/// The sim's own generator: xoshiro256** seeded through SplitMix64. The same seed draws the same numbers on .NET (the CLI,
/// the tests) and on Mono (the Unity window), which <see cref="System.Random"/> does not promise. It derives from
/// <see cref="Random"/> so a helper that takes a <c>Random</c> reads either; the match hands out only these
/// (<see cref="MatchStreams"/>).
/// </summary>
public sealed class SimRandom : Random
{
    ulong _s0, _s1, _s2, _s3;

    /// <summary>How many 64-bit draws this stream has handed out: a probe can see a draw without taking one.</summary>
    public long Draws { get; private set; }

    public SimRandom(ulong seed)
    {
        var sm = seed;
        _s0 = SplitMix(ref sm);
        _s1 = SplitMix(ref sm);
        _s2 = SplitMix(ref sm);
        _s3 = SplitMix(ref sm);
    }

    /// <summary>A named stream of a seed: the same seed and name always give the same stream, and names never share one.</summary>
    public static SimRandom Stream(int seed, string name)
    {
        // FNV-1a over the name, mixed into the seed; SplitMix in the constructor spreads the bits.
        var hash = 14695981039346656037UL;
        foreach (var c in name)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }
        return new SimRandom(unchecked((ulong)(uint)seed * 0x9E3779B97F4A7C15UL) ^ hash);
    }

    static ulong SplitMix(ref ulong state)
    {
        var z = state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

    /// <summary>The next 64 raw bits.</summary>
    public ulong NextULong()
    {
        Draws++;
        var result = Rotl(_s1 * 5, 7) * 9;
        var t = _s1 << 17;
        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = Rotl(_s3, 45);
        return result;
    }

    uint NextUInt() => (uint)(NextULong() >> 32);

    /// <summary>A uniform integer in [0, range), without modulo bias (Lemire).</summary>
    uint Below(uint range)
    {
        var m = (ulong)NextUInt() * range;
        var low = (uint)m;
        if (low < range)
        {
            var threshold = unchecked(0u - range) % range;
            while (low < threshold)
            {
                m = (ulong)NextUInt() * range;
                low = (uint)m;
            }
        }
        return (uint)(m >> 32);
    }

    /// <summary>A uniform double in [0, 1), 53 bits.</summary>
    public override double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

    protected override double Sample() => NextDouble();

    public override int Next() => (int)Below(int.MaxValue);

    public override int Next(int maxValue)
    {
        if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
        return maxValue == 0 ? 0 : (int)Below((uint)maxValue);
    }

    public override int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
        var range = (long)maxValue - minValue;
        return range == 0 ? minValue : (int)(minValue + Below((uint)range));
    }

    public override void NextBytes(byte[] buffer) => NextBytes(buffer.AsSpan());

    public override void NextBytes(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++) buffer[i] = (byte)(NextULong() >> 56);
    }

    /// <summary>A standard normal draw (Box–Muller), two uniforms from this stream.</summary>
    public double Gauss()
    {
        var u1 = 1.0 - NextDouble();
        var u2 = NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}

/// <summary>
/// A match's random streams, split from its one seed by name. A draw on one stream never moves another: a new CPU pitch
/// roll leaves every contact, handling and hazard draw where it was.
/// </summary>
public sealed class MatchStreams
{
    public MatchStreams(int seed)
    {
        PitchAi = SimRandom.Stream(seed, "pitch-ai");
        BatAi = SimRandom.Stream(seed, "bat-ai");
        Contact = SimRandom.Stream(seed, "contact");
        Handling = SimRandom.Stream(seed, "handling");
        Hazard = SimRandom.Stream(seed, "hazard");
    }

    /// <summary>The CPU defense's reads: the pitch, the pickoff, the catcher's release (spec §4.8, §11).</summary>
    public SimRandom PitchAi { get; }

    /// <summary>The CPU offense's reads: the swing, the box, the bunt square, the steal plan (spec §5.9, §11.6).</summary>
    public SimRandom BatAi { get; }

    /// <summary>The bat meeting the ball: the at-bat resolver's draws (spec §5).</summary>
    public SimRandom Contact { get; }

    /// <summary>The gloves and the arms: the fielding resolve, drops, bobbles, deflections and throw quality (spec §8, §10).</summary>
    public SimRandom Handling { get; }

    /// <summary>What a park hazard does (FD-08): which exit it picks, where it sends the ball.</summary>
    public SimRandom Hazard { get; }

    /// <summary>Every draw on every stream so far.</summary>
    public long Draws => PitchAi.Draws + BatAi.Draws + Contact.Draws + Handling.Draws + Hazard.Draws;
}
