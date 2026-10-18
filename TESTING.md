# F-5E development verification

2026-10-18, Nuclear Option 0.34.2 / Blueprinter 2.0.1.

- Unity build and native-system reference validation: passed.
- Aircraft catalog: F-5E Tiger II appears with its dedicated silhouette.
- Catalog rank: 2; purchase button: $14m; purchase added one owned aircraft.
- Bundle loaded by Blueprinter, and F-5 runtime helper loaded alongside Su-25 helper.
- Original model conversion retained all 123 AC3D objects (17,844 triangles).
- Bundle container inspection: no native game asset entries and no Su-25 assets.
- Initial empty livery selector fixed with selectable liveries for both factions.
- Fresh Fly spawn reached the cockpit but failed with immediate structural
  damage/engine fire. This prototype is NOT flight-ready.
- Runtime log confirms own F5 definition, 28 physics parts and 17 gear-animation
  tracks bound to three owned native gears.
- Contact logging identified stationary self-collisions: reversed control-surface
  sides, oversized body boxes and obsolete donor bay/fairing colliders.
- Builder correction in progress: positive Unity model scale, narrower body/wing
  collision partitions, removal of unused donor bay/fairing contacts. Repeat
  fresh-spawn verification is required after compilation.
- Source/native gear visual alignment and cockpit visibility need in-game checking.
- Taxi, takeoff, all camera modes, weapons and landing: not validated yet.

Unrelated installed mods also emit startup warnings (NOAutopilot map-zoom patch
and NuclearOptionsGroundForces missing MelonLoader). These are separate from
the F-5 verification; no unrelated mods were changed.
