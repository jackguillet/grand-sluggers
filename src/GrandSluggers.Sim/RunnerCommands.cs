namespace GrandSluggers.Sim;

/// <summary>The same runner selection, send, halt, return and slide commands on either side of pitch contact.</summary>
public static class RunnerCommands
{
    public static void Apply(Match m, LivePadInput run, ref LivePadInput previous)
    {
        if (run.Orders is { } orders)
        {
            m.ControllerRunners.Apply(m, orders);
            var selected = m.ControllerRunners.Selected(m);
            if (run.WestDown && selected is not null
                && selected.FeetTo(selected.NextBag) <= RunnerSystem.SlideFt(selected.NextBag, GroundZones.Of(m.Park, m.Rules), m.Rules))
                selected.RequestSlide();
            previous = run;
            return;
        }
        if (run.KeysBag is >= 1 and <= 4) m.SelectRunner(run.KeysBag);
        var advDown = run.AllAdvance && !previous.AllAdvance;
        var retDown = run.AllReturn && !previous.AllReturn;
        if (run.Freeze)
        {
            var named = run.StickBag > 0 ? m.Runners.FirstOrDefault(r => r.Live && (r.NextBag == run.StickBag || r.Bag == run.StickBag)) : null;
            if (named is not null) m.HaltAt(named.FromBag);
            else m.FreezeRunners();
        }
        else if (run.AllAdvance)
        {
            if (advDown && m.Runners.Any(r => r.Live && r.Phase == RunnerPhase.Returning))
                foreach (var r in m.Runners.Where(r => r.Live && r.Phase == RunnerPhase.Returning)) r.Halt();
            else m.AdvanceAll();
        }
        else if (run.AllReturn)
        {
            if (retDown && m.Runners.Any(r => r.Live && r.Advancing && r.Feet > 0))
                foreach (var r in m.Runners.Where(r => r.Live && r.Advancing)) r.Halt();
            else m.ReturnAll();
        }
        var sel = m.SelectedState;
        if (sel is not null && run.StickBag > 0 && !run.Freeze)
        {
            if (run.StickBag == sel.NextBag && sel.Bag < 4) m.SendRunnerAt(sel.FromBag);
            else if (run.StickBag == sel.Bag || run.StickBag == Baserunning.PrevBag(Math.Max(1, sel.Bag))) m.ReturnToBagAt(sel.FromBag);
        }
        if ((run.WestDown || run.SouthDown) && sel is not null && sel.FeetTo(sel.NextBag) <= RunnerSystem.SlideFt(sel.NextBag, GroundZones.Of(m.Park, m.Rules), m.Rules))
            sel.RequestSlide();
        previous = run;
    }
}
