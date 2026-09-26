namespace GrandSluggers.Sim;

/// <summary>The live play as the CPU glove's decision table reads it (<see cref="CpuFieldDecider"/>): read-only views onto this play.</summary>
public sealed partial class LivePlaySystem : ICpuFieldView
{
    RulesTable ICpuFieldView.Rules => R;
    double ICpuFieldView.Elapsed => ElapsedSeconds;
    int ICpuFieldView.Outs => _match.Outs;
    int ICpuFieldView.HomeScore => _match.HomeScore;
    int ICpuFieldView.AwayScore => _match.AwayScore;
    Runner? ICpuFieldView.RunnerAt(int fromBag) => _match.RunnerAt(fromBag);
    Runner? ICpuFieldView.RunnerForBag(int bag) => RunnerForBag(bag);
    double ICpuFieldView.ThrowArrivalSec(int bag) => CpuThrowArrivalSec(bag);
    double ICpuFieldView.WalkSec(int bag) => CpuWalkSec(bag);
    double ICpuFieldView.ThrowReadySec(int bag) => CpuThrowReadySec(bag);
    double ICpuFieldView.HoldLeftSec => HotHoldLeftSec;
}
