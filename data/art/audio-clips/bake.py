#!/usr/bin/env python3
"""Original Grand Sluggers hits. No Nintendo samples. No licensed music."""
from __future__ import annotations

import math
import os
import struct
import wave

RATE = 44100
DIR = os.path.dirname(os.path.abspath(__file__))


def clamp(x: float, lo: float = -1.0, hi: float = 1.0) -> float:
    return lo if x < lo else hi if x > hi else x


def hash01(i: int, seed: int = 0) -> float:
    n = (i * 16777619 + seed * 2654435761) & 0xFFFFFFFF
    n ^= n >> 13
    n = (n * 1274126177) & 0xFFFFFFFF
    return (n & 0xFFFF) / 32768.0 - 1.0


def env_exp(t: float, k: float) -> float:
    return math.exp(-t * k)


def write_wav(name: str, samples: list[float]) -> None:
    peak = max((abs(x) for x in samples), default=1.0) or 1.0
    path = os.path.join(DIR, name + ".wav")
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        frames = b"".join(
            struct.pack("<h", int(clamp(s / peak * 0.89) * 32767)) for s in samples
        )
        w.writeframes(frames)
    print(f"{name:12} {len(samples) / RATE:.3f}s  peak {peak:.2f}  -> {path}")


def bat_perfect() -> list[float]:
    n = int(RATE * 0.14)
    out = [0.0] * n
    for i in range(n):
        t = i / RATE
        click = (1.0 - t / 0.003) * hash01(i, 7) if t < 0.003 else 0.0
        crack_f = 3100.0 * math.exp(-t * 18.0) + 900.0
        crack = math.sin(2 * math.pi * crack_f * t) * env_exp(t, 42.0)
        wood = math.sin(2 * math.pi * 168.0 * t) * env_exp(t, 28.0) * 0.35
        grit = hash01(i, 11) * env_exp(t, 70.0) * 0.45
        hiss = hash01(i * 3, 19) * env_exp(t, 110.0) * 0.22
        out[i] = click * 0.85 + crack * 0.95 + wood + grit + hiss
    return out


def bat_solid() -> list[float]:
    n = int(RATE * 0.12)
    out = [0.0] * n
    for i in range(n):
        t = i / RATE
        click = (1.0 - t / 0.004) * hash01(i, 3) * 0.55 if t < 0.004 else 0.0
        crack_f = 1600.0 * math.exp(-t * 14.0) + 420.0
        crack = math.sin(2 * math.pi * crack_f * t) * env_exp(t, 34.0)
        wood = math.sin(2 * math.pi * 118.0 * t) * env_exp(t, 22.0) * 0.55
        grit = hash01(i, 5) * env_exp(t, 48.0) * 0.38
        out[i] = click + crack * 0.7 + wood + grit
    return out


def bat_cheap() -> list[float]:
    n = int(RATE * 0.10)
    out = [0.0] * n
    for i in range(n):
        t = i / RATE
        thud = math.sin(2 * math.pi * 82.0 * t) * env_exp(t, 18.0)
        dull = math.sin(2 * math.pi * 140.0 * t) * env_exp(t, 26.0) * 0.4
        # brown-ish: integrate white, no highs
        dust = hash01(i, 2) * env_exp(t, 30.0) * 0.28
        if i:
            dust = out[i - 1] * 0.15 + dust
        out[i] = thud * 0.9 + dull + dust
    return out


def glove() -> list[float]:
    n = int(RATE * 0.08)
    out = [0.0] * n
    for i in range(n):
        t = i / RATE
        slap = (1.0 - t / 0.0025) * hash01(i, 13) if t < 0.0025 else 0.0
        leather = math.sin(2 * math.pi * 240.0 * t) * env_exp(t, 48.0)
        pocket = math.sin(2 * math.pi * 110.0 * t) * env_exp(t, 36.0) * 0.55
        air = hash01(i, 17) * env_exp(t, 64.0) * 0.4
        out[i] = slap * 0.7 + leather * 0.8 + pocket + air
    return out


def crowd_bed() -> list[float]:
    seconds = 3.2
    n = int(RATE * seconds)
    fade = int(RATE * 0.08)
    raw = [0.0] * n
    b0 = b1 = b2 = b3 = b4 = b5 = b6 = 0.0
    for i in range(n):
        t = i / RATE
        w = hash01(i, 41)
        b0 = 0.99886 * b0 + w * 0.0555179
        b1 = 0.99332 * b1 + w * 0.0750759
        b2 = 0.96900 * b2 + w * 0.1538520
        b3 = 0.86650 * b3 + w * 0.3104856
        b4 = 0.55000 * b4 + w * 0.5329522
        b5 = -0.7616 * b5 - w * 0.0168980
        pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + w * 0.5362
        b6 = w * 0.115926
        murmur = 0.72 + 0.28 * math.sin(t * 6.2 + hash01(int(t * 9), 8) * 0.7)
        far = 0.55 + 0.45 * math.sin(t * 1.7)
        # band-limit: mix pink with a mid hum
        hum = math.sin(2 * math.pi * 220.0 * t) * 0.04 * (0.6 + 0.4 * math.sin(t * 2.3))
        raw[i] = pink * 0.22 * murmur * far + hum
    out = raw[:]
    for i in range(fade):
        k = i / fade
        out[n - fade + i] = raw[n - fade + i] * (1.0 - k) + raw[i] * k
    return out


def main() -> None:
    write_wav("bat-perfect", bat_perfect())
    write_wav("bat-solid", bat_solid())
    write_wav("bat-cheap", bat_cheap())
    write_wav("glove", glove())
    write_wav("crowd-bed", crowd_bed())


if __name__ == "__main__":
    main()
