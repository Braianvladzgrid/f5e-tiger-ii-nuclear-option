# F-5E development verification

Nuclear Option 0.34.2 / Blueprinter 2.0.1. Updated 2026-10-21.

Verified in game on 2026-10-18:
- Dedicated catalog silhouette, tier 2 and $14m purchase.
- Fresh build-8 spawn without structural damage after correcting mirrored
  control surfaces and separating body/wing collision volumes.
- All 28 physics parts remained at 100 health during the second flight test.
- Three native landing gears bound to 17 source animation tracks.
- Native takeoff and approximately two minutes of sustained flight; final
  telemetry about 465 m MSL and 206 m/s, without structural damage.
- Gear-up command hid the source wheels; camera cycling caused no recorded damage.

Test limits:
- A temporary local control probe positioned the aircraft on the runway and
  drove normal control inputs. It applied no extra forces or damage protection.
- The first flight climbed 100 m, then crashed after the probe neutralized pitch.
  The second flight used continued attitude control and remained healthy.
- Human controller handling, weapons firing and landing have not been verified.
- Gear doors/alignment and cockpit visibility still need visual verification.
- Flight balance is provisional; one native controller represents both engines.

2026-10-21 build 9:
- Successfully compiled and installed with a fixed cockpit camera attached to
  the cockpit part instead of the animated pilot head.
- SHA-256: 280d7ee5ffa9da096409b0a67bee4b5b5f40feb8d6bcfbe1220894f6bc262cc4
- Temporary flight-control probe removed from the game installation.
- Fresh in-game cockpit/HUD verification pending.

Evidence: work/f5/test-telemetry-flight1.txt and test-telemetry-flight2.txt
in the development workspace. Test tools are excluded from the release archive.

Unrelated installed mods emit their own warnings; no unrelated mods were changed.

Build-9 runtime attempt: bundle loaded successfully, but before a fresh F-5
spawn the game stopped responding and Player.log recorded D3D11 render-texture
creation failures 0x887a0005. Window-capture recovery also failed with the same
GPU-device error. This attempt does not validate the new camera or HUD.


Fresh build-9 spawn after PC restart, 2026-10-21:
- Mission and F-5 spawn completed without the prior GPU error.
- Tier 2 / $14m and dedicated icon confirmed again.
- HUD rendered; cockpit and exterior/flyby modes displayed.
- Aircraft responded to throttle and moved out of the hangar; throttle and
  airbrake were active on entry, so this is not a controlled taxi/brake test.
- Cockpit still showed empty source instrument apertures. Editor inspection
  found targetScreen at z=7.74 m while the source panel is around z=4.35 m.
- Build 10 moves that native screen to the source panel, preserves the exhaust
  disk in exterior rendering, and matches inner canopy copies to source scale.
  Compilation and fresh-spawn verification of build 10 are pending.

Build 10 result, 2026-10-21:
- Unity compilation and installation passed.
- SHA-256: f2ea7008b7002a3ffa88ea654f867832a09253e9227bfe9385afc6bf9b626583
- Fresh spawn passed; HUD and repositioned tactical display rendered in game.
  Radar sweep, weapon status and aircraft information were visible on the panel.
- No F-5 collision/damage failure appeared in this short stationary/ground check.
- Gear initialization again reported 17 tracks, 9 visual assemblies and 3 gears.
- The source panel still has decorative white details overlapping the display;
  visual polish is pending. Weapon firing and landing remain untested.
- Aircraft was left paused because the current throttle input was active.

2026-10-21 instrument and integration work (builds 11-12):
- Added native-value instruments: airspeed, MSL altitude, pitch/bank, fuel mass,
  engine RPM ratio, native gear state and actual control brake input.
- Added supplementary cockpit HUD text for TAS, vertical speed, derived angle
  of attack and remaining fuel mass. These read simulation values only.
- Replaced disconnected source blanks/knobs; moved the tactical screen below
  the primary instruments. Instrument sizes were iterated after real screenshots.
- Build 11: 202 telemetry samples, max 204.4461 m/s and 462.3613 m MSL,
  minimum recorded part health 100; gear retraction/extension and camera cycling.
- Landing trial 1 reached three-wheel contact but did not record a rollout stop.
- Landing trial 2 crashed after the test controller latched stale gear-contact
  state immediately after placement. This is a FAILED fixture run, not a pass.
- Initial gun fixture changed synchronization state without firing; ammo remained
  unchanged. Revised fixture uses native WeaponManager.FireGuns instead.
- Build 12 bundle compiled and installed; SHA-256:
  ebbba5c3490bcbba332c764002e242787318516ad5cb99b104592f17fcdfeccf.
  Fresh screen-size and revised weapon/landing checks are pending.

Checkpoint: user shutting down PC, 2026-10-21.
Build 12 and revised instrument DLL are installed. The game had loaded the
Free Flight scenario, but the final fresh F-5 spawn was not reached.
Final gauge sizing, revised contact-delay landing fixture and native FireGuns
fixture therefore remain unverified. Resume there; do not claim completion.
Latest runtime DLL SHA-256:
DA03BB62F16BD4E827233FA45159C32CB1739449D4DC1648AB4DF86B7C0AD759

2026-10-22 visual refinement, build 15:
- Added paired segmented nozzle rims and recessed turbine/interior geometry.
- Separated exhaust steel, canopy-frame and tire materials from shared body paint.
- Recalculated source normals with a 65-degree smoothing angle.
- Updated canopy tint/reflection and body material response.
- Aligned rendered exhaust effects with the model's openings; native nozzle force
  transforms, thrust values and aircraft mass parameters were not changed.
- Collision assets have no changes relative to the previous committed version.
- Unity build and close-up render passed. Bundle SHA-256:
  6b38a21ddf8e182a9f5e483180dee585a7c2c24ce22196d2d13d99fd2ef31d4a.
- Fresh in-game spawn and screenshot review pending at this checkpoint.

2026-10-22 build 15 runtime verification:
- Fresh hangar and player spawn show the segmented twin nozzles, darker exhaust
  collars and revised body/canopy materials. Tier 2 / $14m confirmed in hangar.
- Live cockpit gauges and supplementary HUD initialized without F5 exceptions.
  Primary dials are visible; the low panel still needs further layout/detail work.
- Native-force takeoff fixture climbed 100 m, then sustained flight at roughly
  195 m/s / 442 m MSL with all 28 recorded parts at 100 health. Gear-up completed.
- Revised FireGuns fixture did not consume ammo (1 -> 1): weapons are NOT validated.
- Revised approach fixture stopped on three wheel contacts, but tail health fell
  to 77.35938 following a terrain2_roads91 collision. This is NOT a clean landing
  pass; approach/controller and tail clearance need investigation.
- Evidence retained in work/f5/test-telemetry-build15.txt and Player-build15.log.
- Supplementary AOA now displays -- below 10 m/s airspeed, avoiding meaningless
  stationary readings. Compiled successfully; this last small runtime change has
  not had a new in-game spawn yet. Runtime SHA-256:
  de5c9b373ef00c787890a63ad85f39d5f8ad025535988ed438d63153ba33f765.
- Temporary F5TestProbe.dll removed from the game addon after testing; only the production runtime DLL remains.

2026-10-22 build 18, station and cockpit corrections:
- Seven external stations: 2 wingtip rails (IRM-S2 single), 4 underwing pylons,
  1 centerline pylon. Internal cannon is separate. Each external mount accepts
  single native stores only; removed inherited multi-store racks/large options.
- Mounts now bind to stable wing/fuselage AeroParts, with distinct transforms
  and hardpoint indices. Moved stores forward and down; added visual pylons.
- Editor audit checked all allowed store meshes against flap/aileron bounds at
  local X deflections -45,-30,-15,0,15,30,45 degrees: no AABB overlap.
  See Validation/station-audit18.txt. This is not a release/separation test.
- Exterior-only copy of source seat, consoles and panel makes cabin contents
  visible through the canopy; excluded duplicate glass/frame interior copies.
- Live instrument cluster scaled to 72%, moved down 35 mm and forward 130 mm.
  Airspeed/altitude dials now show knots/feet; native game HUD units remain as set.
  Removed occluding imported blanks only from first-person cockpit, preserving
  the exterior copy. New live panel includes five gauges, artificial horizon,
  annunciators and supplementary HUD flight data.
- Unity compilation, editor audit and exterior cabin render passed.
- Aircraft bundle SHA-256:
  2d4287bf5b5f5cf087f3a699925153dfb8a7821cd5abb65fed9eaf31b4ee463a
- Runtime SHA-256:
  c5d6198d883b56c233848698427c958091c297f808333959fb8377e306cf1d95
- Installed both files. Fresh in-game cockpit/loadout review is still pending.
- Temporary F5TestProbe is installed for this session and must be removed after
  integration checks, before normal use. Landing/firing remain unvalidated.

2026-10-22 build 19 correction after loaded-flight review:
- Build 18 compact cockpit and revised five loadout groups verified in game.
  Configured 3 GPO-500 + 2 MMR-S3 + 2 IRM-S2, totaling seven stores.
- Build 18 takeoff succeeded but bombs struck the ground during placement/settling,
  damaging fuselage/inner-wing parts. This is a FAILED ground-clearance result.
- Raised underwing mounts to y=-0.15, centerline to y=-0.35; shortened visual
  pylons. Removed 500 kg bombs from centerline options; 125/250 kg remain.
- Build 19 compiled and passed the surface-deflection mesh audit again.
- Native gun firing with gear retracted verified in build 18: 750 -> 392 rounds
  in the three-second fixture, safety=False. Evidence: work/f5/test-telemetry-
  build18-ground-clearance-fail.txt. This does not validate every external weapon.
- Expanded native HUD pylon-index coverage to include centerline/cannon indices,
  fixing the PylonIndicator.UpdateImages IndexOutOfRangeException seen in build 18.
  Added a compact per-station store-count row on the F-5 instrument panel.
- Installed build 19 bundle SHA-256:
  4e5c448ad1086d24a68683bd767a4d26dabc952e4555081912e07b215451a676
  Runtime SHA-256:
  f4ec2f5b7c5b2f763cb0e2cc22aa7c3ebe3bdb2402891ae6981f44dc55308f85
- Final fresh-spawn ground-clearance/HUD checks are pending at this checkpoint.
- Probe DLL moved out of the game directory after this test process loaded it.
  It remains active only in the current game process; next launch is normal.

Build 19 verification completed 2026-10-22:
- Fresh cockpit reviewed in game: compact gauges, attitude instrument and HUD
  telemetry visible; occluding source blanks removed. Seven-station readout loads.
- Loadout menu confirms centerline 500 kg option removed; two GPO-500 underwing
  and one PAB-250 centerline used for this clearance check.
- Fresh spawn and runway settling retained all 28 recorded parts at 100 health.
- Controlled native-force takeoff climbed >100 m; subsequent recorded flight
  also retained all parts at 100. No F5Contact events in this test log.
- Short approach/landing fixture reached a stopped state, all 28 parts at 100
  health (LANDING STOPPED at 2026-10-23T01:52:45Z). This is a local placed-approach
  fixture, not a manually flown circuit or validation at every airfield/loadout.
- PylonIndicator index exception did not recur; F5HUD expansion logged.
- Evidence: work/f5/test-telemetry-build19.txt, Player-build19.log.
- External weapon launch/separation, manual controller landing circuits and
  complete orbit-camera behavior are still unvalidated. Keep prototype status.
- Temporary probe removed from addon folder. Test process closed after checks.
