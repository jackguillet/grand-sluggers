#!/usr/bin/env python3
"""Original Grand Sluggers hits. No Nintendo samples. No licensed music.

A hit is an *impact*, not a note: a sub-millisecond contact burst that rings a
bank of body modes, inside a park. Sine + decay is a beep, which is what #223
filed. Everything here is noise-excited modal synthesis plus a room.

    python3 bake.py                  # bake the shipped slots
    python3 bake.py --only glove     # one slot
    python3 bake.py --candidates DIR # bat-crack voices for a human to pick

Human picks the bat crack: audition the candidates, set BAT_VOICE, re-bake.
"""
from __future__ import annotations

import argparse
import math
import os
import random
import struct
import wave

RATE = 44100        # hits: transient detail lives above 10 kHz
BED_RATE = 22050    # crowd bed: no content up there, so buy loop length instead
DIR = os.path.dirname(os.path.abspath(__file__))

# Which barrel voice ships. Human gate: bake --candidates, listen, set this.
BAT_VOICE = "ash"


# ---------------------------------------------------------------- primitives

def clamp(x: float, lo: float = -1.0, hi: float = 1.0) -> float:
    return lo if x < lo else hi if x > hi else x


def noise(n: int, seed: int) -> list[float]:
    rng = random.Random(seed)
    return [rng.uniform(-1.0, 1.0) for _ in range(n)]


def mix(into: list[float], src: list[float], gain: float = 1.0) -> list[float]:
    for i in range(min(len(into), len(src))):
        into[i] += src[i] * gain
    return into


def burst(n: int, rate: int, seconds: float, seed: int, tau: float | None = None) -> list[float]:
    """Contact excitation: a noise slice short enough to read as one impact."""
    m = max(1, int(rate * seconds))
    tau = tau if tau is not None else seconds * 0.35
    w = noise(m, seed)
    out = [0.0] * n
    for i in range(min(m, n)):
        out[i] = w[i] * math.exp(-(i / rate) / tau)
    return out


def biquad(x: list[float], b0: float, b1: float, b2: float, a1: float, a2: float) -> list[float]:
    out = [0.0] * len(x)
    x1 = x2 = y1 = y2 = 0.0
    for i, s in enumerate(x):
        y = b0 * s + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        out[i] = y
        x2, x1 = x1, s
        y2, y1 = y1, y
    return out


def lowpass(x: list[float], rate: int, freq: float, q: float = 0.707) -> list[float]:
    w = 2.0 * math.pi * min(freq, rate * 0.45) / rate
    al = math.sin(w) / (2.0 * q)
    c = math.cos(w)
    a0 = 1.0 + al
    return biquad(x, (1 - c) / 2 / a0, (1 - c) / a0, (1 - c) / 2 / a0,
                  -2 * c / a0, (1 - al) / a0)


def highpass(x: list[float], rate: int, freq: float, q: float = 0.707) -> list[float]:
    w = 2.0 * math.pi * min(freq, rate * 0.45) / rate
    al = math.sin(w) / (2.0 * q)
    c = math.cos(w)
    a0 = 1.0 + al
    return biquad(x, (1 + c) / 2 / a0, -(1 + c) / a0, (1 + c) / 2 / a0,
                  -2 * c / a0, (1 - al) / a0)


def bandpass(x: list[float], rate: int, freq: float, q: float) -> list[float]:
    w = 2.0 * math.pi * min(freq, rate * 0.45) / rate
    al = math.sin(w) / (2.0 * q)
    c = math.cos(w)
    a0 = 1.0 + al
    return biquad(x, al / a0, 0.0, -al / a0, -2 * c / a0, (1 - al) / a0)


def resonate(x: list[float], rate: int, modes: list[tuple[float, float, float]]) -> list[float]:
    """Ring a bank of decaying body modes: (hz, decay seconds, gain).

    Two-pole resonator per mode, each normalised to its own peak so `gain` is
    the amplitude you hear. Without that a narrow low mode integrates the
    exciter and buries the crack under a boom. Excited by noise this is a
    struck object; excited by nothing it is silence. It cannot hold a tone.
    """
    out = [0.0] * len(x)
    for freq, tau, gain in modes:
        if freq >= rate * 0.48 or gain == 0.0:
            continue
        w = 2.0 * math.pi * freq / rate
        r = math.exp(-1.0 / max(1e-6, tau) / rate)
        a1 = 2.0 * r * math.cos(w)
        a2 = -r * r
        mode = [0.0] * len(x)
        y1 = y2 = 0.0
        for i, s in enumerate(x):
            y = s + a1 * y1 + a2 * y2
            y2, y1 = y1, y
            mode[i] = y
        top = max((abs(y) for y in mode), default=0.0) or 1.0
        k = gain / top
        for i in range(len(out)):
            out[i] += mode[i] * k
    return out


def soft(x: list[float], drive: float) -> list[float]:
    """Round the peak only. Driving an unnormalised signal compresses the
    transient away, and a hit without a transient is a tone."""
    top = max((abs(s) for s in x), default=0.0) or 1.0
    k = math.tanh(drive) or 1.0
    return [math.tanh(s / top * drive) / k for s in x]


def delay_line(x: list[float], samples: int, feedback: float) -> list[float]:
    out = list(x)
    for i in range(samples, len(out)):
        out[i] += out[i - samples] * feedback
    return out


def allpass(x: list[float], samples: int, g: float) -> list[float]:
    out = [0.0] * len(x)
    buf = [0.0] * samples
    p = 0
    for i, s in enumerate(x):
        d = buf[p]
        v = s + d * g
        out[i] = d - v * g
        buf[p] = v
        p = (p + 1) % samples
    return out


def room(x: list[float], rate: int, t60: float, predelay: float = 0.0) -> list[float]:
    """Schroeder park. Without it every hit sounds like it happened in a DAW."""
    pre = int(rate * predelay)
    src = [0.0] * pre + x if pre else list(x)
    wet = [0.0] * len(src)
    for ms in (29.7, 37.1, 41.1, 43.7):
        d = max(1, int(rate * ms / 1000.0))
        fb = 10.0 ** (-3.0 * (ms / 1000.0) / max(0.05, t60))
        mix(wet, delay_line(src, d, min(0.92, fb)), 0.25)
    wet = allpass(wet, max(1, int(rate * 0.005)), 0.7)
    wet = allpass(wet, max(1, int(rate * 0.0017)), 0.7)
    return wet[:len(x)] if len(wet) >= len(x) else wet + [0.0] * (len(x) - len(wet))


def wet_mix(dry: list[float], rate: int, amount: float, t60: float, predelay: float) -> list[float]:
    if amount <= 0.0:
        return dry
    return mix(list(dry), room(dry, rate, t60, predelay), amount)


# ---------------------------------------------------------------- bat cracks

# Barrel voices. A wood bat's crack is its bending / hoop modes: a low body
# mode near 170 Hz and a bright cluster from 1-6 kHz that dies inside 60 ms.
# Three woods, three characters. The human picks one; the tiers share it.
BAT_VOICES: dict[str, dict] = {
    "ash": {
        "what": "Classic wood. Mid-forward crack, the sound of a clean line drive.",
        "modes": [(172, 0.018, 0.38), (562, 0.014, 0.40), (1180, 0.011, 0.72),
                  (2350, 0.009, 1.00), (3900, 0.006, 0.78), (5600, 0.004, 0.40)],
        "sizzle": 3600.0,
        "drive": 1.5,
    },
    "maple": {
        "what": "Harder and brighter. More ping on top, longer carry.",
        "modes": [(196, 0.016, 0.30), (640, 0.013, 0.36), (1420, 0.013, 0.78),
                  (2750, 0.011, 1.00), (4600, 0.008, 0.92), (6800, 0.005, 0.52)],
        "sizzle": 4800.0,
        "drive": 1.8,
    },
    "hickory": {
        "what": "Heavy and woody. Deeper thump under the crack, less top.",
        "modes": [(146, 0.024, 0.58), (470, 0.018, 0.50), (980, 0.012, 0.64),
                  (1950, 0.008, 0.88), (3300, 0.005, 0.52), (4800, 0.003, 0.24)],
        "sizzle": 2900.0,
        "drive": 1.3,
    },
}

# Contact tiers. Learnable means three different *events*, not three volumes:
# a crack, a knock, and a jam. Damping shortens the top modes as contact gets
# worse and `tilt` takes the top off the whole hit, so the ladder holds for any
# barrel the human picks rather than for the one that happened to be tuned.
BAT_TIERS: dict[str, dict] = {
    "perfect": {"seconds": 0.20, "damp": 1.00, "top": 1.15, "sizzle": 1.25,
                "thump": 0.16, "wet": 0.26, "t60": 1.30, "peak": 0.97, "seed": 7,
                "tilt": 18000.0},
    "solid":   {"seconds": 0.17, "damp": 0.58, "top": 0.46, "sizzle": 0.30,
                "thump": 0.52, "wet": 0.17, "t60": 1.00, "peak": 0.82, "seed": 23,
                "tilt": 2600.0},
    "cheap":   {"seconds": 0.14, "damp": 0.34, "top": 0.30, "sizzle": 0.14,
                "thump": 0.85, "wet": 0.09, "t60": 0.70, "peak": 0.62, "seed": 41,
                "tilt": 1050.0},
}


def bat(voice: str, tier: str) -> tuple[list[float], float]:
    v = BAT_VOICES[voice]
    t = BAT_TIERS[tier]
    n = int(RATE * t["seconds"])
    seed = t["seed"]

    # Ball on wood: contact is under a millisecond, so the exciter is too.
    contact = highpass(burst(n, RATE, 0.0009, seed, tau=0.00035), RATE, 700.0)
    body = burst(n, RATE, 0.004, seed + 1, tau=0.0016)

    modes = []
    for k, (hz, tau, gain) in enumerate(v["modes"]):
        top = t["top"] if k >= 2 else 0.45 + 0.55 * t["damp"]
        modes.append((hz, tau * (t["damp"] if k >= 2 else 1.0), gain * top))
    out = resonate(mix(list(contact), body, 0.6), RATE, modes)

    # The crack itself: a spray of high noise gone in 20 ms.
    sizzle = bandpass(burst(n, RATE, 0.020, seed + 2, tau=0.0045), RATE, v["sizzle"], 0.9)
    mix(out, sizzle, 1.10 * t["sizzle"])

    # Ball squash. Owns a mishit, sits under a crack.
    thump = resonate(burst(n, RATE, 0.006, seed + 3, tau=0.0022), RATE,
                     [(118, 0.012, 1.0), (196, 0.008, 0.55)])
    mix(out, lowpass(thump, RATE, 420.0), 0.55 * t["thump"])

    if tier == "cheap":
        # Jam shot: dead wood, a knock with no top and a short buzz.
        knock = bandpass(burst(n, RATE, 0.012, seed + 4, tau=0.0030), RATE,
                         v["modes"][1][0] * 1.25, 0.7)
        mix(out, knock, 2.60)

    out = soft(out, v["drive"])
    if t["tilt"] < RATE * 0.4:
        out = lowpass(out, RATE, t["tilt"], 0.7)
    out = wet_mix(out, RATE, t["wet"], t["t60"], 0.006)
    return fade_out(out, RATE, 0.02), t["peak"]


# ---------------------------------------------------------------- leather

def glove() -> tuple[list[float], float]:
    """Ball into the pocket: slap on top, a short pop of trapped air under."""
    n = int(RATE * 0.12)
    slap = bandpass(burst(n, RATE, 0.0035, 61, tau=0.0011), RATE, 1900.0, 0.8)
    leather = burst(n, RATE, 0.006, 62, tau=0.0022)
    pocket = resonate(leather, RATE,
                      [(92, 0.012, 0.17), (168, 0.008, 0.28), (340, 0.006, 0.62),
                       (760, 0.004, 0.62)])
    out = mix(list(pocket), slap, 2.40)
    mix(out, lowpass(burst(n, RATE, 0.030, 63, tau=0.0055), RATE, 3200.0), 0.55)
    out = soft(out, 1.4)
    return fade_out(wet_mix(out, RATE, 0.13, 0.8, 0.009), RATE, 0.02), 0.86


def throw_pop() -> tuple[list[float], float]:
    """Release: less body than a catch, a whip of air off the fingers."""
    n = int(RATE * 0.13)
    whip = [0.0] * n
    air = noise(n, 71)
    for i in range(int(RATE * 0.045)):
        u = i / (RATE * 0.045)
        whip[i] = air[i] * math.sin(math.pi * u) ** 2
    whip = bandpass(whip, RATE, 1000.0, 0.6)
    hand = resonate(burst(n, RATE, 0.003, 72, tau=0.0012), RATE,
                    [(215, 0.007, 0.9), (520, 0.005, 0.7), (1150, 0.004, 0.5)])
    out = mix(list(hand), whip, 0.42)
    out = soft(out, 1.3)
    return fade_out(wet_mix(out, RATE, 0.10, 0.7, 0.008), RATE, 0.02), 0.62


# ---------------------------------------------------------------- the park

def crowd_bed() -> tuple[list[float], float]:
    """A park, not a looped sine.

    Many band-limited voices murmuring at their own rate, a low rumble, sparse
    far-off claps, all smeared through a big slow room. Every modulator is
    periodic over the loop and the room runs two laps, so the seam is silent.
    """
    rate = BED_RATE
    seconds = 6.0
    n = int(rate * seconds)
    rng = random.Random(1815)

    dry = [0.0] * n
    for v in range(22):
        freq = 190.0 * (1.9 ** rng.random()) * (1.0 + v * 0.085)
        band = bandpass(noise(n, 300 + v * 17), rate, min(freq, 2600.0), 1.4 + rng.random() * 1.8)
        # Each voice breathes on its own whole number of cycles -> loops clean.
        cycles = rng.randint(2, 11)
        phase = rng.random() * math.tau
        depth = 0.45 + rng.random() * 0.45
        gain = 0.55 + rng.random() * 0.45
        for i in range(n):
            u = i / n
            m = 1.0 - depth + depth * (0.5 + 0.5 * math.sin(math.tau * cycles * u + phase))
            dry[i] += band[i] * m * gain

    air = lowpass(highpass(noise(n, 613), rate, 900.0, 0.7), rate, 3800.0, 0.7)
    mix(dry, air, 0.16)
    rumble = lowpass(noise(n, 977), rate, 170.0, 0.8)
    for i in range(n):
        u = i / n
        dry[i] += rumble[i] * 2.2 * (0.7 + 0.3 * math.sin(math.tau * u))

    # Scattered claps and shouts, far enough back to be texture not an event.
    for _ in range(34):
        at = rng.randrange(n)
        length = int(rate * (0.006 + rng.random() * 0.010))
        amp = 0.25 + rng.random() * 0.5
        hit = bandpass(burst(length, rate, length / rate, rng.randrange(9999), tau=length / rate * 0.3),
                       rate, 900.0 + rng.random() * 2200.0, 1.4)
        for i, s in enumerate(hit):
            dry[(at + i) % n] += s * amp

    # Two laps through the room, keep the second: the tail has wrapped by then.
    wet = room(dry + dry, rate, 2.1, 0.017)[n:]
    out = [0.0] * n
    for i in range(n):
        u = i / n
        swell = 0.86 + 0.14 * math.sin(math.tau * u) + 0.06 * math.sin(math.tau * 3 * u)
        out[i] = (dry[i] * 0.42 + wet[i] * 0.78) * swell
    return out, None


# ---------------------------------------------------------------- output

def fade_out(x: list[float], rate: int, seconds: float) -> list[float]:
    f = min(len(x), int(rate * seconds))
    for i in range(f):
        x[len(x) - f + i] *= 1.0 - i / f
    return x


def write_wav(path: str, samples: list[float], rate: int = RATE,
              peak: float | None = 0.89, rms: float | None = None) -> None:
    if rms is not None:
        level = math.sqrt(sum(s * s for s in samples) / max(1, len(samples))) or 1.0
        samples = [s * (rms / level) for s in samples]
    elif peak is not None:
        top = max((abs(s) for s in samples), default=1.0) or 1.0
        samples = [s * (peak / top) for s in samples]
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(b"".join(struct.pack("<h", int(clamp(s) * 32767)) for s in samples))
    top = max((abs(s) for s in samples), default=0.0)
    print(f"{os.path.basename(path):22} {len(samples) / rate:5.2f}s @{rate}  peak {top:.2f}")


SLOTS = {
    "bat-perfect": lambda: bat(BAT_VOICE, "perfect"),
    "bat-solid": lambda: bat(BAT_VOICE, "solid"),
    "bat-cheap": lambda: bat(BAT_VOICE, "cheap"),
    "glove": glove,
    "throw": throw_pop,
    "crowd-bed": crowd_bed,
}


OCTAVES = [(60, 125), (125, 250), (250, 500), (500, 1000),
           (1000, 2000), (2000, 4000), (4000, 8000), (8000, 16000)]


def a_weight(f: float) -> float:
    f2 = f * f
    ra = (12194.0 ** 2 * f2 * f2) / (
        (f2 + 20.6 ** 2)
        * math.sqrt((f2 + 107.7 ** 2) * (f2 + 737.9 ** 2))
        * (f2 + 12194.0 ** 2))
    return 10.0 ** (2.0 / 20.0) * ra


def centroid(x: list[float], rate: int) -> float:
    """Loudness-weighted octave centroid — the same measure AuthoredHitTests
    uses to check that a player can tell the three contact tiers apart."""
    total = moment = 0.0
    for lo, hi in OCTAVES:
        if hi > rate / 2:
            continue
        mid = math.sqrt(lo * hi)
        band = bandpass(bandpass(x, rate, mid, mid / (hi - lo)), rate, mid, mid / (hi - lo))
        energy = sum(v * v for v in band) * a_weight(mid) ** 2
        total += energy
        moment += energy * mid
    return moment / total if total else 0.0


def crest(x: list[float]) -> float:
    rms = math.sqrt(sum(v * v for v in x) / max(1, len(x)))
    return (max(abs(v) for v in x) / rms) if rms else 0.0


def check() -> int:
    """Every barrel has to keep the ladder, because the human picks the barrel.
    Run this before changing BAT_VOICE or a voice table."""
    bad = 0
    for voice in BAT_VOICES:
        rung = {}
        for tier in BAT_TIERS:
            samples, peak = bat(voice, tier)
            top = max(abs(v) for v in samples) or 1.0
            samples = [v * peak / top for v in samples]
            rung[tier] = (centroid(samples, RATE), crest(samples))
        gaps = (rung["perfect"][0] / rung["solid"][0], rung["solid"][0] / rung["cheap"][0])
        ok = gaps[0] > 1.2 and gaps[1] > 1.2 and all(c > 6.0 for _, c in rung.values())
        bad += 0 if ok else 1
        mark = "ok  " if ok else "FAIL"
        print(f"{mark} {voice:8} " + "  ".join(
            f"{t} {rung[t][0]:5.0f}Hz crest {rung[t][1]:4.1f}" for t in BAT_TIERS)
            + f"   gaps {gaps[0]:.2f}x {gaps[1]:.2f}x")
    print("every voice keeps the ladder" if not bad else f"{bad} voice(s) would fail the gate")
    return 1 if bad else 0


def bake(ids: list[str], out_dir: str) -> None:
    for slot in ids:
        samples, peak = SLOTS[slot]()
        rate = BED_RATE if slot == "crowd-bed" else RATE
        if slot == "crowd-bed":
            write_wav(os.path.join(out_dir, slot + ".wav"), samples, rate, peak=None, rms=0.14)
        else:
            write_wav(os.path.join(out_dir, slot + ".wav"), samples, rate, peak=peak)


def candidates(out_dir: str) -> None:
    """One set per barrel voice, for the human who picks the bat crack."""
    for voice, spec in BAT_VOICES.items():
        print(f"\n{voice}: {spec['what']}")
        for tier in BAT_TIERS:
            samples, peak = bat(voice, tier)
            write_wav(os.path.join(out_dir, f"bat-{voice}-{tier}.wav"), samples, RATE, peak=peak)
    print(f"\nShipping BAT_VOICE = {BAT_VOICE!r}. Change it in bake.py and re-bake to switch.")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--only", action="append", choices=sorted(SLOTS), help="bake one slot")
    ap.add_argument("--candidates", metavar="DIR", help="bake every bat voice for audition")
    ap.add_argument("--check", action="store_true",
                    help="verify every bat voice keeps the perfect/solid/cheap ladder")
    ap.add_argument("--out", default=DIR, help="output directory (default: the catalog)")
    args = ap.parse_args()
    if args.check:
        raise SystemExit(check())
    if args.candidates:
        candidates(args.candidates)
        return
    bake(args.only or list(SLOTS), args.out)


if __name__ == "__main__":
    main()
