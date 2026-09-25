namespace GrandSluggers.Sim;

/// <summary>The live play as the pursuit's decisions read it (<see cref="PursuitDecider"/>): read-only views onto this play.</summary>
public sealed partial class LivePlaySystem : IPursuitView
{
    RulesTable IPursuitView.Rules => R;
    Park IPursuitView.Park => Park;
    double IPursuitView.Elapsed => ElapsedSeconds;
    FieldingPreview? IPursuitView.Preview => Preview;
    IReadOnlyList<Sample>? IPursuitView.Path => Path;
    AtBatResult? IPursuitView.Hit => Hit;
    double IPursuitView.Hang => Hang;
    string IPursuitView.GlovePos => GlovePos;
    double IPursuitView.GloveX => GloveX;
    double IPursuitView.GloveZ => GloveZ;
    double IPursuitView.BallX => BallX;
    double IPursuitView.BallY => BallY;
    double IPursuitView.BallZ => BallZ;
    bool IPursuitView.HoldsBall => HoldsBall;
    bool IPursuitView.Throwing => Throwing;
    bool IPursuitView.LooseBall => LooseBall;
    string IPursuitView.BuddyPos => BuddyPos;
    IReadOnlyDictionary<string, (double X, double Z)> IPursuitView.Bodies => _fielders;
    IReadOnlyDictionary<string, double> IPursuitView.ReadyTable => _readyAt;
    IReadOnlyDictionary<string, Character> IPursuitView.Assigned => Assigned();
    double IPursuitView.ReadyAt(string pos) => ReadyAt(pos);
    bool IPursuitView.CanMove(string pos) => CanMove(pos);
    bool IPursuitView.Coasting(string pos) => Coasting(pos);
    bool IPursuitView.KeptOff(string pos) => _items.IsOff(pos);
    Character IPursuitView.GloveBody => GloveChar();
    double IPursuitView.CatchWindow => CatchWindow(Assigned());
}
