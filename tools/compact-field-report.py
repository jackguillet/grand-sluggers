#!/usr/bin/env python3
"""Reproduce #708 design arithmetic and an optional schematic; never tunes runtime."""
import argparse
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INPUT = ROOT / "docs/research/game-feel-708-candidates.json"
OUTPUT = ROOT / "docs/research/game-feel-708-derived.json"
FIGURE = ROOT / "docs/research/game-feel-708-comparison.png"
STARTS = {"P": (0, 60.5), "C": (0, -15), "1B": (78, 72),
          "2B": (42, 118), "3B": (-78, 72), "SS": (-42, 118),
          "LF": (-110, 250), "CF": (0, 305), "RF": (110, 250)}
BAGS = [(0, 0), (63.64, 63.64), (0, 127.28), (-63.64, 63.64)]


def fence(profile, angle):
    """Symmetric specialization of AtBatResolver.RoundFence, in feet."""
    pole, center, right = profile["fencesFt"]
    assert pole == right, "This research plot supports symmetric candidates only"
    z = pole / math.sqrt(2)
    circle_z = (center * center - pole * pole) / (2 * (center - z))
    radius = center - circle_z
    b = math.cos(math.radians(angle)) * circle_z
    return b + math.sqrt(b * b - circle_z * circle_z + radius * radius)


def derive(data):
    selected = data["acceptedCandidate"]
    assert selected is None or selected in {p["id"] for p in data["profiles"]}
    if selected is not None:
        assert data["acceptedBy"] and data["acceptedOn"] and data["acceptanceEvidence"]
    assert not data["runtimeChangesAuthorizedByThisPacket"]
    proportions = json.loads((ROOT / "docs/research/game-feel-701-proportions.json").read_text())
    control = data["profiles"][0]
    records = []
    for p in data["profiles"]:
        base = p["basepathFt"]
        k = base / 90
        assert math.isclose(p["moundFt"], 60.5 * k)
        for a, expected in zip((-45, 0, 45), p["fencesFt"]):
            assert math.isclose(fence(p, a), expected)
        starts = {}
        for name, (x, z) in STARTS.items():
            scale = k
            if name in ("LF", "CF", "RF"):
                a = math.degrees(math.atan2(x, z))
                scale = fence(p, a) / fence(control, a)
            elif name == "C":
                scale = 1
            starts[name] = [x * scale, z * scale]
        # Constant-speed, straight-ray sensitivity only, excluding read/reach/acceleration.
        of_scale = p["fencesFt"][1] / 400
        rear = p["fencesFt"][1] - starts["CF"][1]
        of_speed = 18.3 * of_scale
        if_speed = 30.5 * k
        records.append({
            "id": p["id"], "basepathFt": base, "fencesFt": p["fencesFt"],
            "wallPerBasepath": [n / base for n in p["fencesFt"]],
            "relativeBodyIncreasePercent": (90 / base - 1) * 100,
            "centerDepthReductionPercent": (1 - of_scale) * 100,
            "bagsFt": [[x * k, z * k] for x, z in BAGS],
            "fieldingStartsFt": starts,
            "groundDressFt": {"innerHalf": 50 * k, "backArcRadius": 92 * k},
            "characters": [{"id": c["character"], "nominalHeadTopFt": c["nominalRestHeadTopFeet"],
                            "headTopPerBasepath": c["nominalRestHeadTopFeet"] / base}
                           for c in proportions["characterRows"]],
            "runnerLinearSpeedAtUnchangedBagTimeFtPerSec": {
                str(run): base / max(2.45, min(3.65, 3.55 - .12 * run)) for run in (1, 5, 9)},
            "constantSpeedSensitivityNotSelected": {
                "sameInfieldChaseTimeRun5FtPerSec": if_speed,
                "sameOutfieldRearChaseTimeRun5FtPerSec": of_speed,
                "centerStartToWallFt": rear,
                "rearChaseTimeSec": rear / of_speed,
                "infieldTimeMultiplierIfUsingOutfieldSensitivity": if_speed / of_speed,
                "limitations": "No acceleration, read, reach, moving interception, fielding route or throw. Two incompatible speeds are sensitivities, not a candidate movement profile."
            },
            "unchanged13FtCatchRadiusOverBasepath": 13 / base,
            "simulated": False,
        })
    tracked = [INPUT, ROOT / "docs/research/game-feel-701-proportions.json",
               ROOT / "src/GrandSluggers.Sim/Diamond.cs", ROOT / "src/GrandSluggers.Sim/AtBatResolver.cs",
               ROOT / "src/GrandSluggers.Sim/Fielding.cs", ROOT / "src/GrandSluggers.Sim/BuntDefense.cs",
               ROOT / "src/GrandSluggers.Sim/ParkDiamond.cs", ROOT / "data/rules/running.json",
               ROOT / "data/rules/fielding.json", ROOT / "data/parks/harbor-diamond.json",
               ROOT / "data/art/clips.json", ROOT / "data/art/baseball-takes.json",
               ROOT / "src/GrandSluggers.Sim/InPlay.cs",
               ROOT / "src/GrandSluggers.Sim/LivePlaySystem.Field.cs",
               ROOT / "src/GrandSluggers.Sim/ChemistryTable.cs",
               ROOT / "src/GrandSluggers.Sim/Match.cs",
               ROOT / "src/GrandSluggers.Sim/FieldAbilities.cs",
               ROOT / "data/characters/vale.json", ROOT / "data/characters/brondo.json",
               ROOT / "data/characters/role-players.json",
               ROOT / "unity/Assets/Scripts/Runtime/Controls.cs",
               ROOT / "unity/Assets/Scripts/Runtime/InPlayDirector.cs"]
    proposal = data.get("runnerClockProposal")
    if proposal:
        bag = max(proposal["bagSeconds"]["min"], min(proposal["bagSeconds"]["max"],
                  proposal["bagSeconds"]["base"] - 5 * proposal["bagSeconds"]["perRunSubtracted"]))
        assert math.isclose(proposal["run5NominalBagSeconds"], bag)
        assert math.isclose(proposal["run5NominalFirstSeconds"], bag + proposal["batterStartupSeconds"])
        assert math.isclose(proposal["run5ControlLinearFeetPerSecond"], 90 / bag)
        assert math.isclose(proposal["run5C80LinearFeetPerSecond"], 80 / bag)
        assert math.isclose(proposal["alternativeKeepWorldSpeedNominalFirstSeconds"],
                            80 / (90 / bag) + proposal["batterStartupSeconds"])
        if proposal["state"] == "accepted-calibration-anchor":
            assert proposal["acceptedBy"] and proposal["acceptedOn"] and proposal["acceptanceEvidence"]
    release = data.get("throwReleaseProposal")
    if release:
        if release["state"] == "accepted-calibration-anchor":
            assert release["acceptedBy"] and release["acceptedOn"] and release["acceptanceEvidence"]
        example = release["arithmeticOnlyExample"]
        reception = sum(example[k] for k in ("possessionSeconds", "humanDecisionSeconds",
                                             "releaseSeconds", "assumedFlightSeconds"))
        assert math.isclose(reception, example["assumedCoveredReceptionSeconds"])
        assert math.isclose(example["runnerNominalArrivalSeconds"] - reception, example["marginSeconds"])
        assert math.isclose(example["releaseSeconds"], release["releaseSeconds"])
    travel = data.get("throwTravelProposal")
    if travel:
        if travel["state"] == "accepted-calibration-anchor":
            assert travel["acceptedBy"] and travel["acceptedOn"] and travel["acceptanceEvidence"]
        seconds_per_foot = travel["referenceFlightSeconds"] / travel["referenceDistanceFeet"]
        assert math.isclose(travel["baselineHorizontalFeetPerSecond"], 1 / seconds_per_foot)
        for sample in travel["samples"]:
            flight = sample["distanceFeet"] * seconds_per_foot
            assert math.isclose(flight, sample["flightSeconds"])
            assert math.isclose(flight + release["releaseSeconds"], sample["commandToTargetSeconds"])
        example = travel["raceExample"]
        assert math.isclose(example["throwDistanceFeet"] * seconds_per_foot, example["flightSeconds"])
        assert math.isclose(example["releaseSeconds"], release["releaseSeconds"])
        reception = sum(example[k] for k in ("possessionSeconds", "humanDecisionSeconds",
                                             "releaseSeconds", "flightSeconds"))
        assert math.isclose(reception, example["coveredReceptionSeconds"])
        assert math.isclose(example["runnerNominalArrivalSeconds"] - reception, example["marginSeconds"])
    long_throw = data.get("longThrowProposal")
    if long_throw:
        if long_throw["state"] == "accepted-design-direction":
            assert long_throw["acceptedBy"] and long_throw["acceptedOn"] and long_throw["acceptanceEvidence"]
            assert long_throw["chemistryQualification"]
        example = long_throw["breakEvenExample"]
        assert math.isclose(sum(example["relayLegFeet"]), example["totalDistanceFeet"])
        speed = example["assumedEqualArmHorizontalFeetPerSecond"]
        release_sec = example["releasePerLegSeconds"]
        assert math.isclose(release_sec, release["releaseSeconds"])
        direct = example["totalDistanceFeet"] / speed + release_sec
        relay = sum(example["relayLegFeet"]) / speed + len(example["relayLegFeet"]) * release_sec + example["assumedExtraRelayDecisionSeconds"]
        assert math.isclose(direct, example["directAtConstantSpeedSeconds"])
        assert math.isclose(relay, example["relayAtConstantSpeedSeconds"])
        assert math.isclose(relay - direct, example["minimumExtraDirectFlightSecondsToTie"])
    chemistry = data.get("goodChemistryProposal")
    if chemistry:
        if chemistry["state"] == "accepted-calibration-anchor":
            assert chemistry["acceptedBy"] and chemistry["acceptedOn"] and chemistry["acceptanceEvidence"]
        boost = chemistry["speedMultiplier"]
        release_sec = chemistry["ordinaryReleaseSeconds"]
        assert math.isclose(release_sec, release["releaseSeconds"])
        simple = chemistry["ordinary80FootExample"]
        assert math.isclose(simple["neutralFlightSeconds"], travel["referenceFlightSeconds"])
        assert math.isclose(simple["goodFlightSeconds"], simple["neutralFlightSeconds"] / boost)
        assert math.isclose(simple["goodCommandToTargetSeconds"], release_sec + simple["goodFlightSeconds"])
        example = chemistry["relayExample"]
        leg = example["neutralFlightEachSeconds"]
        overhead = 2 * release_sec + example["assumedExtraDecisionSeconds"]
        assert math.isclose(example["neutralDirectSeconds"], release_sec + 2 * leg)
        assert math.isclose(example["goodDirectSeconds"], release_sec + 2 * leg / boost)
        assert math.isclose(example["neutralRelaySeconds"], overhead + 2 * leg)
        assert math.isclose(example["oneGoodLegRelaySeconds"], overhead + leg + leg / boost)
        assert math.isclose(example["twoGoodLegsRelaySeconds"], overhead + 2 * leg / boost)
    long_range = data.get("longRangeProfileProposal")
    long_rows = []
    if long_range:
        if long_range["state"] == "accepted-calibration-anchor":
            assert long_range["acceptedBy"] and long_range["acceptedOn"] and long_range["acceptanceEvidence"]
        def flight(distance, field, good=False):
            comfortable = long_range["middleComfortableRangeFeet"] + long_range["rangeFeetPerFieldPoint"] * (field - long_range["middleFieldStat"])
            arm = long_range["armSpeedBase"] + long_range["armSpeedPerFieldPoint"] * field
            speed = travel["baselineHorizontalFeetPerSecond"] * arm
            extra = long_range["extraFlightAtReferenceExcessSeconds"] * (max(0, distance - comfortable) / long_range["referenceExcessFeet"]) ** long_range["excessExponent"]
            return (distance / speed + extra) / (chemistry["speedMultiplier"] if good else 1)

        assert math.isclose(flight(80, 5), travel["referenceFlightSeconds"])
        assert math.isclose(flight(80, 5, True), chemistry["ordinary80FootExample"]["goodFlightSeconds"])
        cutoff_field = long_range["relayComparison"]["cutoffFieldStat"]
        split = long_range["relayComparison"]["splitFraction"]
        decision_gap = long_range["relayComparison"]["extraDecisionSeconds"]
        release_sec = release["releaseSeconds"]
        for field in long_range["comparisonFieldStats"]:
            previous = -1
            for distance in long_range["comparisonDistancesFeet"]:
                ordinary = flight(distance, field)
                assert ordinary > previous
                previous = ordinary
                legs = (distance * split, distance * (1 - split))
                times = {}
                for name, first_good, second_good in (("neutral", False, False), ("firstLegGood", True, False),
                                                      ("secondLegGood", False, True), ("bothLegsGood", True, True)):
                    times[name] = (2 * release_sec + decision_gap + flight(legs[0], field, first_good)
                                   + flight(legs[1], cutoff_field, second_good))
                long_rows.append({"distanceFeet": distance, "throwerField": field, "cutoffField": cutoff_field,
                                  "comfortableRangeFeet": long_range["middleComfortableRangeFeet"] + long_range["rangeFeetPerFieldPoint"] * (field - long_range["middleFieldStat"]),
                                  "neutralDirectCommandToTargetSeconds": release_sec + ordinary,
                                  "goodDirectCommandToTargetSeconds": release_sec + flight(distance, field, True),
                                  "idealRelayCommandToTargetSeconds": times,
                                  "neutralRelayAdvantageSeconds": release_sec + ordinary - times["neutral"],
                                  "scope": "Trial formula; acceptance status in longRangeProfileState. Ideal ready midpoint cutoff, illustrative decision gap; no simulation or guaranteed reception"})
    negative = data.get("negativeChemistryProposal")
    if negative:
        if negative["state"] == "accepted-calibration-anchor":
            assert negative["acceptedBy"] and negative["acceptedOn"] and negative["acceptanceEvidence"]
        factor = negative["badPairTravelSpeedMultiplier"]
        assert 0 < factor < 1
        assert math.isclose(negative["ordinaryReleaseSeconds"], release["releaseSeconds"])
        old = negative["currentRule"]
        assert math.isclose(1 - (1 - old["slantChance"]) ** 2, old["twoBadLegsAtLeastOneSlantProbability"])
        for example in negative["examples"]:
            assert math.isclose(example["neutralFlightSeconds"], flight(example["distanceFeet"], 5))
            assert math.isclose(example["badFlightSeconds"], example["neutralFlightSeconds"] / factor)
            assert math.isclose(example["badCommandToTargetSeconds"], release["releaseSeconds"] + example["badFlightSeconds"])
    relay_control = data.get("relayOwnershipProposal")
    if relay_control and relay_control["state"] == "accepted-design-direction":
        assert relay_control["acceptedBy"] and relay_control["acceptedOn"] and relay_control["acceptanceEvidence"]
    snap = data.get("snapThrowProposal")
    if snap:
        if snap["state"] == "accepted-calibration-anchor":
            assert snap["acceptedBy"] and snap["acceptedOn"] and snap["acceptanceEvidence"]
        assert 0 < snap["eligibleReleaseSeconds"] < snap["ordinaryReleaseSeconds"]
        assert math.isclose(snap["ordinaryReleaseSeconds"], release["releaseSeconds"])
        assert snap["flightSpeedMultiplier"] == 1.0
        for example in snap["examples"]:
            pair_factor = {"neutral": 1.0, "good": 1.3, "bad": 0.9}[example["pair"]]
            expected = flight(example["distanceFeet"], example["fieldStat"]) / pair_factor
            assert math.isclose(example["flightSeconds"], expected)
            assert math.isclose(example["ordinaryTotalSeconds"], expected + snap["ordinaryReleaseSeconds"])
            assert math.isclose(example["snapTotalSeconds"], expected + snap["eligibleReleaseSeconds"])
    laser = data.get("laserThrowProposal")
    if laser:
        if laser["state"] == "accepted-calibration-anchor":
            assert laser["acceptedBy"] and laser["acceptedOn"] and laser["acceptanceEvidence"]
        assert laser["travelSpeedMultiplier"] > 1
        assert math.isclose(laser["ordinaryReleaseSeconds"], release["releaseSeconds"])
        for example in laser["examples"]:
            pair_factor = {"neutral": 1.0, "good": 1.3, "bad": 0.9}[example["pair"]]
            ordinary = flight(example["distanceFeet"], example["fieldStat"]) / pair_factor
            fast = ordinary / laser["travelSpeedMultiplier"]
            assert math.isclose(example["ordinaryFlightSeconds"], ordinary)
            assert math.isclose(example["laserFlightSeconds"], fast)
            assert math.isclose(example["ordinaryTotalSeconds"], release["releaseSeconds"] + ordinary)
            assert math.isclose(example["laserTotalSeconds"], release["releaseSeconds"] + fast)
    buffer = data.get("throwBufferProposal")
    if buffer:
        if buffer["state"] == "accepted-calibration-anchor":
            assert buffer["acceptedBy"] and buffer["acceptedOn"] and buffer["acceptanceEvidence"]
        assert buffer["windowSeconds"] > 0
        for example in buffer["examples"]:
            start = max(example["pressAtSeconds"], example["readyAtSeconds"])
            valid = start - example["pressAtSeconds"] <= buffer["windowSeconds"]
            assert valid == example["valid"]
            if valid:
                assert math.isclose(example["releaseStartsAtSeconds"], start)
            else:
                assert example["releaseStartsAtSeconds"] is None
    cancel = data.get("throwCancelProposal")
    if cancel and cancel["state"] == "accepted-design-direction":
        assert cancel["acceptedBy"] and cancel["acceptedOn"] and cancel["acceptanceEvidence"]
    pursuit = data.get("pursuitSpeedProposal")
    if pursuit:
        if pursuit["state"] == "accepted-calibration-anchor":
            assert pursuit["acceptedBy"] and pursuit["acceptedOn"] and pursuit["acceptanceEvidence"]
        v = pursuit["run5FeetPerSecond"]
        scale = v / pursuit["baselineRun5FeetPerSecond"]
        for row in pursuit["statRows"]:
            assert math.isclose(row["feetPerSecond"], (pursuit["baselineBaseFeetPerSecond"] + row["run"] * pursuit["baselineFeetPerSecondPerRun"]) * scale)
        for row in pursuit["distanceRows"]:
            assert math.isclose(row["secondsAtRun5TopSpeed"], row["distanceFeet"] / v)
        rear = pursuit["rearChase"]
        assert math.isclose(rear["controlTravelSeconds"], rear["controlGapFeet"] / rear["controlOfAirSpeedFeetPerSecond"])
        assert math.isclose(rear["trialTravelSeconds"], rear["c80GapFeet"] / v)
        assert math.isclose(rear["timePreservingSpeedFeetPerSecond"], rear["c80GapFeet"] / rear["controlTravelSeconds"])
        inside = pursuit["infieldSensitivity"]
        assert math.isclose(inside["controlGroundTravelSeconds"], inside["controlDistanceFeet"] / pursuit["baselineRun5FeetPerSecond"])
        assert math.isclose(inside["trialTravelSeconds"], inside["c80DistanceFeet"] / v)
        assert math.isclose(inside["timePreservingSpeedFeetPerSecond"], inside["c80DistanceFeet"] / inside["controlGroundTravelSeconds"])
        for alt in pursuit["alternatives"]:
            assert math.isclose(alt["thirtyFeetSeconds"], 30 / alt["run5FeetPerSecond"])
            assert math.isclose(alt["rearChaseSeconds"], rear["c80GapFeet"] / alt["run5FeetPerSecond"])
    outfield_read = data.get("outfieldReadProposal")
    if outfield_read:
        if outfield_read["state"] == "accepted-calibration-anchor":
            assert outfield_read["acceptedBy"] and outfield_read["acceptedOn"] and outfield_read["acceptanceEvidence"]
        a = outfield_read["arithmetic"]
        earlier = outfield_read["currentSecondsFromContact"] - outfield_read["secondsFromContact"]
        assert math.isclose(a["earlierEligibilitySeconds"], earlier)
        assert math.isclose(a["speedFeetPerSecond"], pursuit["run5FeetPerSecond"])
        assert math.isclose(a["extraPotentialFeetAtRun5TopSpeed"], earlier * a["speedFeetPerSecond"])
        assert math.isclose(a["controlReadPlusRearTravelSeconds"], outfield_read["currentSecondsFromContact"] + pursuit["rearChase"]["controlTravelSeconds"])
        assert math.isclose(a["trialReadPlusRearTravelSeconds"], outfield_read["secondsFromContact"] + pursuit["rearChase"]["trialTravelSeconds"])
    infield_read = data.get("infieldReadProposal")
    if infield_read:
        if infield_read["state"] == "accepted-calibration-anchor":
            assert infield_read["acceptedBy"] and infield_read["acceptedOn"] and infield_read["acceptanceEvidence"]
        a = infield_read["arithmetic"]
        assert set(infield_read["positions"]) == {"1B", "2B", "SS", "3B"}
        assert math.isclose(a["speedFeetPerSecond"], pursuit["run5FeetPerSecond"])
        assert math.isclose(a["outfieldLeadSeconds"], outfield_read["secondsFromContact"] - infield_read["secondsFromContact"])
        assert math.isclose(a["potentialLeadFeetAtRun5TopSpeed"], a["outfieldLeadSeconds"] * a["speedFeetPerSecond"])
        for row in a["positionGains"]:
            delta = infield_read["currentPositionSeconds"][row["position"]] - infield_read["secondsFromContact"]
            assert math.isclose(row["earlierSeconds"], delta, abs_tol=1e-12)
            assert math.isclose(row["potentialExtraFeet"], delta * a["speedFeetPerSecond"], abs_tol=1e-12)
    pitcher_read = data.get("pitcherReadProposal")
    if pitcher_read:
        if pitcher_read["state"] == "accepted-calibration-anchor":
            assert pitcher_read["acceptedBy"] and pitcher_read["acceptedOn"] and pitcher_read["acceptanceEvidence"]
        a = pitcher_read["arithmetic"]
        assert pitcher_read["position"] == "P"
        assert math.isclose(a["earlierThanCurrentSeconds"], pitcher_read["currentSecondsFromContact"] - pitcher_read["secondsFromContact"])
        assert math.isclose(a["laterThanBaseInfieldSeconds"], pitcher_read["secondsFromContact"] - infield_read["secondsFromContact"])
        assert math.isclose(a["earlierThanOutfieldSeconds"], outfield_read["secondsFromContact"] - pitcher_read["secondsFromContact"])
        assert math.isclose(a["speedFeetPerSecond"], pursuit["run5FeetPerSecond"])
        assert math.isclose(a["potentialExtraFeetAtRun5TopSpeed"], a["earlierThanCurrentSeconds"] * a["speedFeetPerSecond"])
    catcher_read = data.get("catcherReadProposal")
    if catcher_read:
        a = catcher_read["arithmetic"]
        assert catcher_read["position"] == "C"
        assert math.isclose(a["earlierThanCurrentSeconds"], catcher_read["currentSecondsFromContact"] - catcher_read["secondsFromContact"])
        assert math.isclose(a["laterThanPitcherSeconds"], catcher_read["secondsFromContact"] - pitcher_read["secondsFromContact"])
        assert math.isclose(a["laterThanBaseInfieldSeconds"], catcher_read["secondsFromContact"] - infield_read["secondsFromContact"])
        assert math.isclose(a["speedFeetPerSecond"], pursuit["run5FeetPerSecond"])
        assert math.isclose(a["potentialExtraFeetAtRun5TopSpeed"], a["earlierThanCurrentSeconds"] * a["speedFeetPerSecond"])
        assert math.isclose(a["controlReadPlusTravelSeconds"], catcher_read["currentSecondsFromContact"] + a["illustrativeChaseFeet"] / pursuit["baselineRun5FeetPerSecond"])
        assert math.isclose(a["trialReadPlusTravelSeconds"], catcher_read["secondsFromContact"] + a["illustrativeChaseFeet"] / pursuit["run5FeetPerSecond"])
    return {"schemaVersion": 1, "status": "derived-design-arithmetic-not-simulation",
            "acceptedLeadSpatialTrial": selected,
            "pitcherReadState": pitcher_read["state"] if pitcher_read else None,
            "infieldReadState": infield_read["state"] if infield_read else None,
            "outfieldReadState": outfield_read["state"] if outfield_read else None,
            "pursuitSpeedState": pursuit["state"] if pursuit else None,
            "throwCancelState": cancel["state"] if cancel else None,
            "throwBufferState": buffer["state"] if buffer else None,
            "laserThrowState": laser["state"] if laser else None,
            "snapThrowState": snap["state"] if snap else None,
            "relayOwnershipState": relay_control["state"] if relay_control else None,
            "negativeChemistryState": negative["state"] if negative else None,
            "longRangeProfileState": long_range["state"] if long_range else None,
            "sourceSha256": {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in tracked},
            "profiles": records, "proposedLongRangeComparisons": long_rows}


def plot(data, result):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.patches import Polygon
    colors = ["#718298", "#158873", "#b77528"]
    fig = plt.figure(figsize=(13, 8.4), facecolor="#f7f5ef")
    grid = fig.add_gridspec(2, 3, height_ratios=[3.1, 1], hspace=.30)
    for i, (p, r, color) in enumerate(zip(data["profiles"], result["profiles"], colors)):
        ax = fig.add_subplot(grid[0, i])
        points = [(0, 0)] + [(math.sin(math.radians(a)) * fence(p, a),
                              math.cos(math.radians(a)) * fence(p, a)) for a in range(-45, 46)]
        ax.add_patch(Polygon(points, facecolor=color, edgecolor=color, alpha=.15))
        diamond = r["bagsFt"] + [r["bagsFt"][0]]
        ax.plot([b[0] for b in diamond], [b[1] for b in diamond], color=color, linewidth=2)
        ax.scatter([b[0] for b in diamond[:4]], [b[1] for b in diamond[:4]], color=color, s=16)
        for name, (x, z) in r["fieldingStartsFt"].items():
            ax.plot(x, z, ".", color="#28394b", markersize=4)
            if name in ("CF", "SS", "P"):
                ax.annotate(name, (x, z), xytext=(4, 0), textcoords="offset points", fontsize=8)
        ax.text(0, p["fencesFt"][1] + 12, f'{p["fencesFt"][1]} ft CF', ha="center", fontsize=10)
        ax.set(xlim=(-250, 250), ylim=(-30, 430), aspect="equal")
        ax.set_title(f'{p["id"]} · {p["label"]}\n{p["basepathFt"]} ft bases · {p["fencesFt"][0]}/{p["fencesFt"][1]}/{p["fencesFt"][2]} ft walls', fontsize=12, pad=18)
        ax.set_facecolor("#f7f5ef")
        ax.set_xticks([])
        ax.set_yticks([0, 100, 200, 300, 400] if i == 0 else [])
        ax.spines[["top", "right", "bottom"]].set_visible(False)
        ax.spines["left"].set_visible(i == 0)
        if i == 0:
            ax.set_ylabel("Feet from home · identical scale in all three plans")
        bx = fig.add_subplot(grid[1, i])
        chars = [next(c for c in r["characters"] if c["id"] == name) for name in ("zig", "rio", "ashlord")]
        values = [100 * c["headTopPerBasepath"] for c in chars]
        bx.barh(["Zig", "Rio", "Ashlord"], values, color=color, height=.55)
        for y, v in enumerate(values):
            bx.text(v + .15, y, f"{v:.2f}%", va="center", fontsize=10)
        bx.set_xlim(0, 14)
        bx.invert_yaxis()
        bx.set_xticks([])
        bx.set_facecolor("#f7f5ef")
        bx.spines[:].set_visible(False)
        bx.tick_params(axis="y", length=0)
        bx.set_title(f'Body / basepath: +{r["relativeBodyIncreasePercent"]:.1f}% vs control', fontsize=11)
    fig.suptitle("Compact field trials · geometry decision only", fontsize=21, x=.06, ha="left", y=.985)
    fig.text(.06, .936, "Ground plans share world scale. Dots show proposed starts, not catch coverage. No candidate has been playtested.", fontsize=11)
    fig.text(.06, .047, "Bars: nominal rest head-top as a percentage of one basepath. Bodies stay the same size; camera, pose and extras are excluded.", fontsize=10)
    fig.text(.06, .021, "C80 and C70 are original trial dimensions informed by conditional Wii evidence; they are not measured Mario dimensions. #693 / #708", fontsize=10)
    fig.subplots_adjust(top=.83, bottom=.13, left=.065, right=.97, wspace=.25)
    fig.savefig(FIGURE, dpi=160, facecolor=fig.get_facecolor())
    plt.close(fig)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verify committed derived arithmetic without rewriting")
    parser.add_argument("--figure", action="store_true", help="Render schematic with matplotlib")
    args = parser.parse_args()
    data = json.loads(INPUT.read_text())
    result = derive(data)
    payload = json.dumps(result, indent=2, allow_nan=False) + "\n"
    if args.check:
        if OUTPUT.read_text() != payload:
            raise SystemExit("Derived report differs; regenerate and review")
    else:
        OUTPUT.write_text(payload)
    if args.figure:
        plot(data, result)
    print(f"#708: three spatial profiles verified; selected spatial trial={data['acceptedCandidate']}; no simulation performed")
