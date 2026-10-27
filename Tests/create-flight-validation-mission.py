"""Create a local noncombat mission from an owned game's current mission JSON.

Run after loading Free Flight - Heartland, then returning to the menu.
Only the new mission is written; the original mission and game assets are untouched.
"""
import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("source", type=Path, help="TempMissions/CurrentMission/CurrentMission.json")
parser.add_argument("missions", type=Path, help="The game's user Missions directory")
args = parser.parse_args()
data = json.loads(args.source.read_text(encoding="utf-8-sig"))
assert data["JsonVersion"] == 6, "Inspect the new mission format before converting it"
assert any(f["factionName"] == "Boscali" for f in data["factions"])
for category in ("aircraft", "vehicles", "ships", "buildings", "missiles", "pilots", "containers", "airbases"):
    for item in data.get(category, []):
        if "faction" in item:
            item["faction"] = "Boscali"
for category in ("aircraft", "missiles", "pilots"):
    data[category] = []
for faction in data["factions"]:
    faction["AIAircraftLimit"] = 0
    faction["addAIPerEnemyPlayer"] = 0
data["missionSettings"]["description"] = (
    "Local F-5 validation. All bases and units belong to Boscali. "
    "Normal aircraft physics, collision and damage remain enabled."
)
name = "F5 Flight Validation - No Hostiles"
destination = args.missions / name
destination.mkdir(parents=True, exist_ok=False)
(destination / (name + ".json")).write_text(json.dumps(data, indent=2), encoding="utf-8")
(destination / "meta.json").write_text(json.dumps({"FileName": name}), encoding="utf-8")
print(destination)
