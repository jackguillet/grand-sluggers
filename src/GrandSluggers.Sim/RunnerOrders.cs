namespace GrandSluggers.Sim;

/// <summary>Semantic controller orders. Selection never moves a body. Bag 4 selects the batter.</summary>
public sealed record RunnerOrderInput(int SelectBag = 0, bool SelectAll = false,
    bool Advance = false, bool Return = false, bool Halt = false);

/// <summary>One selection and held-order boundary through SET, flight and contact.</summary>
public sealed class RunnerOrders
{
    string? _runnerId;
    bool _all = true;
    bool _advanceBlocked, _returnBlocked;
    RunnerOrderInput _previous = new();
    public bool AllSelected => _all;
    public string? SelectedId => _runnerId;
    public string Label(Match match) => _all ? "ALL RUNNERS"
        : Selected(match) is { } r ? r.Who.Name.ToUpperInvariant() : "SELECT RUNNER";

    public Runner? Selected(Match match) => _runnerId is null ? null
        : match.Runners.FirstOrDefault(r => r.Live && r.Who.Id == _runnerId);

    public void Reset()
    {
        _runnerId = null;
        _all = true;
        _advanceBlocked = _returnBlocked = false;
        _previous = new();
    }

    public void Apply(Match match, RunnerOrderInput input)
    {
        if (match.Paused || match.Over) return;
        if (!input.Advance) _advanceBlocked = false;
        if (!input.Return) _returnBlocked = false;
        var selecting = input.SelectAll || input.SelectBag is >= 1 and <= 4;
        if (selecting)
        {
            _all = input.SelectAll;
            var from = input.SelectBag == 4 ? 0 : input.SelectBag;
            _runnerId = _all ? null : match.RunnerAt(from)?.Who.Id;
            if (_runnerId is not null) match.SelectRunner(input.SelectBag);
            // A newly pressed order may accompany selection; an old hold cannot transfer.
            _advanceBlocked |= input.Advance && _previous.Advance;
            _returnBlocked |= input.Return && _previous.Return;
        }
        var runner = Selected(match);
        if (!_all && runner is null) _runnerId = null; // never broaden to ALL
        var halt = input.Halt || input.Advance && input.Return;
        if (halt)
        {
            if (_all) match.FreezeRunners();
            else if (runner is not null) match.HaltAt(runner.FromBag);
            _advanceBlocked |= input.Advance;
            _returnBlocked |= input.Return;
        }
        else if (input.Advance && !_advanceBlocked)
        {
            if (_all) match.AdvanceAll();
            else if (runner is not null)
            {
                match.SendRunnerAt(runner.FromBag);
            }
        }
        else if (input.Return && !_returnBlocked)
        {
            if (_all) match.ReturnAll();
            else if (runner is not null) match.ReturnToBagAt(runner.FromBag);
        }
        _previous = input;
    }
}
