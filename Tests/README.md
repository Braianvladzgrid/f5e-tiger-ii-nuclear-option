# Local integration fixture

F5TestProbe.cs is a development-only BepInEx plugin. Never ship its DLL with the
addon. It can override normal control inputs and place native rigid bodies on
a runway or an approach fixture; it applies no extra forces, collision ignores
or damage protection. Flight and landing results must be labelled as fixture
tests, not manual-controller or full-circuit validation.

Commands are read from work/f5/test-command.txt in the development workspace.
The probe holds a newly spawned aircraft at idle with brakes until commanded.
Always remove F5TestProbe.dll from the game addon after the test session.

`stations` lists native weapon stations. `fire_station N` selects that station
and calls WeaponManager.Fire for 0.3 seconds with native safety and ready checks.
Ammo changes demonstrate firing, not target hits. Weapon.IsAttached describes
hardpoint integrity and must not be used as a launch-separation measurement.
Camera state/pan/tilt is logged without overriding camera input.

`full_power` disables the fixture's cruise-speed throttle hold after `takeoff`;
the aircraft still uses native forces with altitude/attitude control. `cruise`
restores speed hold while preserving the current altitude target. Log both nozzle afterburner amounts and flame renderer
states when testing ignition at throttle 1 and extinction below 0.9. Never infer
maximum speed from a cruise-hold run. Rewired bindings are read after isReady.
The full-power altitude target is 1500 m above the runway; the old 300 m
target intersected terrain after a long straight run. Monitor terrain and end
the run before mountains or map limits. `view_side` / `view_back` frame an
already selected orbit camera for visual inspection only, without certifying
the user's camera mappings.

The first landing probe incorrectly waited for IsLanded before braking; the
second incorrectly latched stale wheel-contact state immediately after a fixture
teleport. Both are failed test setups, not successful landing evidence.


Use a noncombat scenario for flight acceptance. Free Flight - Heartland includes
armed carriers: the Oct 26 first-damage trace proved a missile hit before the
observed self contacts. create-flight-validation-mission.py generates a separate
user mission from the owned game's temporary mission, assigning all bases/units
to Boscali. It does not disable aircraft damage or physics. Do not distribute
the generated game mission as part of the aircraft addon.

The probe traces the first damage/joint-break calls with stack traces. The
trace is bounded per game session. Restart the game when a fresh first-event
trace is needed. The approach command should be used on a fresh aircraft near
a land airbase; a distant ship/invalid runway can make the fixture fail.
