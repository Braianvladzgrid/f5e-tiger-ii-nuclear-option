# Local integration fixture

F5TestProbe.cs is a development-only BepInEx plugin. Never ship its DLL with the
addon. It can override normal control inputs and place native rigid bodies on
a runway or an approach fixture; it applies no extra forces, collision ignores
or damage protection. Flight and landing results must be labelled as fixture
tests, not manual-controller or full-circuit validation.

Commands are read from work/f5/test-command.txt in the development workspace.
The probe holds a newly spawned aircraft at idle with brakes until commanded.
Always remove F5TestProbe.dll from the game addon after the test session.

The first landing probe incorrectly waited for IsLanded before braking; the
second incorrectly latched stale wheel-contact state immediately after a fixture
teleport. Both are failed test setups, not successful landing evidence.
