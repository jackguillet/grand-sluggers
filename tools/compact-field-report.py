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


def lateral_closure_seconds(gap, reach, read, accel, top):
    """Shortest hang at which two neighbours cover the whole line between them.

    Straight-line lateral interception only: each fielder waits out the read, accelerates
    linearly for `accel` seconds and holds `top`. No route, height, dive, jump or pose.
    """
    need = gap / 2 - reach
    if need <= 0:
        return 0.0
    ramp = .5 * top * accel
    if need <= ramp:
        return read + math.sqrt(2 * need * accel / top)
    return read + accel + (need - ramp) / top

PAIR_READ = {"3B-SS": "infieldReadSeconds", "SS-2B": "infieldReadSeconds", "2B-1B": "infieldReadSeconds",
             "LF-CF": "outfieldReadSeconds", "CF-RF": "outfieldReadSeconds"}


def reach_coverage(data, starts, base):
    """Closure-hang arithmetic for each reach option; empty when the research block is absent."""
    research = data.get("catchReachCoverageResearch")
    if not research:
        return None
    pursuit = research["pursuitInputs"]
    accel, top = pursuit["accelerationSeconds"], pursuit["topSpeedFeetPerSecond"]
    options = list(research["reachOptionsFt"]) + [13 * base / 90]
    rows = []
    for pair in research["fielderPairs"]:
        a, b = pair.split("-")
        gap = math.dist(starts[a], starts[b])
        read = pursuit[PAIR_READ[pair]]
        rows.append({
            "pair": pair, "spacingFt": gap, "spacingPerBasepath": gap / base, "readSeconds": read,
            "closureHangSecondsByReachFt": {f"{r:g}": lateral_closure_seconds(gap, r, read, accel, top)
                                            for r in sorted(set(round(o, 4) for o in options), reverse=True)},
        })
    return {"scaledOptionFt": 13 * base / 90, "pairs": rows,
            "limitations": research["model"]["excludes"], "simulated": False}

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
            "catchReachCoverage": reach_coverage(data, starts, base),
            "simulated": False,
        })
    tracked = [INPUT, ROOT / "docs/research/game-feel-701-proportions.json",
               ROOT / "src/GrandSluggers.Sim/Diamond.cs", ROOT / "src/GrandSluggers.Sim/AtBatResolver.cs",
               ROOT / "src/GrandSluggers.Sim/BallFlight.cs", ROOT / "src/GrandSluggers.Sim/BattedBall.cs",
               ROOT / "src/GrandSluggers.Sim/Fielding.cs", ROOT / "src/GrandSluggers.Sim/FlyCatch.cs", ROOT / "src/GrandSluggers.Sim/BuntDefense.cs",
               ROOT / "src/GrandSluggers.Sim/ParkDiamond.cs", ROOT / "data/rules/running.json",
               ROOT / "data/rules/fielding.json", ROOT / "data/parks/harbor-diamond.json",
               ROOT / "data/art/clips.json", ROOT / "data/art/baseball-takes.json",
               ROOT / "src/GrandSluggers.Sim/InPlay.cs",
               ROOT / "src/GrandSluggers.Sim/LivePlaySystem.Field.cs",
               ROOT / "src/GrandSluggers.Sim/BodyFacing.cs",
               ROOT / "src/GrandSluggers.Sim/FieldAssist.cs",
               ROOT / "src/GrandSluggers.Sim/StickPlay.cs",
               ROOT / "data/feel/table.json",
               ROOT / "src/GrandSluggers.Sim/FieldingPursuit.cs",
               ROOT / "src/GrandSluggers.Sim/ChemistryTable.cs",
               ROOT / "src/GrandSluggers.Sim/Match.cs",
               ROOT / "src/GrandSluggers.Sim/FieldAbilities.cs",
               ROOT / "data/characters/vale.json", ROOT / "data/characters/brondo.json",
               ROOT / "data/characters/role-players.json",
               ROOT / "unity/Assets/Scripts/Runtime/Controls.cs",
               ROOT / "unity/Assets/Scripts/Runtime/InPlayDirector.cs",
               ROOT / "unity/Assets/Scripts/Runtime/AtBatDirector.cs",
               ROOT / "unity/Assets/Scripts/Runtime/MatchDirector.cs",
               ROOT / "src/GrandSluggers.Sim/Seats.cs",
               ROOT / "src/GrandSluggers.Sim/AtBatFeel.cs",
               ROOT / "src/GrandSluggers.Sim/Rules.cs",
               ROOT / "data/abilities/star-skills.json",
               ROOT / "src/GrandSluggers.Sim/StarSkillTable.cs",
               ROOT / "src/GrandSluggers.Sim/ContentValidation.cs",
               ROOT / "src/GrandSluggers.Sim/Models.cs"]
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
        if catcher_read["state"] == "accepted-calibration-anchor":
            assert catcher_read["acceptedBy"] and catcher_read["acceptedOn"] and catcher_read["acceptanceEvidence"]
        a = catcher_read["arithmetic"]
        assert catcher_read["position"] == "C"
        assert math.isclose(a["earlierThanCurrentSeconds"], catcher_read["currentSecondsFromContact"] - catcher_read["secondsFromContact"])
        assert math.isclose(a["laterThanPitcherSeconds"], catcher_read["secondsFromContact"] - pitcher_read["secondsFromContact"])
        assert math.isclose(a["laterThanBaseInfieldSeconds"], catcher_read["secondsFromContact"] - infield_read["secondsFromContact"])
        assert math.isclose(a["speedFeetPerSecond"], pursuit["run5FeetPerSecond"])
        assert math.isclose(a["potentialExtraFeetAtRun5TopSpeed"], a["earlierThanCurrentSeconds"] * a["speedFeetPerSecond"])
        assert math.isclose(a["controlReadPlusTravelSeconds"], catcher_read["currentSecondsFromContact"] + a["illustrativeChaseFeet"] / pursuit["baselineRun5FeetPerSecond"])
        assert math.isclose(a["trialReadPlusTravelSeconds"], catcher_read["secondsFromContact"] + a["illustrativeChaseFeet"] / pursuit["run5FeetPerSecond"])
    acceleration = data.get("pursuitAccelerationProposal")
    if acceleration:
        if acceleration["state"] == "accepted-calibration-anchor":
            assert acceleration["acceptedBy"] and acceleration["acceptedOn"] and acceleration["acceptanceEvidence"]
        t = acceleration["restToFullSeconds"]
        a = acceleration["run5Arithmetic"]
        v = pursuit["run5FeetPerSecond"]
        assert t > 0 and acceleration["curve"] == "linear-speed-ramp"
        assert math.isclose(a["topSpeedFeetPerSecond"], v)
        assert math.isclose(a["accelerationFeetPerSecondSquared"], v / t)
        half = a["halfRampSeconds"]
        assert math.isclose(half, t / 2)
        assert math.isclose(a["halfRampSpeedFeetPerSecond"], v * half / t)
        assert math.isclose(a["halfRampDistanceFeet"], v * half * half / (2*t))
        assert math.isclose(a["rampDistanceFeet"], v*t/2)
        assert math.isclose(a["instantStartDistanceFeet"], v*t)
        assert math.isclose(a["distanceDeficitFeet"], a["instantStartDistanceFeet"] - a["rampDistanceFeet"])
        assert math.isclose(a["longStraightDelaySeconds"], t/2)
        assert a["illustrativeChaseFeet"] > a["rampDistanceFeet"]
        assert math.isclose(a["instantStartTravelSeconds"], a["illustrativeChaseFeet"]/v)
        assert math.isclose(a["rampedTravelSeconds"], a["illustrativeChaseFeet"]/v+t/2)
        for row in acceleration["characterArithmetic"]:
            speed = (21+1.9*row["run"])*v/pursuit["baselineRun5FeetPerSecond"]
            assert math.isclose(row["speedFeetPerSecond"], speed)
            assert math.isclose(row["rampDistanceFeet"], speed*t/2)
        for name, read in (("baseInfield", infield_read), ("pitcher", pitcher_read), ("outfield", outfield_read), ("catcher", catcher_read)):
            assert math.isclose(acceleration["uncappedContactToFullSpeedSeconds"][name], read["secondsFromContact"]+t)
    braking = data.get("pursuitBrakingProposal")
    if braking:
        if braking["state"] == "accepted-calibration-anchor":
            assert braking["acceptedBy"] and braking["acceptedOn"] and braking["acceptanceEvidence"]
        t = braking["fullSpeedToStopSeconds"]
        a = braking["run5Arithmetic"]
        v = pursuit["run5FeetPerSecond"]
        b = v/t
        assert t > 0 and braking["curve"] == "constant-deceleration-to-zero"
        assert math.isclose(a["topSpeedFeetPerSecond"], v)
        assert math.isclose(a["decelerationFeetPerSecondSquared"], b)
        assert math.isclose(a["fullStopDistanceFeet"], v*v/(2*b))
        assert math.isclose(a["halfSpeedFeetPerSecond"], v/2)
        u = a["halfSpeedFeetPerSecond"]
        assert math.isclose(a["halfSpeedStopSeconds"], u/b)
        assert math.isclose(a["halfSpeedStopDistanceFeet"], u*u/(2*b))
        ramp = acceleration["restToFullSeconds"]
        assert math.isclose(a["accelerationSeconds"], ramp)
        assert math.isclose(a["brakingToAccelerationDurationRatio"], t/ramp)
        assert math.isclose(a["fullRampMinimumRestToRestFeet"], v*(ramp+t)/2)
        assert a["illustrativeRestToRestFeet"] >= a["fullRampMinimumRestToRestFeet"]
        assert math.isclose(a["restToRestSeconds"], a["illustrativeRestToRestFeet"]/v+(ramp+t)/2)
        for row in braking["characterArithmetic"]:
            speed = (21+1.9*row["run"])*v/pursuit["baselineRun5FeetPerSecond"]
            assert math.isclose(row["speedFeetPerSecond"], speed)
            assert math.isclose(row["fullStopDistanceFeet"], speed*t/2)
        for row in braking["alternatives"]:
            assert math.isclose(row["run5FullStopDistanceFeet"], v*row["seconds"]/2)
    reversal = data.get("pursuitReversalProposal")
    if reversal:
        if reversal["state"] == "accepted-calibration-anchor":
            assert reversal["acceptedBy"] and reversal["acceptedOn"] and reversal["acceptanceEvidence"]
        tb = braking["fullSpeedToStopSeconds"]
        ta = acceleration["restToFullSeconds"]
        v = pursuit["run5FeetPerSecond"]
        a = reversal["run5Arithmetic"]
        assert math.isclose(reversal["usesAcceptedBrakingSeconds"], tb)
        assert math.isclose(reversal["usesAcceptedAccelerationSeconds"], ta)
        assert math.isclose(a["speedFeetPerSecond"], v)
        assert math.isclose(a["oppositeMotionBeginsSeconds"], tb)
        assert math.isclose(a["oppositeFullSpeedSeconds"], tb+ta)
        assert math.isclose(a["wrongWayExcursionFeet"], v*tb/2)
        assert math.isclose(a["newDirectionTravelDuringAccelerationFeet"], v*ta/2)
        assert math.isclose(a["positionAtOppositeFullSpeedFeet"], v*(tb-ta)/2)
        assert tb <= ta, "return-to-origin example assumes it occurs during acceleration"
        assert math.isclose(a["returnsToCommandPositionSeconds"], tb+math.sqrt(tb*ta))
        assert math.isclose(a["halfInitialSpeedFullReversalSeconds"], tb/2+ta)
        assert math.isclose(a["halfInitialSpeedWrongWayFeet"], v*tb/8)
        for row in reversal["characterArithmetic"]:
            speed = (21+1.9*row["run"])*v/pursuit["baselineRun5FeetPerSecond"]
            assert math.isclose(row["speedFeetPerSecond"], speed)
            assert math.isclose(row["wrongWayExcursionFeet"], speed*tb/2)
    turning = data.get("pursuitAngledTurnProposal")
    if turning:
        if turning["state"] == "accepted-calibration-anchor":
            assert turning["acceptedBy"] and turning["acceptedOn"] and turning["acceptanceEvidence"]
        tb, ta = braking["fullSpeedToStopSeconds"], acceleration["restToFullSeconds"]
        v = pursuit["run5FeetPerSecond"]
        assert math.isclose(turning["usesAcceptedBrakingSeconds"], tb)
        assert math.isclose(turning["usesAcceptedAccelerationSeconds"], ta)
        for row in turning["fullSpeedAngleArithmetic"]:
            angle = math.radians(row["angleDegrees"])
            sh, ch = math.sin(angle/2), math.cos(angle/2)
            assert math.isclose(row["brakingPhaseSeconds"], tb*sh, abs_tol=1e-12)
            assert math.isclose(row["accelerationPhaseSeconds"], ta*sh, abs_tol=1e-12)
            assert math.isclose(row["completeSeconds"], (tb+ta)*sh, abs_tol=1e-12)
            assert math.isclose(row["minimumSpeedFraction"], ch, abs_tol=1e-12)
            assert math.isclose(row["run5MinimumSpeedFeetPerSecond"], v*ch, abs_tol=1e-12)
            # Independent endpoint geometry: chord midpoint gives the speed minimum.
            qx, qz = v*math.cos(angle), v*math.sin(angle)
            chord = math.hypot(qx-v, qz)
            assert math.isclose(chord/2/(v/tb)+chord/2/(v/ta), row["completeSeconds"], abs_tol=1e-12)
            assert math.isclose(math.hypot((v+qx)/2, qz/2), row["run5MinimumSpeedFeetPerSecond"], abs_tol=1e-12)
            for f in (0, .25, .5, .75, 1):
                assert math.hypot(v*(1-f)+qx*f, qz*f) <= v+1e-10
        a = turning["run5RightAngleDisplacement"]
        t1, t2 = tb/math.sqrt(2), ta/math.sqrt(2)
        assert math.isclose(a["oldDirectionFeetAtCompletion"], v*(.75*t1+.25*t2))
        assert math.isclose(a["newDirectionFeetAtCompletion"], v*(.25*t1+.75*t2))
    analog = data.get("pursuitAnalogProposal")
    if analog:
        if analog["state"] == "accepted-calibration-anchor":
            assert analog["acceptedBy"] and analog["acceptedOn"] and analog["acceptanceEvidence"]
        assert analog["curve"] == "linear-active-radial-travel"
        a = analog["run5Arithmetic"]
        v = pursuit["run5FeetPerSecond"]
        accel = v/acceleration["restToFullSeconds"]
        brake = v/braking["fullSpeedToStopSeconds"]
        assert math.isclose(a["topSpeedFeetPerSecond"], v)
        for name, fraction in (("quarter", .25), ("half", .5), ("full", 1)):
            assert math.isclose(a[name+"ActiveTargetFeetPerSecond"], v*fraction)
        half = v/2
        assert math.isclose(a["restToHalfSeconds"], half/accel)
        assert math.isclose(a["restToHalfDistanceFeet"], half*a["restToHalfSeconds"]/2)
        assert math.isclose(a["fullToHalfSeconds"], half/brake)
        assert math.isclose(a["fullToHalfDistanceFeet"], (v+half)*a["fullToHalfSeconds"]/2)
        assert math.isclose(a["halfToFullSeconds"], half/accel)
        assert math.isclose(a["halfToFullDistanceFeet"], (v+half)*a["halfToFullSeconds"]/2)
        # Algebraic check across illustrative neutral radii, not selected thresholds.
        for z in (0, .2, .4):
            for fraction in (0, .25, .5, 1):
                magnitude = z+(1-z)*fraction
                assert math.isclose((magnitude-z)/(1-z), fraction, abs_tol=1e-12)
    neutral = data.get("pursuitNeutralProposal")
    if neutral:
        if neutral["state"] == "accepted-calibration-anchor":
            assert neutral["acceptedBy"] and neutral["acceptedOn"] and neutral["acceptanceEvidence"]
            assert math.isclose(analog["neutralRadius"], neutral["manualSpeedZeroRadius"])
            assert analog["neutralRadiusDecision"] == neutral["decisionId"]
        enter, leave = neutral["manualEnterRadius"], neutral["manualExitRadius"]
        z = neutral["manualSpeedZeroRadius"]
        assert 0 <= leave < enter < 1 and z == leave
        a = neutral["run5Arithmetic"]
        v = pursuit["run5FeetPerSecond"]
        assert math.isclose(a["topSpeedFeetPerSecond"], v)
        assert math.isclose(a["ownershipBandWidth"], enter-leave)
        assert math.isclose(a["manualTargetFractionAtEntry"], (enter-z)/(1-z))
        assert math.isclose(a["manualTargetSpeedAtEntryFeetPerSecond"], v*(enter-z)/(1-z))
        assert math.isclose(a["halfActiveRangeMagnitude"], z+(1-z)/2)
        assert math.isclose(a["halfActiveTargetFeetPerSecond"], v/2)
        assert math.isclose(a["manualTargetAtMagnitude018FeetPerSecond"], v*(.18-z)/(1-z))
        assert math.isclose(a["manualTargetAtMagnitude050FeetPerSecond"], v*(.50-z)/(1-z))
        for row in neutral["ownershipExamples"]:
            magnitude = row["magnitude"]
            after = True if magnitude >= enter else False if magnitude <= leave else row["manualBefore"]
            assert after == row["manualAfter"]
    calibration = data.get("pursuitCalibrationProposal")
    if calibration and calibration["state"] == "accepted-calibration-anchor":
        assert calibration["acceptedBy"] and calibration["acceptedOn"] and calibration["acceptanceEvidence"]
    arming = data.get("pursuitArmingProposal")
    if arming:
        if arming["state"] == "accepted-calibration-anchor":
            assert arming["acceptedBy"] and arming["acceptedOn"] and arming["acceptanceEvidence"]
        assert arming["neutralRadius"] == neutral["manualExitRadius"]
        assert arming["extraNeutralDwellSeconds"] == 0
        for row in arming["exampleChecks"]:
            expected = row["profileValid"] and (row["wasArmed"] or row["magnitude"] <= arming["neutralRadius"])
            assert expected == row["armedAfter"]
    samples = data.get("pursuitCalibrationSamplesProposal")
    if samples:
        if samples["state"] == "accepted-calibration-anchor":
            assert samples["acceptedBy"] and samples["acceptedOn"] and samples["acceptanceEvidence"]
        offset, deviation = samples["maxCenterOffsetRadius"], samples["maxSampleDeviationRadius"]
        window = samples["windowSeconds"]
        rel = samples["relationships"]
        assert math.isclose(rel["maxSampleRadiusByTriangleBound"], offset+deviation)
        assert math.isclose(rel["calibratedSampleDeviationBound"], deviation)
        assert rel["armingNeutralRadius"] == arming["neutralRadius"]
        assert math.isclose(rel["deviationMarginBelowArmingRadius"], arming["neutralRadius"]-deviation)
        for case in samples["syntheticWindows"]:
            rows = case["samples"]
            assert all(rows[i]["t"] < rows[i+1]["t"] for i in range(len(rows)-1))
            cx = sum(row["x"] for row in rows)/len(rows)
            cy = sum(row["y"] for row in rows)/len(rows)
            spread = max(math.hypot(row["x"]-cx,row["y"]-cy) for row in rows)
            valid = rows[-1]["t"]-rows[0]["t"] >= window-1e-12 and math.hypot(cx,cy) <= offset+1e-12 and spread <= deviation+1e-12
            assert valid == case["accepted"], case["name"]
    dash = data.get("fieldDashPeakProposal")
    if dash:
        if dash["state"] == "accepted-calibration-anchor":
            assert dash["acceptedBy"] and dash["acceptedOn"] and dash["acceptanceEvidence"]
        mul = dash["peakSpeedMultiplier"]
        a = dash["run5Arithmetic"]
        v = pursuit["run5FeetPerSecond"]
        assert mul > 1 and math.isclose(a["ordinaryFeetPerSecond"], v)
        assert math.isclose(a["dashPeakFeetPerSecond"], v*mul)
        assert math.isclose(a["extraFeetPerSecondAtPeak"], v*(mul-1))
        assert math.isclose(a["legacyMultiplierOnTrialBaseFeetPerSecond"], v*dash["currentHeldMultiplier"])
        assert math.isclose(a["legacyMultiplierOnTrialExtraFeetPerSecond"], v*(dash["currentHeldMultiplier"]-1))
        distance = a["illustrativeDistanceFeet"]
        assert math.isclose(a["ordinaryConstantSpeedTravelSeconds"], distance/v)
        assert math.isclose(a["peakConstantSpeedTravelSeconds"], distance/(v*mul))
        assert math.isclose(a["constantSpeedTimeSavingSeconds"], distance/v-distance/(v*mul))
        for row in dash["characterArithmetic"]:
            speed = (21+1.9*row["run"])*v/pursuit["baselineRun5FeetPerSecond"]
            assert math.isclose(row["ordinaryFeetPerSecond"], speed)
            assert math.isclose(row["dashPeakFeetPerSecond"], speed*mul)
        for row in dash["alternatives"]:
            assert math.isclose(row["run5PeakFeetPerSecond"], v*row["multiplier"])
    duration = data.get("fieldDashDurationProposal")
    if duration:
        seconds = duration["maxBurstSeconds"]
        row = duration["run5Arithmetic"]
        v = pursuit["run5FeetPerSecond"]
        peak = v*dash["peakSpeedMultiplier"]
        assert seconds > 0 and row["windowSeconds"] == seconds
        assert math.isclose(row["ordinaryFeetPerSecond"], v)
        assert math.isclose(row["peakFeetPerSecond"], peak)
        assert math.isclose(row["ordinaryConstantSpeedDistanceFeet"], v*seconds)
        assert math.isclose(row["peakConstantSpeedDistanceFeet"], peak*seconds)
        assert math.isclose(row["extraDistanceWithinWindowFeet"], (peak-v)*seconds)
        for alternative in duration["alternatives"]:
            assert math.isclose(alternative["run5IdealExtraDistanceFeet"], (peak-v)*alternative["seconds"])
    carrier = data.get("ballDashCarrierProposal")
    if carrier and carrier["state"] == "accepted-calibration-anchor":
        assert carrier["acceptedBy"] and carrier["acceptedOn"] and carrier["acceptanceEvidence"]
    carry = data.get("ordinaryCarrySpeedProposal")
    if carry:
        if carry["state"] == "accepted-calibration-anchor":
            assert carry["acceptedBy"] and carry["acceptedOn"] and carry["acceptanceEvidence"]
        ratio = carry["ordinaryPursuitMultiplier"]
        boost = carrier["speedMultiplier"]
        for row in carry["characterArithmetic"]:
            speed = (21+1.9*row["run"])*pursuit["run5FeetPerSecond"]/pursuit["baselineRun5FeetPerSecond"]*ratio
            assert math.isclose(row["ordinaryCarryFeetPerSecond"], speed)
            assert math.isclose(row["ballDashCarryFeetPerSecond"], speed*boost)
        row = carry["run5Comparison"]
        v = pursuit["run5FeetPerSecond"]*ratio
        assert math.isclose(row["ordinaryConstantSpeedCarrySeconds"], row["distanceFeet"]/v)
        assert math.isclose(row["ballDashConstantSpeedCarrySeconds"], row["distanceFeet"]/(v*boost))
        assert math.isclose(row["ordinaryNeutralThrowFlightSeconds"], row["distanceFeet"]/(80/.9))
        assert math.isclose(row["ordinaryNeutralThrowReleaseSeconds"], .30)
        assert math.isclose(row["ordinaryNeutralReleasePlusFlightSeconds"], row["ordinaryNeutralThrowReleaseSeconds"]+row["ordinaryNeutralThrowFlightSeconds"])
        for alt in carry["alternatives"]:
            assert math.isclose(alt["run5OrdinaryCarryFeetPerSecond"], pursuit["run5FeetPerSecond"]*alt["ordinaryPursuitMultiplier"])
            assert math.isclose(alt["run5BallDashCarryFeetPerSecond"], alt["run5OrdinaryCarryFeetPerSecond"]*boost)
    carry_response = data.get("carryMovementResponseProposal")
    if carry_response:
        if carry_response["state"] == "accepted-calibration-anchor":
            assert carry_response["acceptedBy"] and carry_response["acceptedOn"] and carry_response["acceptanceEvidence"]
        assert carry_response["rateBasis"] == "unboosted-ordinary-character-speed"
        assert carry_response["response"] == turning["response"]
        ta = acceleration["restToFullSeconds"]
        tb = braking["fullSpeedToStopSeconds"]
        assert carry_response["ordinaryAccelerationSeconds"] == ta
        assert carry_response["ordinaryBrakingSeconds"] == tb
        v = pursuit["run5FeetPerSecond"]
        peak = v*carrier["speedMultiplier"]
        a, b = v/ta, v/tb
        expected = dict(ordinarySpeedFeetPerSecond=v, ballDashSpeedFeetPerSecond=peak,
            accelerationFeetPerSecondSquared=a, brakingFeetPerSecondSquared=b,
            ordinaryRestToFullSeconds=ta, ordinaryFullStopSeconds=tb, ordinaryFullReverseSeconds=ta+tb,
            ballDashRestToFullSeconds=peak/a, ballDashStartDistanceFeet=peak*peak/(2*a),
            ballDashFullStopSeconds=peak/b, ballDashStopDistanceFeet=peak*peak/(2*b),
            ballDashFullReverseSeconds=peak/b+peak/a,
            ordinaryToBallDashSameDirectionSeconds=(peak-v)/a,
            ordinaryToBallDashDistanceFeet=(peak+v)/2*(peak-v)/a,
            ballDashToOrdinarySameDirectionSeconds=(peak-v)/b,
            ballDashToOrdinaryDistanceFeet=(peak+v)/2*(peak-v)/b,
            ballDashExitExcessDistanceFeet=(peak-v)**2/(2*b))
        for key, value in expected.items():
            assert math.isclose(carry_response["run5Arithmetic"][key], value), key
        for row in carry_response["boostedTurnArithmetic"]:
            angle = math.radians(row["angleDegrees"]/2)
            assert math.isclose(row["completionSeconds"], carrier["speedMultiplier"]*(ta+tb)*math.sin(angle))
            assert math.isclose(row["minimumSpeedFraction"], math.cos(angle), abs_tol=1e-12)
    pickup = data.get("cleanGroundPickupReadinessProposal")
    if pickup:
        if pickup["state"] == "accepted-calibration-anchor":
            assert pickup["acceptedBy"] and pickup["acceptedOn"] and pickup["acceptanceEvidence"]
        assert pickup["addedRecoverySeconds"] == 0
        for row in pickup["examples"]:
            ready = row["securePossessionSeconds"]+pickup["addedRecoverySeconds"]
            start = max(ready, row["commandSeconds"])
            age = start-row["commandSeconds"]
            assert math.isclose(row["bufferAgeAtReadinessSeconds"], age, abs_tol=1e-12)
            valid = age <= .25+1e-12
            assert row["bufferValid"] == valid
            if valid:
                assert math.isclose(row["releaseStartSeconds"], start)
                assert math.isclose(row["ballReleaseSeconds"], start+.30)
            else:
                assert row["releaseStartSeconds"] is None and row["ballReleaseSeconds"] is None
    recoil_basis = data.get("groundPickupRecoilBasisProposal")
    if recoil_basis and recoil_basis["state"] == "accepted-calibration-anchor":
        assert recoil_basis["acceptedBy"] and recoil_basis["acceptedOn"] and recoil_basis["acceptanceEvidence"]
    recoil_cap = data.get("groundPickupRecoilCapProposal")
    if recoil_cap:
        if recoil_cap["state"] == "accepted-calibration-anchor":
            assert recoil_cap["acceptedBy"] and recoil_cap["acceptedOn"] and recoil_cap["acceptanceEvidence"] and recoil_cap["specialHitException"]
        cap = recoil_cap["maxRecoverySeconds"]
        row = recoil_cap["run5Arithmetic"]
        assert cap > 0
        assert row["nominalBagSeconds"] == proposal["run5NominalBagSeconds"]
        speed = row["basepathFeet"]/row["nominalBagSeconds"]
        assert math.isclose(row["ordinaryRunnerFeetPerSecond"], speed)
        assert math.isclose(row["runnerAdvanceDuringMaxRecoveryFeet"], speed*cap)
        assert math.isclose(row["recoilPlusOrdinaryReleaseSeconds"], cap+.30)
        assert math.isclose(row["runnerAdvanceDuringOldCapFeet"], speed*row["baselineOldRecoveryCapSeconds"])
        for alt in recoil_cap["alternatives"]:
            assert math.isclose(alt["run5RunnerAdvanceFeet"], speed*alt["maxRecoverySeconds"])
        for ex in recoil_cap["commandExamples"]:
            assert ex["recoverySeconds"] <= cap
            assert math.isclose(ex["readySeconds"], ex["possessionSeconds"]+ex["recoverySeconds"])
            age = max(0, ex["readySeconds"]-ex["commandSeconds"])
            assert math.isclose(ex["ageAtReadinessSeconds"], age)
            valid = age <= buffer["windowSeconds"]+1e-12
            assert ex["valid"] == valid
            if valid:
                assert math.isclose(ex["releaseStartSeconds"], max(ex["readySeconds"], ex["commandSeconds"]))
                assert math.isclose(ex["ballReleaseSeconds"], ex["releaseStartSeconds"]+.30)
            else:
                assert ex["releaseStartSeconds"] is None and ex["ballReleaseSeconds"] is None
    composition = data.get("specialRecoveryCompositionProposal")
    if composition:
        if composition["state"] == "accepted-calibration-anchor":
            assert composition["acceptedBy"] and composition["acceptedOn"] and composition["acceptanceEvidence"]
        for case in composition["examples"]:
            assert 0 <= case["ordinarySeconds"] <= recoil_cap["maxRecoverySeconds"]
            assert math.isclose(case["totalSeconds"], case["ordinarySeconds"]+case["specialSeconds"])
            assert math.isclose(case["readySeconds"], case["eventSeconds"]+case["totalSeconds"])
        low, high = composition["examples"][:2]
        value = composition["fieldingValue"]
        assert math.isclose(value["ordinaryDifferenceSeconds"], high["ordinarySeconds"]-low["ordinarySeconds"])
        assert math.isclose(value["totalDifferenceSeconds"], high["totalSeconds"]-low["totalSeconds"])
        assert math.isclose(value["previousOverlapDifferenceSeconds"], max(high["ordinarySeconds"],high["specialSeconds"])-max(low["ordinarySeconds"],low["specialSeconds"]), abs_tol=1e-12)
    field_shape = data.get("recoilFieldShapingProposal")
    if field_shape:
        if field_shape["state"] == "accepted-calibration-anchor":
            assert field_shape["acceptedBy"] and field_shape["acceptedOn"] and field_shape["acceptanceEvidence"]
        for row in field_shape["illustrations"]:
            assert 0 <= row["severity"] <= 1
            assert 0 < row["illustrativeFieldFactor"] <= 1
            assert math.isclose(row["ordinarySeconds"], recoil_cap["maxRecoverySeconds"]*row["severity"]*row["illustrativeFieldFactor"])
    field_factors = data.get("recoilFieldFactorsProposal")
    if field_factors:
        if field_factors["state"] == "accepted-calibration-anchor":
            assert field_factors["acceptedBy"] and field_factors["acceptedOn"] and field_factors["acceptanceEvidence"]
        cap = recoil_cap["maxRecoverySeconds"]
        rows = field_factors["rows"]
        assert [r["field"] for r in rows] == list(range(field_factors["minField"],field_factors["maxField"]+1))
        for row in rows:
            factor = field_factors["factorAtMinField"]-field_factors["factorReductionPerPoint"]*(row["field"]-field_factors["minField"])
            assert 0 < factor <= 1 and math.isclose(row["factor"],factor)
            assert math.isclose(row["fullSeveritySeconds"],cap*factor)
            assert math.isclose(row["halfSeveritySeconds"],cap*.5*factor)
        rel = field_factors["relationships"]
        diff = rows[0]["fullSeveritySeconds"]-rows[-1]["fullSeveritySeconds"]
        assert math.isclose(rel["fullSeveritySecondsSavedPerPoint"],cap*field_factors["factorReductionPerPoint"])
        assert math.isclose(rel["field1To10MaxDifferenceSeconds"],diff)
        assert math.isclose(rel["field10ReductionFraction"],1-rows[-1]["factor"])
        assert math.isclose(rel["run5SteadyRunnerDistanceForMaxDifferenceFeet"],80/proposal["run5NominalBagSeconds"]*diff)
        for alt in field_factors["alternatives"]:
            factor = 1-alt["reductionPerPoint"]*9
            assert math.isclose(alt["field10Factor"],factor)
            assert math.isclose(alt["field10MaxSeconds"],cap*factor)
    severity_curve = data.get("recoilSeverityCurveProposal")
    if severity_curve:
        if severity_curve["state"] == "accepted-calibration-anchor":
            assert severity_curve["acceptedBy"] and severity_curve["acceptedOn"] and severity_curve["acceptanceEvidence"]
        assert severity_curve["curve"] == "clamped-linear-between-two-speed-anchors"
        assert severity_curve["onsetFeetPerSecond"] is None and severity_curve["fullSeverityFeetPerSecond"] is None
        for row in severity_curve["normalizedExamples"]:
            severity = max(0,min(1,row["normalizedSpeedPosition"]))
            assert math.isclose(row["severity"],severity)
            for field in (1,5,10):
                factor = field_factors["factorAtMinField"]-field_factors["factorReductionPerPoint"]*(field-field_factors["minField"])
                assert math.isclose(row["field%dSeconds" % field],recoil_cap["maxRecoverySeconds"]*severity*factor)
    recoil_actions = data.get("ordinaryRecoilActionsProposal")
    if recoil_actions:
        if recoil_actions["state"] == "accepted-calibration-anchor":
            assert recoil_actions["acceptedBy"] and recoil_actions["acceptedOn"] and recoil_actions["acceptanceEvidence"]
        for case in recoil_actions["syntheticCases"]:
            eligible = case["secure"] and (case["legalForceContact"] or case["legalTagContact"])
            assert case["contactEligible"] == eligible
    displacement = data.get("ordinaryRecoilDisplacementProposal")
    if displacement:
        if displacement["state"] == "accepted-calibration-anchor":
            assert displacement["acceptedBy"] and displacement["acceptedOn"] and displacement["acceptanceEvidence"]
        assert all(displacement[key] is None for key in ("maxDisplacementFeet", "impulseFeetPerSecond", "velocityResponse"))
    displacement_cap = data.get("ordinaryRecoilDistanceCapProposal")
    if displacement_cap:
        if displacement_cap["state"] == "accepted-calibration-anchor":
            assert displacement_cap["acceptedBy"] and displacement_cap["acceptedOn"] and displacement_cap["acceptanceEvidence"]
        assert displacement_cap["maxAddedImpactDisplacementFeet"] > 0
        basepath = next(p["basepathFt"] for p in data["profiles"] if p["id"] == selected)
        for row in displacement_cap["comparisons"]:
            assert math.isclose(row["percentOfC80Basepath"], 100*row["capFeet"]/basepath)
    motion = data.get("ordinaryRecoilMotionProfileProposal")
    if motion:
        if motion["state"] == "accepted-calibration-anchor":
            assert motion["acceptedBy"] and motion["acceptedOn"] and motion["acceptanceEvidence"]
        for row in motion["examples"]:
            w = row["severity"]*(1-.05*(row["field"]-1))
            t, k = .20*w, 10*w
            assert math.isclose(row["recoverySeconds"], t)
            assert math.isclose(row["initialImpactFeetPerSecond"], k)
            assert math.isclose(row["impactDistanceFeet"], k*t/2)
            assert math.isclose(row["impactDistanceInches"], 12*w*w)
            assert 0 <= row["impactDistanceFeet"] <= displacement_cap["maxAddedImpactDisplacementFeet"]
            if t > 0:
                assert math.isclose(k/t, motion["impactDecelerationFeetPerSecondSquared"])
                midpoint = t*.37
                first = k*(midpoint-midpoint*midpoint/(2*t))
                remaining_speed = k*(1-midpoint/t)
                second = remaining_speed*(t-midpoint)/2
                assert math.isclose(first+second, row["impactDistanceFeet"])
        for field in range(1,11):
            factor = 1-.05*(field-1)
            previous = -1
            for severity in (0, 1e-9, .01, .25, .5, 1):
                distance = (severity*factor)**2
                assert distance >= previous
                previous = distance
    special_motion = data.get("specialImpactMotionCompositionProposal")
    if special_motion:
        if special_motion["state"] == "accepted-calibration-anchor":
            assert special_motion["acceptedBy"] and special_motion["acceptedOn"] and special_motion["acceptanceEvidence"]
        for row in special_motion["examples"]:
            w = 1-.05*(row["field"]-1)
            assert math.isclose(row["ordinaryDistanceFeet"], w*w)
            assert math.isclose(row["ordinaryRecoverySeconds"], .20*w)
            assert math.isclose(row["combinedImpactDistanceFeet"], w*w+row["specialDistanceFeet"])
            assert math.isclose(row["actionReadyAfterSeconds"], .20*w+row["specialRecoverySeconds"])
            assert math.isclose(row["impactMotionEndsAfterSeconds"], max(.20*w, .40))
            assert math.isclose(row["combinedInitialImpactFeetPerSecond"], 10*w+2*row["specialDistanceFeet"]/.40)
    resistance = data.get("specialImpactFieldResistanceProposal")
    if resistance:
        if resistance["state"] == "accepted-calibration-anchor":
            assert resistance["acceptedBy"] and resistance["acceptedOn"] and resistance["acceptanceEvidence"]
        for row in resistance["examples"]:
            factor = 1-resistance["maxReduction"]*(row["field"]-resistance["minField"])/(resistance["maxField"]-resistance["minField"])
            w = 1-.05*(row["field"]-1)
            assert math.isclose(row["specialFactor"], factor)
            assert math.isclose(row["specialDistanceFeet"], 2*factor)
            assert math.isclose(row["specialMotionSeconds"], .4*factor)
            assert math.isclose(row["specialRecoverySeconds"], .4*factor)
            assert math.isclose(row["combinedDistanceFeet"], w*w+2*factor)
            assert math.isclose(row["combinedRecoverySeconds"], .2*w+.4*factor)
            # Illustrative triangular profile keeps its initial 10 ft/s speed.
            assert math.isclose(10*row["specialMotionSeconds"]/2, row["specialDistanceFeet"])
    special_possession = data.get("specialPushbackPossessionProposal")
    if special_possession:
        if special_possession["state"] == "accepted-calibration-anchor":
            assert special_possession["acceptedBy"] and special_possession["acceptedOn"] and special_possession["acceptanceEvidence"]
        for case in special_possession["cases"]:
            assert case["secureAfter"] == (case["secureBefore"] and not case["authoredDislodgeOccurs"])
    special_actions = data.get("specialPushbackActionsProposal")
    if special_actions:
        if special_actions["state"] == "accepted-calibration-anchor":
            assert special_actions["acceptedBy"] and special_actions["acceptedOn"] and special_actions["acceptanceEvidence"]
        for case in special_actions["cases"]:
            assert case["contactEligible"] == (case["secure"] and case["legalContact"])
    mixed_status = data.get("mixedStatusActionReadinessProposal")
    if mixed_status:
        if mixed_status["state"] == "accepted-calibration-anchor":
            assert mixed_status["acceptedBy"] and mixed_status["acceptedOn"] and mixed_status["acceptanceEvidence"]
        for case in mixed_status["examples"]:
            assert case["eligible"] == (case["normalPrerequisites"] and not case["activeRestrictionsBlockingAction"])
    repeated_recovery = data.get("repeatedImpactRecoveryProposal")
    if repeated_recovery:
        if repeated_recovery["state"] == "accepted-calibration-anchor":
            assert repeated_recovery["acceptedBy"] and repeated_recovery["acceptedOn"] and repeated_recovery["acceptanceEvidence"]
        for row in repeated_recovery["examples"]:
            new_end = row["newImpactSeconds"] + row["newDurationSeconds"]
            assert math.isclose(row["newEndSeconds"], new_end)
            assert math.isclose(row["combinedEndSeconds"], max(row["firstEndSeconds"], new_end))
            assert math.isclose(row["unselectedQueuedEndSeconds"], max(row["firstEndSeconds"], row["newImpactSeconds"])+row["newDurationSeconds"])
    repeat_eligibility = data.get("specialImpactRepeatEligibilityProposal")
    if repeat_eligibility:
        if repeat_eligibility["state"] == "accepted-calibration-anchor":
            assert repeat_eligibility["acceptedBy"] and repeat_eligibility["acceptedOn"] and repeat_eligibility["acceptanceEvidence"]
        seen_impacts = set()
        for case in repeat_eligibility["examples"]:
            key = (case["activation"], case["fielder"])
            applies = case["qualifyingContact"] and key not in seen_impacts
            assert case["applySpecialImpact"] == applies
            if applies:
                seen_impacts.add(key)
    air_catch = data.get("cleanAirCatchReadinessProposal")
    if air_catch:
        if air_catch["state"] == "accepted-calibration-anchor":
            assert air_catch["acceptedBy"] and air_catch["acceptedOn"] and air_catch["acceptanceEvidence"]
        assert air_catch["extraPostSecurePauseSeconds"] == 0
        for row in air_catch["examples"]:
            ready = row["secureSeconds"] + air_catch["extraPostSecurePauseSeconds"]
            assert math.isclose(row["readySeconds"], ready)
            start = max(ready, row["commandSeconds"])
            if start-row["commandSeconds"] > .25+1e-9:
                assert row["releaseSeconds"] is None
            else:
                assert math.isclose(row["releaseSeconds"], start+.30)
    air_recoil = data.get("groundedAirCatchRecoilProposal")
    if air_recoil:
        if air_recoil["state"] == "accepted-calibration-anchor":
            assert air_recoil["acceptedBy"] and air_recoil["acceptedOn"] and air_recoil["acceptanceEvidence"]
        assert all(air_recoil[key] is None for key in ("airOnsetFeetPerSecond", "airFullSeverityFeetPerSecond", "sharesNumericalGroundAnchors"))
        for row in air_recoil["examples"]:
            w = row["severity"]*(1-.05*(row["field"]-1))
            assert math.isclose(row["recoverySeconds"], .20*w)
            assert math.isclose(row["impactDistanceFeet"], w*w)
    jump_ready = data.get("jumpCatchThrowReadinessProposal")
    if jump_ready:
        if jump_ready["state"] == "accepted-calibration-anchor":
            assert jump_ready["acceptedBy"] and jump_ready["acceptedOn"] and jump_ready["acceptanceEvidence"]
        assert jump_ready["requiresLandingBeforeReleaseStart"]
        assert jump_ready["extraCleanLandingPauseSeconds"] == 0
        for row in jump_ready["examples"]:
            ready = max(row["secureSeconds"], row["landingSeconds"], row["otherThrowReadySeconds"])
            assert math.isclose(row["readySeconds"], ready)
            start = max(ready, row["commandSeconds"])
            if start-row["commandSeconds"] > .25+1e-9:
                assert row["releaseSeconds"] is None
            else:
                assert math.isclose(row["releaseSeconds"], start+.30)
    air_control = data.get("jumpAirControlProposal")
    if air_control:
        if air_control["state"] == "accepted-calibration-anchor":
            assert air_control["acceptedBy"] and air_control["acceptedOn"] and air_control["acceptanceEvidence"]
        assert all(air_control[key] is None for key in ("airAccelerationScale", "airBrakingScale", "airSpeedLimitFeetPerSecond", "maxCorrectionDistanceFeet"))
    jump_input = data.get("normalJumpInputProfileProposal")
    if jump_input:
        if jump_input["state"] == "accepted-calibration-anchor":
            assert jump_input["acceptedBy"] and jump_input["acceptedOn"] and jump_input["acceptanceEvidence"]
        assert all(jump_input[key] is None for key in ("peakRiseFeet", "airtimeSeconds", "verticalCurve"))
    takeoff = data.get("normalJumpTakeoffOwnershipProposal")
    if takeoff:
        if takeoff["state"] == "accepted-calibration-anchor":
            assert takeoff["acceptedBy"] and takeoff["acceptedOn"] and takeoff["acceptanceEvidence"]
        assert takeoff["pressToTakeoffSeconds"] is None
    jump_arc = data.get("normalJumpArcTrialProposal")
    if jump_arc:
        if jump_arc["state"] == "accepted-calibration-anchor":
            assert jump_arc["acceptedBy"] and jump_arc["acceptedOn"] and jump_arc["acceptanceEvidence"]
        jump_peak, jump_duration = jump_arc["peakBodyRiseFeet"], jump_arc["airtimeSeconds"]
        assert jump_peak > 0 and jump_duration > 0
        assert math.isclose(jump_arc["apexSeconds"], jump_duration/2)
        assert math.isclose(jump_arc["initialVerticalFeetPerSecond"], 4*jump_peak/jump_duration)
        assert math.isclose(jump_arc["verticalAccelerationFeetPerSecondSquared"], -8*jump_peak/jump_duration**2)
        for row in jump_arc["samples"]:
            u = row["seconds"]/jump_duration
            assert 0 <= u <= 1
            assert math.isclose(row["heightFeet"], 4*jump_peak*u*(1-u), abs_tol=1e-12)
        for row in jump_arc["horizontalExposure"]:
            speed = (21+1.9*row["run"])*18/30.5
            assert math.isclose(row["ordinaryTakeoffSpeedFeetPerSecond"], speed)
            assert math.isclose(row["neutralDriftFeet"], speed*jump_duration)
        for row in jump_arc["alternatives"]:
            assert math.isclose(row["run5NeutralDriftFeet"], 18*row["airtimeSeconds"])
    jump_response = data.get("normalJumpAirResponseTrialProposal")
    if jump_response:
        if jump_response["state"] == "accepted-calibration-anchor":
            assert jump_response["acceptedBy"] and jump_response["acceptedOn"] and jump_response["acceptanceEvidence"]
        assert math.isclose(jump_response["airtimeSeconds"], jump_arc["airtimeSeconds"])
        for row in jump_response["examples"]:
            jr_v = (21+1.9*row["run"])*18/30.5
            jr_a = jump_response["airAccelerationScale"]*jr_v/.20
            jr_b = jump_response["airBrakingScale"]*jr_v/.10
            jr_t = jump_response["airtimeSeconds"]
            assert jr_a*jr_t < jr_v and jr_b*jr_t < jr_v
            for key, value in {
                "ordinarySpeedFeetPerSecond": jr_v,
                "airAccelerationFeetPerSecondSquared": jr_a,
                "airBrakingFeetPerSecondSquared": jr_b,
                "neutralRunningDriftFeet": jr_v*jr_t,
                "stationaryFullInputTravelFeet": .5*jr_a*jr_t**2,
                "stationaryLandingSpeedFeetPerSecond": jr_a*jr_t,
                "oppositeInputTravelFeet": jr_v*jr_t-.5*jr_b*jr_t**2,
                "oppositeInputLandingForwardSpeedFeetPerSecond": jr_v-jr_b*jr_t,
                "maxCorrectionFromNeutralPathFeet": .5*jr_b*jr_t**2,
            }.items():
                assert math.isclose(row[key], value)
    jump_startup = data.get("normalJumpStartupTrialProposal")
    if jump_startup:
        if jump_startup["state"] == "accepted-calibration-anchor":
            assert jump_startup["acceptedBy"] and jump_startup["acceptedOn"] and jump_startup["acceptanceEvidence"]
        assert jump_startup["addedGameplayStartupSeconds"] == 0
        assert math.isclose(jump_startup["airtimeSeconds"], jump_arc["airtimeSeconds"])
        for js_row in [jump_startup] + jump_startup["alternatives"]:
            js_delay = js_row["addedGameplayStartupSeconds"]
            assert js_delay >= 0
            assert math.isclose(js_row["apexAfterAcceptedInputSeconds"], js_delay+jump_arc["apexSeconds"])
            assert math.isclose(js_row["landingAfterAcceptedInputSeconds"], js_delay+jump_arc["airtimeSeconds"])
    jump_buffer = data.get("normalJumpInputBufferProposal")
    if jump_buffer:
        if jump_buffer["state"] == "accepted-calibration-anchor":
            assert jump_buffer["acceptedBy"] and jump_buffer["acceptedOn"] and jump_buffer["acceptanceEvidence"]
        assert jump_buffer["maxAgeSeconds"] > 0
        assert jump_buffer["expiryBoundary"] == "inclusive"
        assert not jump_buffer["airborneInputMayQueue"]
        for jb_row in jump_buffer["examples"]:
            jb_age = jb_row["fullyEligibleSeconds"]-jb_row["pressSeconds"]
            jb_valid = 0 <= jb_age <= jump_buffer["maxAgeSeconds"]
            assert jb_row["executes"] == jb_valid
            if jb_valid:
                assert math.isclose(jb_row["takeoffSeconds"], jb_row["fullyEligibleSeconds"])
                assert math.isclose(jb_row["landingSeconds"], jb_row["takeoffSeconds"]+jump_arc["airtimeSeconds"])
            else:
                assert jb_row["takeoffSeconds"] is None and jb_row["landingSeconds"] is None
    jump_characters = data.get("normalJumpCharacterProfileProposal")
    if jump_characters:
        if jump_characters["state"] == "accepted-calibration-anchor":
            assert jump_characters["acceptedBy"] and jump_characters["acceptedOn"] and jump_characters["acceptanceEvidence"]
        for jc_key in ("peakBodyRiseFeet", "airtimeSeconds", "apexSeconds"):
            assert math.isclose(jump_characters[jc_key], jump_arc[jc_key])
    jump_catch_input = data.get("normalJumpCatchInputProposal")
    if jump_catch_input:
        if jump_catch_input["state"] == "accepted-calibration-anchor":
            assert jump_catch_input["acceptedBy"] and jump_catch_input["acceptedOn"] and jump_catch_input["acceptanceEvidence"]
    jump_glove_tracking = data.get("normalJumpGloveTrackingProposal")
    if jump_glove_tracking:
        assert jump_glove_tracking["state"] == "superseded-by-user-direction"
        assert jump_glove_tracking["supersededBy"] == "F693-02-character-catch-range"
        assert jump_glove_tracking["supersessionEvidence"]
        assert all(jump_glove_tracking[key] is None for key in
                   ("maximumGloveAdjustmentFeet", "responseSecondsByField", "gloveCatchGeometry"))
    catch_range_direction = data.get("characterCatchRangeDirection")
    if catch_range_direction:
        assert catch_range_direction["state"] == "accepted-calibration-anchor"
        assert catch_range_direction["acceptedBy"] and catch_range_direction["acceptedOn"] and catch_range_direction["acceptanceEvidence"]
    fielding_role = data.get("fieldingRatingRoleProposal")
    if fielding_role:
        if fielding_role["state"] == "accepted-calibration-anchor":
            assert fielding_role["acceptedBy"] and fielding_role["acceptedOn"] and fielding_role["acceptanceEvidence"]
        assert fielding_role["summaryFormula"] is None and fielding_role["errorModel"] is None
    handling_errors = data.get("handlingErrorOpportunitiesProposal")
    if handling_errors:
        if handling_errors["state"] == "accepted-calibration-anchor":
            assert handling_errors["acceptedBy"] and handling_errors["acceptedOn"] and handling_errors["acceptanceEvidence"]
        assert all(handling_errors[key] is None for key in
                   ("difficultyThresholds", "handlingResponseCurve", "errorResolutionModel"))
    handling_resolution = data.get("ordinaryHandlingResolutionProposal")
    if handling_resolution:
        assert handling_resolution["state"] == "superseded-by-user-direction"
        assert handling_resolution["supersededBy"] == "F693-02-ordinary-handling-error-chance"
        assert handling_resolution["supersessionEvidence"]
        assert all(handling_resolution[key] is None for key in
                   ("challengeInputs", "handlingLimitCurve", "failureOutcomeProfile"))
    handling_chance = data.get("ordinaryHandlingErrorChanceDirection")
    if handling_chance:
        assert handling_chance["state"] == "accepted-calibration-anchor"
        assert handling_chance["acceptedBy"] and handling_chance["acceptedOn"] and handling_chance["acceptanceEvidence"]
    handling_cap = data.get("ordinaryHandlingErrorCapProposal")
    if handling_cap:
        if handling_cap["state"] == "accepted-calibration-anchor":
            assert handling_cap["acceptedBy"] and handling_cap["acceptedOn"] and handling_cap["acceptanceEvidence"]
        assert 0 < handling_cap["maximumOrdinaryErrorChance"] < 1
        assert handling_cap["routineErrorChance"] == 0
        assert all(handling_cap[key] is None for key in
                   ("difficultyCurve", "defensiveQualityCurve", "specialErrorCap"))
        hc_example = handling_cap["expectationExample"]
        assert math.isclose(hc_example["expectedErrors"], hc_example["qualifyingAttemptsAtCap"]*handling_cap["maximumOrdinaryErrorChance"])
        for hc_row in handling_cap["alternatives"]:
            assert math.isclose(hc_row["expectedErrorsPer100AtCap"], 100*hc_row["maximumOrdinaryErrorChance"])
    handling_curve = data.get("ordinaryHandlingChanceCurveProposal")
    if handling_curve:
        if handling_curve["state"] == "accepted-calibration-anchor":
            assert handling_curve["acceptedBy"] and handling_curve["acceptedOn"] and handling_curve["acceptanceEvidence"]
        assert handling_curve["difficultyMapping"] is None and handling_curve["handlingTraitMapping"] is None
        hcurve_cap = handling_curve["maximumOrdinaryErrorChance"]
        hcurve_reduction = handling_curve["maximumRelativeRiskReduction"]
        assert math.isclose(hcurve_cap, handling_cap["maximumOrdinaryErrorChance"])
        assert 0 <= hcurve_reduction <= 1
        assert math.isclose(handling_curve["strongestResidualRiskFactor"], 1-hcurve_reduction)
        for hcurve_row in handling_curve["examples"]:
            hcurve_d, hcurve_h = hcurve_row["difficulty"], hcurve_row["handlingQuality"]
            assert 0 <= hcurve_d <= 1 and 0 <= hcurve_h <= 1
            hcurve_p = hcurve_cap*hcurve_d*(1-hcurve_reduction*hcurve_h)
            assert 0 <= hcurve_p <= hcurve_cap
            assert math.isclose(hcurve_row["errorChance"], hcurve_p, abs_tol=1e-12)
    awkward_hop = data.get("awkwardHopDifficultySourceProposal")
    if awkward_hop:
        assert awkward_hop["state"] == "accepted-calibration-anchor"
        assert awkward_hop["acceptedBy"] and awkward_hop["acceptedOn"] and awkward_hop["acceptanceEvidence"]
        assert all(awkward_hop[key] is None for key in
                   ("difficultyMapping", "hopPhaseBounds", "heightBounds", "speedBounds"))
    ordinary_bobble = data.get("ordinaryBobbleOutcomeProposal")
    if ordinary_bobble:
        assert ordinary_bobble["state"] == "accepted-calibration-anchor"
        assert ordinary_bobble["acceptedBy"] and ordinary_bobble["acceptedOn"] and ordinary_bobble["acceptanceEvidence"]
        assert all(ordinary_bobble[key] is None for key in
                   ("scatterDistanceFt", "scatterSpeedFtPerSec", "bounceProfile",
                    "reactionDurationSec", "reacquisitionEligibility"))
    bobble_permissions = data.get("bobbleRecoveryPermissionsProposal")
    if bobble_permissions:
        assert bobble_permissions["state"] == "superseded-by-user-direction"
        assert bobble_permissions["supersededBy"] == "F693-02-bobble-stun"
        assert all(bobble_permissions[key] is None for key in
                   ("recoveryDurationSec", "handlingDurationMapping", "freshAttemptDefinition"))
    bobble_stun = data.get("bobbleStunProposal")
    if bobble_stun:
        assert bobble_stun["state"] == "accepted-calibration-anchor"
        assert bobble_stun["acceptedBy"] and bobble_stun["acceptedOn"] and bobble_stun["acceptanceEvidence"]
        assert bobble_stun["freshAttemptDefinition"] is None
        assert bobble_stun["entryMotionProfile"] == "grounded-ordinary-braking-within-stun"
        assert math.isclose(bobble_stun["stunDurationSec"], .40)
        assert bobble_stun["handlingDurationMapping"] == "shared-duration-no-handling-scaling"
    stun_handling = data.get("bobbleStunHandlingProposal")
    if stun_handling:
        assert stun_handling["state"] == "superseded-by-user-direction"
        assert stun_handling["supersededBy"] == "F693-02-uniform-bobble-stun"
        assert all(stun_handling[key] is None for key in
                   ("stunDurationSec", "maximumRelativeReduction", "minimumReadableStunSec", "handlingTraitMapping"))
    uniform_stun = data.get("uniformBobbleStunProposal")
    if uniform_stun:
        assert uniform_stun["state"] == "accepted-calibration-anchor"
        assert uniform_stun["acceptedBy"] and uniform_stun["acceptedOn"] and uniform_stun["acceptanceEvidence"]
        assert uniform_stun["handlingAffectsDuration"] is False
        assert math.isclose(uniform_stun["stunDurationSec"], .40)
    stun_duration = data.get("bobbleStunDurationProposal")
    if stun_duration:
        assert stun_duration["state"] == "accepted-calibration-anchor"
        assert stun_duration["acceptedBy"] and stun_duration["acceptedOn"] and stun_duration["acceptanceEvidence"]
        assert stun_duration["handlingAffectsDuration"] is False
        assert math.isclose(stun_duration["stunDurationSec"], .40)
        stun_sensitivity = stun_duration["sensitivity"]
        stun_runner_speed = stun_sensitivity["basepathFt"] / stun_sensitivity["steadyBagTravelSec"]
        assert math.isclose(stun_sensitivity["runnerSpeedFtPerSec"], stun_runner_speed)
        for stun_row in stun_sensitivity["alternatives"]:
            assert math.isclose(stun_row["runnerTravelFt"], stun_runner_speed * stun_row["stunSec"])
    bobble_braking = data.get("groundedBobbleBrakingProposal")
    if bobble_braking:
        assert bobble_braking["state"] == "accepted-calibration-anchor" and bobble_braking["stopRunsInsideStun"] is True
        assert bobble_braking["acceptedBy"] and bobble_braking["acceptedOn"] and bobble_braking["acceptanceEvidence"]
        assert math.isclose(bobble_braking["stunDurationSec"], stun_duration["stunDurationSec"])
        assert math.isclose(bobble_braking["normalFullSpeedStopSec"], .10)
        for bb_row in bobble_braking["examples"]:
            bb_v = (21 + 1.9*bb_row["run"])*18/30.5
            bb_b = bb_v/bobble_braking["normalFullSpeedStopSec"]
            bb_u = bb_v*bb_row["entrySpeedFraction"]
            assert math.isclose(bb_row["ordinaryTopSpeedFtPerSec"], bb_v)
            assert math.isclose(bb_row["brakingFtPerSecSquared"], bb_b)
            assert math.isclose(bb_row["entrySpeedFtPerSec"], bb_u)
            assert math.isclose(bb_row["stopSec"], bb_u/bb_b)
            assert math.isclose(bb_row["travelFt"], bb_u*bb_u/(2*bb_b))
            assert math.isclose(bb_row["remainingStunAfterStopSec"], .40-bb_u/bb_b)
    bobble_reliability = data.get("bobbleRecoveryReliabilityProposal")
    if bobble_reliability:
        assert bobble_reliability["state"] == "accepted-calibration-anchor"
        assert bobble_reliability["acceptedBy"] and bobble_reliability["acceptedOn"] and bobble_reliability["acceptanceEvidence"]
        assert bobble_reliability["sameBobbleRecoveryErrorChance"] == 0
    bobble_direction = data.get("bobbleDeflectionDirectionProposal")
    if bobble_direction:
        assert bobble_direction["state"] == "superseded-by-user-direction"
        assert bobble_direction["supersededBy"] == "F693-02-contact-led-random-bobble"
        assert bobble_direction["randomDirectionRoll"] is False
        assert all(bobble_direction[key] is None for key in
                   ("directionMapping", "angularBounds", "scatterDistanceFt",
                    "scatterSpeedFtPerSec", "verticalProfile"))
    random_bobble = data.get("contactLedRandomBobbleProposal")
    if random_bobble:
        assert random_bobble["state"] == "accepted-calibration-anchor"
        assert random_bobble["acceptedBy"] and random_bobble["acceptedOn"] and random_bobble["acceptanceEvidence"]
        assert random_bobble["randomDirectionRoll"] is True
        assert all(random_bobble[key] is None for key in
                   ("directionMapping", "verticalRandomness"))
        assert random_bobble["distribution"] == "uniform-angle"
        assert random_bobble["angularBounds"]["horizontalMinDegrees"] == -30
        assert random_bobble["angularBounds"]["horizontalMaxDegrees"] == 30
    bobble_spread = data.get("bobbleDirectionSpreadProposal")
    if bobble_spread:
        assert bobble_spread["state"] == "accepted-calibration-anchor"
        assert bobble_spread["acceptedBy"] and bobble_spread["acceptedOn"] and bobble_spread["acceptanceEvidence"]
        assert bobble_spread["horizontalMaxOffsetDegrees"] == 30
        assert all(bobble_spread[key] is None for key in
                   ("baselineDirectionMapping", "verticalRandomness"))
        assert bobble_spread["distribution"] == "uniform-angle"
        spread_example = bobble_spread["sensitivity"]
        assert spread_example["travelIsTarget"] is False
        for spread_row in spread_example["alternatives"]:
            spread_angle = math.radians(spread_row["maxOffsetDegrees"])
            assert spread_row["totalFanDegrees"] == 2*spread_row["maxOffsetDegrees"]
            assert math.isclose(spread_row["lateralComponentFt"], spread_example["illustrativeTravelFt"]*math.sin(spread_angle))
            assert math.isclose(spread_row["baselineProjectionFt"], spread_example["illustrativeTravelFt"]*math.cos(spread_angle))
    expanded_errors = data.get("expandedOrdinaryErrorOutcomesProposal")
    if expanded_errors:
        assert expanded_errors["state"] == "accepted-calibration-anchor"
        assert expanded_errors["acceptedBy"] and expanded_errors["acceptedOn"] and expanded_errors["acceptanceEvidence"]
        assert expanded_errors["outcomeKinds"] == ["local-bobble", "ball-gets-past", "continuing-deflection"]
    error_selection = data.get("errorOutcomeSelectionProposal")
    if error_selection:
        assert error_selection["state"] == "accepted-calibration-anchor" and error_selection["separateSeverityRoll"] is False
        assert error_selection["acceptedBy"] and error_selection["acceptedOn"] and error_selection["acceptanceEvidence"]
        assert all(error_selection[key] is None for key in
                   ("outcomeMapping", "speedThresholds"))
        assert error_selection["retainedSpeedModel"]["boundsDecisionId"] == "F693-02-continuing-error-speed-retention"
        assert error_selection["retainedSpeedModel"]["contactMapping"]["policy"] == "simple-gameplay-contact-context"
        assert error_selection["retainedSpeedModel"]["contactMapping"]["noGloveNormals"] is True
        assert error_selection["randomAngleInheritance"] == "F693-02-continuing-error-direction"
        assert error_selection["recoveryInheritance"] == "F693-02-continuing-error-recovery"
        assert error_selection["reactionInheritance"] == "F693-02-continuing-error-reaction"
    continuing_reaction = data.get("continuingErrorReactionProposal")
    if continuing_reaction:
        assert continuing_reaction["state"] == "accepted-calibration-anchor"
        assert continuing_reaction["acceptedBy"] and continuing_reaction["acceptedOn"] and continuing_reaction["acceptanceEvidence"]
        assert math.isclose(continuing_reaction["contactFailureStunSec"], stun_duration["stunDurationSec"])
        assert continuing_reaction["untouchedMissAddedStunSec"] == 0
        assert continuing_reaction["durationScalesWithHandling"] is False
        assert continuing_reaction["durationScalesWithEscapeDistance"] is False
    continuing_recovery = data.get("continuingErrorRecoveryProposal")
    if continuing_recovery:
        assert continuing_recovery["state"] == "accepted-calibration-anchor"
        assert continuing_recovery["acceptedBy"] and continuing_recovery["acceptedOn"] and continuing_recovery["acceptanceEvidence"]
        assert continuing_recovery["sameErrorRecoveryChance"] == 0
        assert continuing_recovery["untouchedMissGrantsRecoveryProtection"] is False
    continuing_direction = data.get("continuingErrorDirectionProposal")
    if continuing_direction:
        assert continuing_direction["state"] == "accepted-calibration-anchor"
        assert continuing_direction["acceptedBy"] and continuing_direction["acceptedOn"] and continuing_direction["acceptanceEvidence"]
        assert continuing_direction["horizontalMaxOffsetDegrees"] == 15
        assert continuing_direction["horizontalMaxOffsetDegrees"] < bobble_spread["horizontalMaxOffsetDegrees"]
        assert continuing_direction["untouchedMissAddedOffsetDegrees"] == 0
        assert all(continuing_direction[key] is None for key in
                   ("contactBaselineMapping",))
        assert continuing_direction["verticalTreatment"]["sameFactorAsHorizontal"] is True
        assert continuing_direction["verticalTreatment"]["contactFactorMapping"]["policy"] == "simple-gameplay-contact-context"
        assert continuing_direction["verticalTreatment"]["contactFactorMapping"]["noGloveNormals"] is True
        assert continuing_direction["distribution"] == "uniform-angle"
        assert continuing_direction["retainedSpeedModel"]["boundsDecisionId"] == "F693-02-continuing-error-speed-retention"
        assert continuing_direction["retainedSpeedModel"]["contactMapping"]["policy"] == "simple-gameplay-contact-context"
        assert continuing_direction["retainedSpeedModel"]["contactMapping"]["noGloveNormals"] is True
    continuing_retention = data.get("continuingErrorSpeedRetentionProposal")
    if continuing_retention:
        assert continuing_retention["state"] == "accepted-calibration-anchor"
        assert continuing_retention["acceptedBy"] and continuing_retention["acceptedOn"] and continuing_retention["acceptanceEvidence"]
        retain_min = continuing_retention["minimumRetainedHorizontalSpeedFraction"]
        retain_max = continuing_retention["maximumRetainedHorizontalSpeedFraction"]
        assert 0 < retain_min < retain_max < 1
        assert math.isclose(retain_min, .50) and math.isclose(retain_max, .80)
        assert all(continuing_retention[key] is None for key in
                   ("outcomeThresholds",))
        assert continuing_retention["contactRetentionMapping"]["policy"] == "simple-gameplay-contact-context"
        assert continuing_retention["contactRetentionMapping"]["noGloveNormals"] is True
        assert continuing_retention["verticalResponse"]["sameFactorAsHorizontal"] is True
        for retain_row in continuing_retention["examples"]:
            assert math.isclose(retain_row["lowerOutgoingFtPerSec"], retain_row["incomingHorizontalFtPerSec"]*retain_min)
            assert math.isclose(retain_row["upperOutgoingFtPerSec"], retain_row["incomingHorizontalFtPerSec"]*retain_max)
    direction_distribution = data.get("errorDirectionDistributionProposal")
    if direction_distribution:
        assert direction_distribution["state"] == "superseded-by-user-direction"
        assert direction_distribution["supersededBy"] == "F693-02-uniform-error-direction"
        assert direction_distribution["distribution"] == "symmetric-triangular"
        assert direction_distribution["normalizedSupport"] == [-1, 1] and direction_distribution["normalizedMode"] == 0
        assert math.isclose(direction_distribution["leftProbability"], .5)
        assert math.isclose(direction_distribution["rightProbability"], .5)
        dist_caps = {"local-bobble": bobble_spread["horizontalMaxOffsetDegrees"],
                     "continuing-deflection": continuing_direction["horizontalMaxOffsetDegrees"]}
        for dist_row in direction_distribution["examples"]:
            dist_cap = dist_caps[dist_row["branch"]]
            assert dist_row["maxDegrees"] == dist_cap
            assert math.isclose(dist_row["centralHalfDegrees"], dist_cap/2)
            assert math.isclose(dist_row["centralHalfProbability"], 2*.5-.5**2)
            assert math.isclose(dist_row["outerHalfProbability"], 1-dist_row["centralHalfProbability"])
            assert math.isclose(dist_row["expectedAbsoluteOffsetDegrees"], dist_cap/3)
    uniform_direction = data.get("uniformErrorDirectionProposal")
    if uniform_direction:
        assert uniform_direction["state"] == "accepted-calibration-anchor"
        assert uniform_direction["acceptedBy"] and uniform_direction["acceptedOn"] and uniform_direction["acceptanceEvidence"]
        assert uniform_direction["distribution"] == "uniform-angle" and uniform_direction["normalizedSupport"] == [-1, 1]
        assert uniform_direction["leftProbability"] == uniform_direction["rightProbability"] == .5
        uniform_caps = {"local-bobble": bobble_spread["horizontalMaxOffsetDegrees"],
                        "continuing-deflection": continuing_direction["horizontalMaxOffsetDegrees"]}
        for uniform_row in uniform_direction["examples"]:
            uniform_cap = uniform_caps[uniform_row["branch"]]
            assert uniform_row["maxDegrees"] == uniform_cap
            assert math.isclose(uniform_row["centralHalfDegrees"], uniform_cap/2)
            assert uniform_row["centralHalfProbability"] == uniform_row["outerHalfProbability"] == .5
            assert math.isclose(uniform_row["expectedAbsoluteOffsetDegrees"], uniform_cap/2)
    local_vertical = data.get("localBobbleVerticalShapeProposal")
    if local_vertical:
        assert local_vertical["state"] == "accepted-calibration-anchor" and local_vertical["defaultAddedUpwardPop"] is False
        assert local_vertical["acceptedBy"] and local_vertical["acceptedOn"] and local_vertical["acceptanceEvidence"]
        assert all(local_vertical[key] is None for key in
                   ("reboundHeightFt", "settleDistanceFt"))
        assert local_vertical["localHorizontalSpeed"]["maximumFtPerSec"] == 6
        assert local_vertical["localHorizontalSpeed"]["contactMapping"]["retainedFraction"] == .2
        assert local_vertical["verticalContactResponse"]["postContactVerticalSpeedFtPerSec"] == 0
    local_rebound = data.get("localBobbleReboundCeilingProposal")
    if local_rebound:
        assert local_rebound["state"] == "accepted-calibration-anchor"
        assert local_rebound["acceptedBy"] and local_rebound["acceptedOn"] and local_rebound["acceptanceEvidence"]
        assert local_rebound["maximumReboundRiseFt"] == .5
        assert local_rebound["maximumReboundRiseInches"] == local_rebound["maximumReboundRiseFt"] * 12
        assert local_rebound["fixedReboundHeight"] is False
        assert all(local_rebound[key] is None for key in
                   ("minimumReboundRiseFt",))
        assert local_rebound["verticalContactResponse"]["postContactVerticalSpeedFtPerSec"] == 0
        assert local_rebound["restitution"] == .35
    local_restitution = data.get("localBobbleRestitutionProposal")
    if local_restitution:
        assert local_restitution["state"] == "accepted-calibration-anchor"
        assert local_restitution["acceptedBy"] and local_restitution["acceptedOn"] and local_restitution["acceptanceEvidence"]
        assert local_restitution["verticalSpeedRetention"] == .35
        assert local_restitution["maximumReboundRiseFt"] == local_rebound["maximumReboundRiseFt"]
        assert local_restitution["settleThreshold"]["maximumSuppressedReboundRiseFt"] == .25
        assert local_restitution["postGloveVerticalResponse"]["postContactVerticalSpeedFtPerSec"] == 0
        for rebound_example in local_restitution["examples"]:
            rebound_rise = rebound_example["illustrativeDropFromRestFt"] * local_restitution["verticalSpeedRetention"]**2
            assert math.isclose(rebound_example["uncappedReboundRiseFt"], rebound_rise)
            assert math.isclose(rebound_example["cappedReboundRiseFt"], min(local_rebound["maximumReboundRiseFt"], rebound_rise))
    local_settling = data.get("localBobbleSettlingProposal")
    if local_settling:
        assert local_settling["state"] == "accepted-calibration-anchor"
        assert local_settling["acceptedBy"] and local_settling["acceptedOn"] and local_settling["acceptanceEvidence"]
        assert local_settling["maximumSuppressedReboundRiseInches"] == 3
        assert math.isclose(local_settling["maximumSuppressedReboundRiseFt"] * 12, 3)
        assert local_settling["comparison"] == "less-than-or-equal"
        assert local_settling["atActualGroundImpactOnly"] is True and local_settling["forceHorizontalStop"] is False
        settling_example = local_settling["examples"]
        settling_rise = settling_example["previousReboundRiseInches"] * local_restitution["verticalSpeedRetention"]**2
        assert math.isclose(settling_example["uncutNextReboundRiseInches"], settling_rise)
        assert settling_example["nextReboundSuppressed"] == (settling_rise <= local_settling["maximumSuppressedReboundRiseInches"])
        for first_impact in local_settling["firstImpactExamples"]:
            first_rise = first_impact["illustrativeDropFromRestFt"] * 12 * local_restitution["verticalSpeedRetention"]**2
            assert math.isclose(first_impact["uncutReboundRiseInches"], first_rise)
            assert first_impact["settlesAtFirstGroundImpact"] == (first_rise <= local_settling["maximumSuppressedReboundRiseInches"])
    local_glove_release = data.get("localBobbleGloveReleaseProposal")
    if local_glove_release:
        assert local_glove_release["state"] == "accepted-calibration-anchor"
        assert local_glove_release["acceptedBy"] and local_glove_release["acceptedOn"] and local_glove_release["acceptanceEvidence"]
        assert local_glove_release["postContactVerticalSpeedFtPerSec"] == 0
        assert local_glove_release["addedHoldSec"] == 0
        assert local_glove_release["horizontalContactResponse"]["maximumFtPerSec"] == 6
        assert local_glove_release["horizontalContactResponse"]["contactMapping"]["retainedFraction"] == .2
    local_horizontal_cap = data.get("localBobbleHorizontalCapProposal")
    if local_horizontal_cap:
        assert local_horizontal_cap["state"] == "accepted-calibration-anchor"
        assert local_horizontal_cap["acceptedBy"] and local_horizontal_cap["acceptedOn"] and local_horizontal_cap["acceptanceEvidence"]
        assert local_horizontal_cap["maximumPostContactHorizontalSpeedFtPerSec"] == 6
        assert local_horizontal_cap["fixedSpeed"] is False
        assert all(local_horizontal_cap[key] is None for key in
                   ("minimumSpeedFtPerSec",))
        assert local_horizontal_cap["groundHorizontalResponse"]["retainedFraction"] == .9
        assert local_horizontal_cap["contactSpeedMapping"]["retainedFraction"] == .2
        spill_example = local_horizontal_cap["illustration"]
        assert math.isclose(spill_example["distanceFt"], spill_example["constantSpeedFtPerSec"] * spill_example["elapsedSec"])
    local_horizontal_retention = data.get("localBobbleHorizontalRetentionProposal")
    if local_horizontal_retention:
        assert local_horizontal_retention["state"] == "accepted-calibration-anchor"
        assert local_horizontal_retention["acceptedBy"] and local_horizontal_retention["acceptedOn"] and local_horizontal_retention["acceptanceEvidence"]
        assert local_horizontal_retention["horizontalSpeedRetention"] == .20
        assert local_horizontal_retention["maximumOutgoingHorizontalSpeedFtPerSec"] == local_horizontal_cap["maximumPostContactHorizontalSpeedFtPerSec"]
        for retention_example in local_horizontal_retention["examples"]:
            retained_speed = min(local_horizontal_retention["horizontalSpeedRetention"] * retention_example["incomingHorizontalSpeedFtPerSec"], local_horizontal_retention["maximumOutgoingHorizontalSpeedFtPerSec"])
            assert math.isclose(retention_example["outgoingHorizontalSpeedFtPerSec"], retained_speed)
    local_ground_horizontal = data.get("localBobbleGroundHorizontalProposal")
    if local_ground_horizontal:
        assert local_ground_horizontal["state"] == "accepted-calibration-anchor"
        assert local_ground_horizontal["acceptedBy"] and local_ground_horizontal["acceptedOn"] and local_ground_horizontal["acceptanceEvidence"]
        assert local_ground_horizontal["retainedHorizontalFraction"] == .9
        assert local_ground_horizontal["rollingFriction"]["decelerationFtPerSecSquared"] == 6
        for impact_example in local_ground_horizontal["examples"]:
            assert math.isclose(impact_example["outgoingHorizontalSpeedFtPerSec"], impact_example["incomingHorizontalSpeedFtPerSec"] * local_ground_horizontal["retainedHorizontalFraction"])
    local_rolling = data.get("localBobbleRollingDecelerationProposal")
    if local_rolling:
        assert local_rolling["state"] == "accepted-calibration-anchor"
        assert local_rolling["acceptedBy"] and local_rolling["acceptedOn"] and local_rolling["acceptanceEvidence"]
        assert local_rolling["decelerationFtPerSecSquared"] == 6
        for roll_example in local_rolling["examples"]:
            roll_s = roll_example["initialRollingSpeedFtPerSec"]
            roll_a = local_rolling["decelerationFtPerSecSquared"]
            assert math.isclose(roll_example["stopTimeSec"], roll_s / roll_a)
            assert math.isclose(roll_example["travelToRestFt"], roll_s**2 / (2*roll_a))
    continuing_vertical = data.get("continuingErrorVerticalRetentionProposal")
    if continuing_vertical:
        assert continuing_vertical["state"] == "accepted-calibration-anchor"
        assert continuing_vertical["acceptedBy"] and continuing_vertical["acceptedOn"] and continuing_vertical["acceptanceEvidence"]
        assert continuing_vertical["sameFactorAsHorizontal"] is True
        assert continuing_vertical["minimumFactor"] == .5 and continuing_vertical["maximumFactor"] == .8
        assert continuing_vertical["independentVerticalRandomness"] is False
        assert continuing_vertical["contactFactorMapping"]["policy"] == "simple-gameplay-contact-context"
        assert continuing_vertical["contactFactorMapping"]["noGloveNormals"] is True
        for vertical_example in continuing_vertical["examples"]:
            assert .5 <= vertical_example["factor"] <= .8
            assert math.isclose(vertical_example["outgoingHorizontalFtPerSec"], vertical_example["factor"] * vertical_example["incomingHorizontalFtPerSec"])
            assert math.isclose(vertical_example["outgoingVerticalFtPerSec"], vertical_example["factor"] * vertical_example["incomingVerticalFtPerSec"])
    continuing_ground = data.get("continuingErrorGroundResponseProposal")
    if continuing_ground:
        assert continuing_ground["state"] == "accepted-calibration-anchor"
        assert continuing_ground["acceptedBy"] and continuing_ground["acceptedOn"] and continuing_ground["acceptanceEvidence"]
        assert continuing_ground["responseFamily"] == "shared-ordinary-batted-ball-ground"
        assert continuing_ground["extraErrorSpecificBraking"] is False
        assert continuing_ground["inheritsLocalBobbleGroundLimits"] is False
        assert continuing_ground["numericalGroundProfile"] is None
    continuing_curve = data.get("continuingErrorRetentionCurveProposal")
    if continuing_curve:
        assert continuing_curve["state"] == "superseded-by-user-direction" and continuing_curve["curve"] == "linear"
        assert continuing_curve["acceptedBy"] and continuing_curve["acceptedOn"] and continuing_curve["acceptanceEvidence"]
        assert continuing_curve["obstructionMetric"]["basis"] == "three-dimensional-contact-incidence"
        assert continuing_curve["obstructionMetric"]["normalizationBounds"] is None
        assert continuing_curve["branchThresholds"] is None
        assert continuing_curve["lightContactRetention"] == .8 and continuing_curve["strongContinuingContactRetention"] == .5
        for curve_example in continuing_curve["examples"]:
            c_value = curve_example["normalizedContinuingObstruction"]
            assert 0 <= c_value <= 1
            assert math.isclose(curve_example["retainedFraction"], .8-.3*c_value)
    contact_incidence = data.get("errorContactObstructionBasisProposal")
    if contact_incidence:
        assert contact_incidence["state"] == "superseded-by-user-direction"
        assert contact_incidence["acceptedBy"] and contact_incidence["acceptedOn"] and contact_incidence["acceptanceEvidence"]
        assert contact_incidence["basis"] == "three-dimensional-contact-incidence"
        assert contact_incidence["usesRelativeContactVelocity"] is True and contact_incidence["usesPenetrationDepth"] is False
        assert all(contact_incidence[key] is None for key in ("continuingNormalizationBounds", "branchThresholds", "zeroRelativeSpeedFallback"))
        assert contact_incidence["geometryContract"]["shapeFamily"] == "smooth-concave-pocket-rounded-rim"
        assert contact_incidence["geometryContract"]["dimensions"] is None
        for incidence_row, expected_incidence in zip(contact_incidence["examples"], (1, math.sqrt(.5), 0)):
            assert math.isclose(incidence_row["rawIncidence"], expected_incidence)
    glove_surface = data.get("gloveContactSurfaceProposal")
    if glove_surface:
        assert glove_surface["state"] == "superseded-by-user-direction"
        assert glove_surface["acceptedBy"] and glove_surface["acceptedOn"] and glove_surface["acceptanceEvidence"]
        assert glove_surface["shapeFamily"] == "smooth-concave-pocket-rounded-rim"
        assert glove_surface["usesDecorativeMeshTriangles"] is False and glove_surface["changesCatchRange"] is False
        assert all(glove_surface[key] is None for key in ("dimensionsFt", "pocketDepthFt", "rimProfile", "backAndCuffResponse", "authoritativeMotionContract"))
    glove_sides = data.get("gloveCatchSidesProposal")
    if glove_sides:
        assert glove_sides["state"] == "superseded-by-user-direction"
        assert glove_sides["eligibleSolidRegions"] == ["pocket", "rim", "back"]
        assert glove_sides["requiresPocketFacing"] is False and glove_sides["addsGloveFacingInput"] is False
        assert glove_sides["solidSurfaceGeometry"] is None and glove_sides["cuffGeometry"] is None
    arcade_fielding = data["arcadeFieldingSimplification"]
    assert arcade_fielding["state"] == "accepted-calibration-anchor"
    assert arcade_fielding["acceptedBy"] and arcade_fielding["acceptedOn"] and arcade_fielding["acceptanceEvidence"]
    assert arcade_fielding["gloveFacingRole"] == "presentation"
    assert arcade_fielding["acquisitionBasis"] == "explicit-character-fielding-range-and-action-readiness"
    assert all(arcade_fielding[key] is False for key in ("calculatesGloveSurfaceNormals", "calculatesPocketRimBackEligibility", "requiresGloveMeshCollision"))
    for retired_glove in (continuing_curve, contact_incidence, glove_surface, glove_sides):
        assert retired_glove["supersededBy"] == arcade_fielding["decisionId"]
    reach_research = data.get("catchReachCoverageResearch")
    if reach_research:
        assert reach_research["state"] == "accepted-calibration-anchor"
        assert reach_research["acceptedBy"] and reach_research["acceptedOn"] and reach_research["acceptanceEvidence"]
        assert reach_research["acceptedOption"] == "re-author-visible-envelope"
        assert reach_research["model"]["simulated"] is False
        chosen = next(o for o in reach_research["options"] if o["id"] == reach_research["acceptedOption"])
        assert math.isclose(reach_research["acceptedStandUpReachFt"], chosen["candidateTrialFt"])
        assert reach_research["acceptedStandUpReachFt"] in reach_research["reachOptionsFt"], \
            "The accepted reach must be one of the options the coverage table covers"
        assert arcade_fielding["decisionId"] in reach_research["parentDecisionIds"]
        assert math.isclose(reach_research["currentRuntimeStack"]["field5StandUpFt"],
                            10 + .6 * 5), "Field-5 stand-up reach must follow the live rules file"
        control_pairs = {r["pair"]: r for r in records[0]["catchReachCoverage"]["pairs"]}
        for record in records:
            coverage = record["catchReachCoverage"]
            assert math.isclose(coverage["scaledOptionFt"], 13 * record["basepathFt"] / 90)
            for row in coverage["pairs"]:
                seconds = [v for _, v in sorted(row["closureHangSecondsByReachFt"].items(),
                                                key=lambda kv: float(kv[0]))]
                assert seconds == sorted(seconds, reverse=True), "More reach cannot close a gap later"
                control = control_pairs[row["pair"]]
                assert row["spacingFt"] <= control["spacingFt"] + 1e-9, "No compact gap may exceed the control"
        alley = {r["id"]: next(row for row in r["catchReachCoverage"]["pairs"] if row["pair"] == "LF-CF")
                 for r in records}
        headline = reach_research["headline"]
        for key, profile, reach in (("alleyClosureC0LegacyReachSeconds", "C0", "13"),
                                    ("alleyClosureC80LegacyReachSeconds", "C80", "13"),
                                    ("alleyClosureC80ZeroReachSeconds", "C80", "0")):
            assert math.isclose(headline[key], alley[profile]["closureHangSecondsByReachFt"][reach], abs_tol=.005)
        accepted = f'{reach_research["acceptedStandUpReachFt"]:g}'
        consequences = reach_research["acceptedConsequences"]
        lead = next(r for r in records if r["id"] == selected)
        lead_pairs = {row["pair"]: row for row in lead["catchReachCoverage"]["pairs"]}
        for key, pair in (("alleyClosureSeconds", "LF-CF"), ("thirdToShortClosureSeconds", "3B-SS"),
                          ("shortToSecondClosureSeconds", "SS-2B")):
            assert math.isclose(consequences[key], lead_pairs[pair]["closureHangSecondsByReachFt"][accepted], abs_tol=.005)
        assert math.isclose(consequences["reachPerBasepath"],
                            reach_research["acceptedStandUpReachFt"] / lead["basepathFt"])
    return {"schemaVersion": 1, "status": "derived-design-arithmetic-not-simulation",
            "acceptedLeadSpatialTrial": selected,
            "catcherReadState": catcher_read["state"] if catcher_read else None,
            "uniformErrorDirectionState": uniform_direction["state"] if uniform_direction else None,
            "arcadeFieldingState": arcade_fielding["state"],
            "catchReachEnvelopeState": reach_research["state"] if reach_research else None,
            "gloveCatchSidesState": glove_sides["state"] if glove_sides else None,
            "gloveContactSurfaceState": glove_surface["state"] if glove_surface else None,
            "errorContactObstructionBasisState": contact_incidence["state"] if contact_incidence else None,
            "continuingErrorRetentionCurveState": continuing_curve["state"] if continuing_curve else None,
            "continuingErrorGroundResponseState": continuing_ground["state"] if continuing_ground else None,
            "continuingErrorVerticalRetentionState": continuing_vertical["state"] if continuing_vertical else None,
            "localBobbleRollingDecelerationState": local_rolling["state"] if local_rolling else None,
            "localBobbleGroundHorizontalState": local_ground_horizontal["state"] if local_ground_horizontal else None,
            "localBobbleHorizontalRetentionState": local_horizontal_retention["state"] if local_horizontal_retention else None,
            "localBobbleHorizontalCapState": local_horizontal_cap["state"] if local_horizontal_cap else None,
            "localBobbleGloveReleaseState": local_glove_release["state"] if local_glove_release else None,
            "localBobbleSettlingState": local_settling["state"] if local_settling else None,
            "localBobbleRestitutionState": local_restitution["state"] if local_restitution else None,
            "localBobbleReboundCeilingState": local_rebound["state"] if local_rebound else None,
            "localBobbleVerticalShapeState": local_vertical["state"] if local_vertical else None,
            "errorDirectionDistributionState": direction_distribution["state"] if direction_distribution else None,
            "continuingErrorSpeedRetentionState": continuing_retention["state"] if continuing_retention else None,
            "continuingErrorDirectionState": continuing_direction["state"] if continuing_direction else None,
            "continuingErrorRecoveryState": continuing_recovery["state"] if continuing_recovery else None,
            "continuingErrorReactionState": continuing_reaction["state"] if continuing_reaction else None,
            "expandedOrdinaryErrorOutcomesState": expanded_errors["state"] if expanded_errors else None,
            "errorOutcomeSelectionState": error_selection["state"] if error_selection else None,
            "contactLedRandomBobbleState": random_bobble["state"] if random_bobble else None,
            "bobbleDirectionSpreadState": bobble_spread["state"] if bobble_spread else None,
            "bobbleDeflectionDirectionState": bobble_direction["state"] if bobble_direction else None,
            "bobbleRecoveryReliabilityState": bobble_reliability["state"] if bobble_reliability else None,
            "groundedBobbleBrakingState": bobble_braking["state"] if bobble_braking else None,
            "uniformBobbleStunState": uniform_stun["state"] if uniform_stun else None,
            "bobbleStunDurationState": stun_duration["state"] if stun_duration else None,
            "bobbleStunState": bobble_stun["state"] if bobble_stun else None,
            "bobbleStunHandlingState": stun_handling["state"] if stun_handling else None,
            "bobbleRecoveryPermissionsState": bobble_permissions["state"] if bobble_permissions else None,
            "ordinaryBobbleOutcomeState": ordinary_bobble["state"] if ordinary_bobble else None,
            "awkwardHopDifficultySourceState": awkward_hop["state"] if awkward_hop else None,
            "ordinaryHandlingChanceCurveState": handling_curve["state"] if handling_curve else None,
            "ordinaryHandlingErrorChanceState": handling_chance["state"] if handling_chance else None,
            "ordinaryHandlingErrorCapState": handling_cap["state"] if handling_cap else None,
            "ordinaryHandlingResolutionState": handling_resolution["state"] if handling_resolution else None,
            "handlingErrorOpportunitiesState": handling_errors["state"] if handling_errors else None,
            "characterCatchRangeDirectionState": catch_range_direction["state"] if catch_range_direction else None,
            "fieldingRatingRoleState": fielding_role["state"] if fielding_role else None,
            "normalJumpGloveTrackingState": jump_glove_tracking["state"] if jump_glove_tracking else None,
            "normalJumpCatchInputState": jump_catch_input["state"] if jump_catch_input else None,
            "normalJumpCharacterProfileState": jump_characters["state"] if jump_characters else None,
            "normalJumpInputBufferState": jump_buffer["state"] if jump_buffer else None,
            "normalJumpStartupTrialState": jump_startup["state"] if jump_startup else None,
            "normalJumpAirResponseTrialState": jump_response["state"] if jump_response else None,
            "normalJumpArcTrialState": jump_arc["state"] if jump_arc else None,
            "normalJumpTakeoffOwnershipState": takeoff["state"] if takeoff else None,
            "normalJumpInputProfileState": jump_input["state"] if jump_input else None,
            "jumpAirControlState": air_control["state"] if air_control else None,
            "jumpCatchThrowReadinessState": jump_ready["state"] if jump_ready else None,
            "groundedAirCatchRecoilState": air_recoil["state"] if air_recoil else None,
            "cleanAirCatchReadinessState": air_catch["state"] if air_catch else None,
            "specialImpactRepeatEligibilityState": repeat_eligibility["state"] if repeat_eligibility else None,
            "repeatedImpactRecoveryState": repeated_recovery["state"] if repeated_recovery else None,
            "mixedStatusActionReadinessState": mixed_status["state"] if mixed_status else None,
            "specialPushbackActionsState": special_actions["state"] if special_actions else None,
            "specialPushbackPossessionState": special_possession["state"] if special_possession else None,
            "specialImpactFieldResistanceState": resistance["state"] if resistance else None,
            "specialImpactMotionCompositionState": special_motion["state"] if special_motion else None,
            "ordinaryRecoilMotionProfileState": motion["state"] if motion else None,
            "ordinaryRecoilDistanceCapState": displacement_cap["state"] if displacement_cap else None,
            "ordinaryRecoilDisplacementState": displacement["state"] if displacement else None,
            "ordinaryRecoilActionsState": recoil_actions["state"] if recoil_actions else None,
            "recoilSeverityCurveState": severity_curve["state"] if severity_curve else None,
            "recoilFieldFactorsState": field_factors["state"] if field_factors else None,
            "recoilFieldShapingState": field_shape["state"] if field_shape else None,
            "specialRecoveryCompositionState": composition["state"] if composition else None,
            "groundPickupRecoilCapState": recoil_cap["state"] if recoil_cap else None,
            "groundPickupRecoilBasisState": data.get("groundPickupRecoilBasisProposal", {}).get("state"),
            "cleanGroundPickupReadinessState": pickup["state"] if pickup else None,
            "carryMovementResponseState": carry_response["state"] if carry_response else None,
            "ordinaryCarrySpeedState": carry["state"] if carry else None,
            "ballDashCarrierState": data.get("ballDashCarrierProposal", {}).get("state"),
            "fieldDashDurationState": duration["state"] if duration else None,
            "fieldDashPeakState": dash["state"] if dash else None,
            "pursuitCalibrationSamplesState": samples["state"] if samples else None,
            "pursuitArmingState": arming["state"] if arming else None,
            "pursuitCalibrationState": calibration["state"] if calibration else None,
            "pursuitNeutralState": neutral["state"] if neutral else None,
            "pursuitAnalogState": analog["state"] if analog else None,
            "pursuitAngledTurnState": turning["state"] if turning else None,
            "pursuitReversalState": reversal["state"] if reversal else None,
            "pursuitBrakingState": braking["state"] if braking else None,
            "pursuitAccelerationState": acceleration["state"] if acceleration else None,
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
