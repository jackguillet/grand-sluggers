# Reproduce the #693 baseline

Use a dedicated worktree at `eff10d90d1b19ba0ab4f66d471163eb89a8f7db2` with the project's .NET SDK and data. The [baseline JSON](game-feel-693-baseline.json) contains the source hashes and descriptive results. No Nintendo target values are inferred by these commands.

## Existing entry points

From the worktree root:

```sh
dotnet run --project src/GrandSluggers.Cli -- protocol
dotnet test src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj --verbosity minimal -m:1
dotnet run --project src/GrandSluggers.Cli -- match --home rio --away ashlord --park harbor-diamond --seed 7 --trace /tmp/gs693-harbor-seed7.json
```

The test runner requires normal local process/socket access. The trace is a deterministic CPU example, not a human play acceptance or the S-29 cohort. Its baseline JSON summary retains the trace digest and all 39 live-play summaries.

## Temporary S-31/S-32 observation

This probe was run against the existing test helpers without changing sim or test code. It is a revision-specific research tool: reflection depends on private helper signatures. #702 owns the durable, explicit event instrumentation.

Create `scratchpad/feel-probe/Probe.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj" /></ItemGroup></Project>
```

Create `scratchpad/feel-probe/Program.cs` with:

```csharp
using GrandSluggers.Sim;
using GrandSluggers.Sim.Tests;
using System.Reflection;
using System.Text.Json;
var fixture = new FieldingScenarioTests();
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var defense = typeof(FieldingScenarioTests).GetMethod("Defense", flags)!;
var run = typeof(FieldingScenarioTests).GetMethods(flags).Single(m => m.Name == "RunCpu" && m.GetParameters().Length == 6);
var rows = new List<object>();
foreach(var batter in new[]{"cinder", "dart"}) {
 var match=(Match)defense.Invoke(fixture,new object[]{batter,"lace","ashlord",1})!;
 var hit=FlightFixtures.Landing(match.Park,118,4,-18);
 var preview=match.PreviewHit(hit);
 var body=Runner.BatterRunner(match.Batter,HomeSet.BatterBodyX(match.Batter.Bats),HomeSet.BatterZ);
 var arrival=RunnerSystem.ArrivalSec(body,1,0,0,match.Rules);
 double? possession=null,command=null;
 Action<LivePlaySystem> observe=l=>{if(possession is null&&l.HoldsBall)possession=l.ElapsedSeconds;if(command is null&&l.Throwing)command=l.ElapsedSeconds;};
 var args2=new object?[]{match,hit,preview,0,observe,0d};
 var result=((PlayEvent Play,double ThrowLandedAt,ThrowResult? Thrown))run.Invoke(fixture,args2)!;
 rows.Add(new{batter,run=body.Who.Stats.Run,park=match.Park.Id,exitMph=hit.ExitVeloMph,launchDeg=hit.LaunchDeg,sprayDeg=hit.SprayDeg,kind=result.Play.Kind.ToString(),possessionSec=possession,firstThrowFlagSec=command,throwEndSec=result.ThrowLandedAt,runnerArrivalPredictedSec=arrival,marginSec=result.ThrowLandedAt>0?arrival-result.ThrowLandedAt:(double?)null,speedMul=result.Thrown?.SpeedMul});
}
Console.WriteLine(JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
```

Run from the worktree root:

```sh
dotnet run --project scratchpad/feel-probe/Probe.csproj -m:1
```

Only the temporary probe project is added. Do not stage its build output. The raw helper emits `throwEndSec = -1` for no throw; the retained JSON converts that sentinel to `null`. Observe the limitations in the report: Crystal Rink, CPU-only, 60-Hz state transitions, inverse-solved contact, and predicted runner arrival. Neither the flag nor its timestamp establishes what an animation or a couch player sees.
