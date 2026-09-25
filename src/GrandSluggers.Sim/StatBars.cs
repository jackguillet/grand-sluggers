using System.Collections.Generic;

namespace GrandSluggers.Sim.Front;

/// <summary>
/// The four bars a card shows (CH-07, spec §2): Bat, Pitch, Field, Run, in that order. Each value is the sim's
/// derived bar (<see cref="Stats.Bat"/>, <see cref="Stats.Pitch"/>, <see cref="Stats.Field"/>, <see cref="Stats.Run"/>);
/// a board reads it here and never recomputes or stores one. The captain board and the lineup card both draw these
/// rows, so either seat, either pad, sees the same four numbers for the same character (SC-24).
/// </summary>
public static class StatBars
{
    public const int Count = 4;

    /// <summary>The bar labels as every board prints them (couch copy: <see cref="CarnivalFront.StatBarLabels"/>).</summary>
    public static IReadOnlyList<string> Labels => CarnivalFront.StatBarLabels;

    /// <summary>The derived value of bar <paramref name="bar"/> (0 Bat, 1 Pitch, 2 Field, 3 Run). No allocation: boards call it every OnGUI pass.</summary>
    public static int Value(Stats stats, int bar) => bar switch
    {
        0 => stats.Bat,
        1 => stats.Pitch,
        2 => stats.Field,
        _ => stats.Run
    };

    static readonly string[] Numbers = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];

    /// <summary>The printed value of bar <paramref name="bar"/>, from a cached string (bars are 1–10).</summary>
    public static string ValueText(Stats stats, int bar)
    {
        var v = Value(stats, bar);
        return v >= 0 && v < Numbers.Length ? Numbers[v] : v.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>How full bar <paramref name="bar"/> draws, 0–1.</summary>
    public static double Fill(Stats stats, int bar) => CharacterCard.BarFill(Value(stats, bar));
}

/// <summary>
/// Where a card draws its four bar rows, in pixels on the 1280×800 board, relative to the card panel's top-left.
/// Row <c>i</c> spans <see cref="RowY"/> to <see cref="RowY"/> + <see cref="Pitch"/>; the label, the bar and the value
/// sit left to right in it, centered on the row.
/// </summary>
public readonly record struct StatBarLayout(
    float Top, float Pitch,
    float LabelX, float LabelW,
    float BarX, float BarW, float BarH,
    float ValueX, float ValueW,
    int LabelFont, int ValueFont)
{
    public float RowY(int bar) => Top + bar * Pitch;
    public float BarY(int bar) => RowY(bar) + (Pitch - BarH) * .5f;
    /// <summary>The first free pixel row under the four bars.</summary>
    public float Bottom => RowY(StatBars.Count);
}
