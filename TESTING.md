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

