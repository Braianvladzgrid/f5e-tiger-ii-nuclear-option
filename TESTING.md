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
