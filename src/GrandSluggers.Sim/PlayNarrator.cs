using System.Text;

namespace GrandSluggers.Sim;

/// <summary>
/// The one narrator (spec §12): every play caption and every live decision's line is built here, from typed facts the
/// match raises (<see cref="PlayCall"/>, <see cref="InPlay.ThrowVerdict"/>). It is core, not front-of-house: the sim
/// narrates its own events, and the HUD (<see cref="Front.BroadcastHud"/>) only lays them out.
/// </summary>
public static class PlayNarrator
{
    /// <summary>
    /// The caption for a call. Facts join with one space; a Billboard or an item line, and an <see cref="CallPart.Aside"/>,
    /// join with two. An aside that says nothing hands its gap to the next fact. A live decision that already names the
    /// batter at first drops the separate "in at first", and one that turned two drops "Double play."
    /// </summary>
    public static string Narrate(PlayCall call)
    {
        var live = call.Parts.Where(p => p.Beat == CallBeat.Live && p.Moment is not null).Select(p => p.Moment!.Verdict).ToList();
        var text = new StringBuilder();
        var first = true;
        var carryAside = false;
        foreach (var part in call.Parts)
        {
            if (part.Beat == CallBeat.BatterInAtFirst && live.Any(NarratesBatterAtFirst)) continue;
            if (part.Beat == CallBeat.DoublePlay && live.Contains(InPlay.ThrowVerdict.TurnedTwo)) continue;
            var said = Say(part);
            var aside = carryAside || part.Aside || part.Beat is CallBeat.Billboard or CallBeat.Item;
            if (part.Aside && said.Length == 0)
            {
                carryAside = true;
                continue;
            }
            carryAside = false;
            if (!first) text.Append(aside ? "  " : " ");
            text.Append(said);
            first = false;
        }
        return text.ToString();
    }

    static string Say(CallPart p) => p.Beat switch
    {
        CallBeat.StrikeSwinging => $"Strike {p.Number}.",
        CallBeat.StrikeLooking => $"Strike {p.Number} looking.",
        CallBeat.Ball => $"Ball {p.Number}.",
        CallBeat.StruckOutSwinging => $"{p.Who} goes down swinging.",
        CallBeat.StruckOutLooking => $"{p.Who} is caught looking.",
        CallBeat.StruckOutBuntFoul => $"{p.Who} bunts foul for strike three.",
        CallBeat.Walk => $"{p.Who} walks.",
        CallBeat.HitByPitch => $"{p.Who} is hit.",
        CallBeat.Pickoff => "Pickoff.",
        CallBeat.Balk => "Balk: threw to a base after committing to pitch. Runners advance one base.",
        CallBeat.StolenBase => "Stolen base.",
        CallBeat.Foul => "Foul.",
        CallBeat.HomeRun => p.Word switch
        {
            "furnace" => $"{p.Who} HOT IRON - it's gone.",
            "heat-swing" => $"{p.Who} SPARKLER - it's gone.",
            _ => $"{p.Who} goes deep."
        },
        CallBeat.InsideTheParkHomeRun => $"{p.Who} - all the way around!",
        CallBeat.GroundRuleDouble => $"{p.Who} - over the fence on a hop. Ground-rule double.",
        CallBeat.Triple => $"{p.Who} triples.",
        CallBeat.Double => $"{p.Who} doubles.",
        CallBeat.Single => $"{p.Who} singles.",
        CallBeat.RedirectSingle => $"{p.Who} - it went through a {RedirectName(p.Word)}!",
        CallBeat.HeatballSingle => $"{p.Who} - it drops in off the star pitch!",
        CallBeat.Live => p.Moment is { } m ? Verdict(m.Verdict, m.Bag, m.Fielder?.Name ?? p.Other, p.Who, m.Runner?.Name) : "",
        CallBeat.BatterInAtFirst => $"{p.Who} in at first.",
        CallBeat.TriplePlay => "Triple play!",
        CallBeat.DoublePlay => "Double play.",
        CallBeat.BuddyJump => $"{p.Who} + {p.Other} BUDDY JUMP!",
        CallBeat.Clamber => $"{p.Who} CLAMBERS the wall!",
        CallBeat.PutAway => $"{p.Who} puts it away.",
        CallBeat.ToFirst => $"{p.Who} to first.",
        CallBeat.SacFly => "Sac fly.",
        CallBeat.FieldersChoice => "Fielder's choice.",
        CallBeat.Billboard => "Billboard STAR!",
        CallBeat.Item => p.Word switch
        {
            "banana" => "Banana slip!",
            "rocket" => "Rocket daze!",
            "pow" => "POW!",
            _ => $"{p.Word}!"
        },
        CallBeat.CaughtStealing => $"{p.Who} caught stealing.",
        CallBeat.PickedOff => $"{p.Who} picked off.",
        CallBeat.Steals => $"{p.Who} steals {BagName(p.Number)}.",
        CallBeat.BackToBag => $"{p.Who} back to the bag.",
        _ => ""
    };

    /// <summary>The line for a live throw's verdict. Produced from the typed facts, last; nothing reads it back.</summary>
    public static string Verdict(InPlay.ThrowVerdict verdict, int bag, string? fielderName, string? batterName, string? runnerName = null)
    {
        fielderName ??= "";
        batterName ??= "";
        var where = bag == 3 ? " at third" : bag == 4 ? " at home" : "";
        return verdict switch
        {
            InPlay.ThrowVerdict.BatterSafeAfterForce => $"Force at second. {batterName} in at first.",
            InPlay.ThrowVerdict.BeatForce or InPlay.ThrowVerdict.Beat => $"{batterName} beats the throw.",
            InPlay.ThrowVerdict.TurnedTwo => $"{fielderName} turns two.",
            InPlay.ThrowVerdict.OutAtFirst => $"{fielderName} to first.",
            InPlay.ThrowVerdict.ForceOut => $"{fielderName} forces the runner{where}.",
            InPlay.ThrowVerdict.TagOut => $"{fielderName} tags the runner{where}.",
            InPlay.ThrowVerdict.TagRunner => $"{fielderName} tags {runnerName ?? "the runner"}.",
            InPlay.ThrowVerdict.DoubledOff => $"{fielderName} doubles {runnerName ?? "the runner"} off{(bag == 1 ? " first" : bag == 2 ? " second" : where)}.",
            InPlay.ThrowVerdict.Wasted => $"{fielderName} throws to {BagName(bag)} with nobody to play on.",
            _ => ""
        };
    }

    /// <summary>The verdict's own line already says the batter reached first.</summary>
    public static bool NarratesBatterAtFirst(InPlay.ThrowVerdict verdict) => verdict == InPlay.ThrowVerdict.BatterSafeAfterForce;

    public static string BagName(int bag) => bag switch
    {
        1 => "first",
        2 => "second",
        3 => "third",
        4 => "home",
        _ => "the cutoff"
    };

    /// <summary>What a redirect hazard is called on the broadcast.</summary>
    public static string RedirectName(string? type) => type switch
    {
        HazardType.Barrel => "barrel cannon",
        HazardType.Chomper => "chomper",
        _ => "warp can"
    };

    /// <summary>Banner headline. Kind.ToString() is TAKESTRIKE, not a scorebug.</summary>
    public static string Headline(PlayKind kind) => kind switch
    {
        PlayKind.StolenBase => "STOLEN BASE",
        PlayKind.CaughtStealing => "CAUGHT STEALING",
        PlayKind.Pickoff => "PICKOFF",
        PlayKind.HomeRun => "HOME RUN",
        PlayKind.Triple => "TRIPLE",
        PlayKind.Double => "DOUBLE",
        PlayKind.Single => "SINGLE",
        PlayKind.Walk => "WALK",
        PlayKind.HitByPitch => "HIT BY PITCH",
        PlayKind.Strikeout => "STRIKE OUT",
        PlayKind.FlyOut => "OUT",
        PlayKind.GroundOut => "OUT",
        PlayKind.Foul => "FOUL",
        PlayKind.SwingMiss => "SWING AND A MISS",
        PlayKind.TakeStrike => "STRIKE",
        PlayKind.TakeBall => "BALL",
        _ => kind.ToString().ToUpperInvariant()
    };
}
