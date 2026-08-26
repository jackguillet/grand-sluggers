using System.Collections.Generic;

namespace GrandSluggers.Sim;

/// <summary>
/// Couch card: four numbers and the verbs you fire. HUD draws this; tests lock copy.
/// </summary>
public readonly record struct CharacterCard(
    string Id,
    string Name,
    string Faction,
    bool Captain,
    Stats Stats,
    string StarPitch,
    string StarSwing,
    string FieldVerb,
    Chemistry VsCaptain)
{
    public static CharacterCard Of(Character who, Chemistry vsCaptain)
    {
        var card = Of(who);
        return card with { VsCaptain = vsCaptain };
    }

    public static CharacterCard Of(Character who, Character? captain = null, ChemistryTable? chem = null)
    {
        var vs = Chemistry.Neutral;
        if (captain != null && chem != null && !who.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase))
            vs = chem.Between(captain, who);
        else if (captain != null && who.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase))
            vs = Chemistry.Good;
        return new(
            who.Id,
            who.Name,
            who.Faction,
            who.Captain,
            who.Stats.Clamp(),
            Title(who.StarPitch),
            Title(who.StarSwing),
            Title(who.FieldAbility),
            vs);
    }

    public static double BarFill(int n) => Math.Clamp(n, 0, 10) / 10.0;

    public static string Title(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "";
        var parts = id.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var words = new List<string>();
        foreach (var raw in parts)
        {
            var w = raw;
            if (w.Length > 4 && w.EndsWith("ball", StringComparison.OrdinalIgnoreCase))
            {
                words.Add(Cap(w[..^4]));
                words.Add("Ball");
            }
            else
                words.Add(Cap(w));
        }
        return string.Join(' ', words);
    }

    static string Cap(string w) =>
        string.IsNullOrEmpty(w) ? "" : char.ToUpperInvariant(w[0]) + w[1..];
}
