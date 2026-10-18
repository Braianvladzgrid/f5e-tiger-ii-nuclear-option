# F-5E Tiger II for Nuclear Option

Development prototype: tier 2, $14,000,000. Blueprinter 2.0.1, Nuclear Option
0.34.2 and Unity 2022.3.62f2. This project is not yet flight-validated.

Exterior and cockpit use the FlightGear F-5E by Prestes Hangar / Dave Culp /
FGUK / Zexe. Model license: CC BY-NC-SA 3.0. This adaptation is free and
noncommercial, with attribution and the original editable model preserved in
`SourceAssets/F5E-original-source.zip`. See the model license and credits in
`Assets/Blueprinter/Mods/F5`.

The first integration uses native FS-12 flight, damage, cockpit displays and
weapon systems. Native pitch surfaces are moved aft and reversed for the F-5
stabilators; thrust vectoring is disabled. One engine controller currently
represents the engine pair, with two nozzle effects. This is an adaptation,
not an authentic F-5 systems simulation. Analog source instruments are decorative.

The model has its own silhouette icon, source landing-gear folding tracks,
three native wheel-contact assemblies, and wing-mounted external stations.
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

Do not publish a stable release until fresh-spawn, taxi, braking, takeoff,
pitch/roll/yaw, all cameras, weapons, gear retraction/doors, landing and logs
have been checked inside the game.
