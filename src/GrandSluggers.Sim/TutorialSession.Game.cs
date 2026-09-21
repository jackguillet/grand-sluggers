namespace GrandSluggers.Sim;

/// <summary>Game-literacy sequences use the ordinary count, foul flight and half-inning transitions.</summary>
public sealed partial class TutorialSession
{
    int _gameStep;
    int _gameOutsBefore;

    void ResetGameEvidence()
    {
        _gameStep = 0;
        _gameOutsBefore = Match.Outs;
    }

    void PrepareGameHalf()
    {
        // Two ordinary called strikeouts create the teaching third-out opportunity.
        var take = new SwingCommand(false, 0, 0, false);
        for (var outNumber = 0; outNumber < 2; outNumber++)
            for (var strike = 0; strike < 3; strike++)
            {
                Match.BeginAtBat(new PitchCommand(PitchFamily.Fastball, 0, false), take, out _, out var play);
                if (strike == 2 && (play?.Kind != PlayKind.Strikeout || Match.Outs != outNumber + 1))
                    throw new InvalidDataException("Game half setup no longer produces two real called strikeouts.");
            }
    }

    void PitchGame(PitchCommand command)
    {
        var beforeBalls = Match.Balls;
        var beforeStrikes = Match.Strikes;
        var beforeOuts = Match.Outs;
        var beforeInning = Match.Inning;
        var beforeTop = Match.Top;
        Match.BeginAtBat(command, Take, out var hit, out var play);
        LastHit = hit; LastPlay = play;
        if (Lesson.Objective == "game-count-sequence")
        {
            if (_gameStep == 0 && play?.Kind == PlayKind.TakeBall && beforeBalls == 0 && Match.Balls == 1 && Match.Strikes == 0)
            {
                _gameStep = 1;
                return;
            }
            if (_gameStep == 1 && play?.Kind == PlayKind.TakeStrike && beforeStrikes == 0
                && Match.Balls == 1 && Match.Strikes == 1)
                Finish(true, "one-and-one", "Your ball and called strike made a 1–1 count.");
            else Finish(false, "wrong-count", "Pitch a ball, then a called strike to reach 1–1.");
            return;
        }
        var thirdOut = beforeOuts == 2 && beforeStrikes == 2 && beforeInning == 1 && beforeTop
            && play?.Kind == PlayKind.Strikeout && Match.Outs == 0 && Match.Inning == 1 && !Match.Top;
        Finish(thirdOut, thirdOut ? "half-changed" : "game-half-third-out-missed",
            thirdOut ? "Your called third strike made the third out and moved the game to the bottom of the inning."
                : "Put the called third strike in the zone to make the third out and change sides.");
    }

    void SwingGame(SwingCommand command)
    {
        var beforeStrikes = Match.Strikes;
        var live = Match.BeginAtBat(CpuPitch, command, out var hit, out var play);
        LastHit = hit; LastPlay = play;
        if (!live || hit.Quality == ContactQuality.Miss)
        {
            Finish(false, "no-contact", "Make contact, first foul and then fair.");
            return;
        }
        if ((_gameStep == 0 && !hit.Foul) || (_gameStep == 1 && hit.Foul))
        {
            Finish(false, "wrong-territory", "First hit a foul ball, then put the next contact fair.");
            return;
        }
        if (_gameStep == 0 && beforeStrikes != 0)
        {
            Finish(false, "wrong-count", "Start from a fresh 0–0 count.");
            return;
        }
        var preview = Match.PreviewHit(hit, command);
        Match.LivePlay.Recording = true;
        Match.LivePlay.Apply(LivePlayCommand.BeginLive(CpuPitch, command, hit, preview, null,
            new LiveSeats(true, false, false, false), 0, LivePlayCommandSource.System));
    }

    void ObserveGameContact(LivePlayCommandResult result)
    {
        if (result.CompletedPlay is not { } play) return;
        LastPlay = play;
        if (_gameStep == 0)
        {
            if (play.Kind == PlayKind.Foul && Match.Strikes == 1 && Match.Outs == _gameOutsBefore)
                _gameStep = 1;
            else Finish(false, "foul-not-called", "The first contact must land foul and add a strike.");
            return;
        }
        var fair = LastHit is { Foul: false, InPlay: true } && play.Kind != PlayKind.Foul;
        Finish(fair, fair ? "foul-then-fair" : "fair-not-called",
            fair ? "The foul raised the strike count; the next fair ball completed the at-bat."
                : "Keep the first foul strike, then put the next ball fair.");
    }
}
