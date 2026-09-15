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
               ROOT / "src/GrandSluggers.Sim/ParkDiamond.cs", ROOT / "data/rules/running.json",
               ROOT / "data/rules/fielding.json", ROOT / "data/parks/harbor-diamond.json",
               ROOT / "data/art/clips.json", ROOT / "data/art/baseball-takes.json",
               ROOT / "src/GrandSluggers.Sim/InPlay.cs"]
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
    return {"schemaVersion": 1, "status": "derived-design-arithmetic-not-simulation",
            "acceptedLeadSpatialTrial": selected,
            "sourceSha256": {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in tracked},
            "profiles": records}


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
