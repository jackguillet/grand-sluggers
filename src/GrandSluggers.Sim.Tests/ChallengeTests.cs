using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ChallengeTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void CaptainsExist()
    {
        foreach (var id in PresetTeams.CaptainIds)
        {
            var c = _content.Must(id);
            Assert.True(c.Captain, id);
        }
        Assert.Equal(Silhouette.Captains.Length, PresetTeams.CaptainIds.Length);
    }

    [Fact]
    public void ForCaptainFillsNineUnique()
    {
        foreach (var id in PresetTeams.CaptainIds)
        {
            var team = PresetTeams.ForCaptain(_content, id);
            Assert.Equal(9, team.Roster.Count);
            Assert.Equal(9, team.Roster.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(id, team.Captain.Id);
            Assert.Contains(team.Roster, c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ExhibitionPairDoesNotSharePlayers()
    {
        foreach (var home in PresetTeams.CaptainIds)
        {
            var awayId = PresetTeams.NextCaptain(home);
            var (h, a) = PresetTeams.Pair(_content, home, awayId);
            var overlap = h.Roster.Select(c => c.Id).Intersect(a.Roster.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
            Assert.Empty(overlap);
            Assert.Equal(9, h.Roster.Count);
            Assert.Equal(9, a.Roster.Count);
        }
    }

    [Fact]
    public void AllCaptainPairingsFinishAGame()
    {
        foreach (var home in PresetTeams.CaptainIds)
        {
            var away = PresetTeams.NextCaptain(home);
            var match = Match.Exhibition(_content, home, away, innings: 3, seed: 9);
            match.AutoPlayGame();
            Assert.True(match.Over, $"{home} vs {away}");
            Assert.Equal(home, match.Home.Captain.Id);
            Assert.Equal(away, match.Away.Captain.Id);
        }
    }

    [Fact]
    public void ExhibitionDefaultsRioVsAshlord()
    {
        var match = Match.Exhibition(_content, seed: 1);
        Assert.Equal("rio", match.Home.Captain.Id);
        Assert.Equal("ashlord", match.Away.Captain.Id);
    }

    [Fact]
    public void ChallengeStartsWithFactionMates()
    {
        var run = Challenge.Start(_content, "rio");
        Assert.Contains("rio", run.Owned);
        Assert.Contains("nico", run.Owned);
        Assert.Contains("pip", run.Owned);
        Assert.Contains("marlow", run.Owned);
        Assert.Contains("gull", run.Owned);
        Assert.DoesNotContain("ashlord", run.Owned);
        Assert.Equal("ashlord", run.NextOpponentId(_content));
    }

    [Fact]
    public void ChallengeWinRecruitsAnOpponent()
    {
        var run = Challenge.Start(_content, "vale");
        var match = run.MakeMatch(_content, innings: 3, seed: 4);
        var recruit = run.ApplyOutcome(true, match.Away.Captain, match.Away.Roster, match.Away.Roster.First(c => !c.Captain));
        Assert.NotNull(recruit);
        Assert.False(recruit.Captain);
        Assert.Contains(recruit.Id, run.Owned);
        Assert.Contains(match.Away.Captain.Id, run.Beaten);
        Assert.True(run.LastWin);

        var next = run.MakeMatch(_content, innings: 3, seed: 5);
        Assert.Contains(next.Home.Roster, c => c.Id == recruit.Id);
        Assert.DoesNotContain(next.Away.Roster, c => c.Id == recruit.Id);
    }

    [Fact]
    public void ChallengeLossDoesNotRecruit()
    {
        var run = Challenge.Start(_content, "zig");
        var before = run.Owned.Count;
        var match = run.MakeMatch(_content, seed: 2);
        var recruit = run.ApplyOutcome(false, match.Away.Captain, match.Away.Roster, null);
        Assert.Null(recruit);
        Assert.Equal(before, run.Owned.Count);
        Assert.Empty(run.Beaten);
        Assert.False(run.LastWin);
    }

    [Fact]
    public void ChallengeMatchUsesOpponentHomePark()
    {
        var run = Challenge.Start(_content, "rio");
        var match = run.MakeMatch(_content, seed: 1);
        Assert.Equal("ashlord", match.Away.Captain.Id);
        Assert.Equal("ember-keep", match.Park.Id);
    }

    [Fact]
    public void RolePlayerCountIsAtLeastEighteen()
    {
        var roles = _content.Characters.Values.Count(c => !c.Captain);
        var caps = _content.Characters.Values.Count(c => c.Captain);
        Assert.True(roles >= 18, $"role players {roles}");
        Assert.Equal(Silhouette.Captains.Length, caps);
    }
}
