namespace GrandSluggers.Sim.Front;

/// <summary>The book's and the tutorial screens' own words (#1048): the book label, the diagram captions, the tutorial verdicts.</summary>
public static partial class HowToPlay
{
    public const string BookLabel = "HOW TO PLAY";
    public const string CardStillTitle = "THE CARD";
    public const string TutorialSetupHeader = "THE SETUP";
    public static string TutorialVerdict(bool success) => success ? "GOOD WORK" : "TRY AGAIN";

    /// <summary>A bag diagram's caption.</summary>
    public static string BagDiagramCaption(BagDiagrams.Kind kind) => kind switch
    {
        BagDiagrams.Kind.BagMap => "PICK A RUNNER · ARM A THROW",
        BagDiagrams.Kind.Advance => "EVERY RUNNER GOES FOR THE NEXT BAG",
        BagDiagrams.Kind.Return => "EVERY RUNNER COMES BACK ONE BAG",
        _ => ""
    };

    /// <summary>A bag with the arrow that points to it on the book's bag map.</summary>
    public static string BagArrow(int bag) => bag switch
    {
        1 => "→ 1B",
        2 => "↑ 2B",
        3 => "3B ←",
        4 => "↓ HOME",
        _ => ""
    };
}
