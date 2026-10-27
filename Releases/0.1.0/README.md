# F-5E Tiger II for Nuclear Option

First playable release (0.1.0 beta): tier 2, $14,000,000. Blueprinter 2.0.1, Nuclear Option
0.34.2 and Unity 2022.3.62f2. Native-force takeoff, two supersonic power cycles,
gear cycling, camera switching, weapons and a short placed approach/stop have
been checked. See TESTING.md for evidence and limits.

Exterior and cockpit use the FlightGear F-5E by Prestes Hangar / Dave Culp /
FGUK / Zexe. Model license: CC BY-NC-SA 3.0. This adaptation is free and
noncommercial, with attribution and the original editable model preserved in
`SourceAssets/F5E-original-source.zip`. See the model license and credits in
`Assets/Blueprinter/Mods/F5`. Original integration scripts are MIT-licensed;
see LICENSE-code.txt. The model and its derivatives retain CC BY-NC-SA 3.0.

The first integration uses native FS-12 flight, damage, cockpit displays and
weapon systems. Native pitch surfaces are moved aft and reversed for the F-5
stabilators; thrust vectoring is disabled. One engine controller currently
represents the engine pair, with two nozzle effects. This is an adaptation,
not an authentic F-5 systems simulation. The source instruments are supplemented with live native-value gauges and a cockpit HUD data block. This does not reproduce authentic F-5 avionics.

The exterior includes the separate source pilot model. The twin native
afterburner flames retain their ignition/flicker with a parent scale fitted to
the nozzles. Exterior pilot visibility and both flame ignition/extinction were
confirmed in game. A clean-loadout controlled flight reached about 1835 km/h
at 1735 m MSL; this is not a performance certification for all loads/altitudes.

The suspected spontaneous slowdown failure was investigated with first-damage
stack traces. The combat repeat began with a missile penetrating the aircraft,
followed by blast damage and self contacts. Two acceleration/deceleration cycles
in a local noncombat scenario then preserved all 28 parts at 100 health. Normal
damage and collisions were retained; no structural-strength workaround was added.
The earlier hull-clearance candidate is not included. Individual earlier crashes
without first-damage traces cannot be retroactively assigned a proven cause.

The model has its own silhouette icon, source landing-gear folding tracks,
three native wheel-contact assemblies, and seven single-store external stations (two wingtip rails, four underwing pylons, one centerline pylon). Only IRM-S2 singles are allowed on the wingtip rails; centerline stores are limited to 125/250 kg bombs for ground clearance.
The runtime support is scoped to `F5E_TigerII`: mass stations, manual-only
braking and source gear animation. It preserves the Su-25 runtime's brake patch
when both plugins are present.

Local game dependencies are linked from the existing development setup and
must never be included in public distributions. Recreate those dependencies
from your owned game through Blueprinter when moving this project elsewhere.

Build using `F5AircraftBuilder.BuildAndPreview` in Unity. Compile the helper
with `RuntimeSupport/compile-runtime.ps1`. Install the compiled `.nobp` and
`F5FlightSupport.dll` together in
`BepInEx/plugins/com.nikkorap.blueprinter/addons/vladzgrid.f5/`.
Restart Nuclear Option and spawn a new aircraft after each update.

Remaining broader checks: full manual-controller flight/landing circuits,
multiplayer synchronization, every permitted weapon/load combination and
performance across altitudes. These are not certified by the local fixture.



Download the beta ZIP from the [GitHub release](https://github.com/Braianvladzgrid/f5e-tiger-ii-nuclear-option/releases/tag/v0.1.0).
