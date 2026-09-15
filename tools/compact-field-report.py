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
        assert air_recoil["state"] == "pending"
        assert all(air_recoil[key] is None for key in ("airOnsetFeetPerSecond", "airFullSeverityFeetPerSecond", "sharesNumericalGroundAnchors"))
        for row in air_recoil["examples"]:
            w = row["severity"]*(1-.05*(row["field"]-1))
            assert math.isclose(row["recoverySeconds"], .20*w)
            assert math.isclose(row["impactDistanceFeet"], w*w)
    return {"schemaVersion": 1, "status": "derived-design-arithmetic-not-simulation",
            "acceptedLeadSpatialTrial": selected,
            "catcherReadState": catcher_read["state"] if catcher_read else None,
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
