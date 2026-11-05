# Heavy two-hand lever prototype

Open `Assets/Blockout/Discovery/Scenes/Heavy Lever Prototype.unity` and press Play. This is a separate test chamber; Discovery Blank and its placements are preserved.

Walk up to the wall mechanism and **hold the kit's Interact control (E by default)**. Pulling takes five seconds. Movement and equipped items are unavailable while gripping; looking still works. Releasing early cancels and lets the handle wind back over two seconds. Completion latches the lever and raises the test shutter. The yellow RESET TEST station resets the lever, shutter and hearing dummy.

The crossbar, pivot drums, side arms, grip sleeves and visible two-hand blockout gloves are procedural prototype geometry. These are not a finished skeletal two-hand animation or the model from the reference image. The glove/sleeve group is explicitly named for replacement with authored hands/animation later. The existing first-person controller and camera are retained.

## Tuning and reuse

Drag `Heavy Two-Hand Lever.prefab` into another scene. Assign `On Completed` to the mechanism you want to operate and `On Reset` to its reset action. The test-scene instance uses the existing Prototype Mover for its shutter.

On Heavy Two Hand Lever, tune Pull Seconds, Return Seconds, Raised/Lowered Angle, Use Distance, Noise Radius and Noise Pulse Interval. Mechanism Audio is positional; the generated ratchet loop and kit latch sound are replaceable AudioClips. Audio volume and gameplay hearing radius are intentionally independent.

## Friend's enemy AI

`HeavyLeverNoise.Emitted` broadcasts a `LeverNoiseSignal` containing world position, radius, strength and source object. It emits regularly during pulling and a stronger pulse on completion. Add `HeavyLeverHearingReceiver` to the friend's enemy root and wire its **On Investigate Position (Vector3)** event to the AI's investigation-target method. The receiver filters by distance and minimum strength, and unsubscribes when disabled.

Alternatively subscribe an adapter to the noise event and translate it into the AI's own hearing system. Wall occlusion, alert memory, prioritization, pathfinding, attacks and enemy behaviour belong in that adapter/AI. Do not infer that this test receiver handles walls.

The capsule in this scene is a clearly labeled **hearing dummy**, not the friend's AI. It turns red and approaches the sound in the unobstructed chamber. It has no collision, attacks or navigation simulation. Replace it once the actual AI scripts/prefab are available; no dependency on that AI has been invented.

## Verification

`Tools > Dead Air > Test Heavy Lever Prototype` runs the focused test in this scene and restores the previous play-mode startup scene. It refuses to discard unsaved scene changes. Results are written to `Logs/HeavyLeverPrototypeCheck.txt`.

The builder at `Tools > Dead Air > Create Heavy Lever Prototype Scene` preserves an already-created scene instead of rebuilding over your edits. This prototype is not added to the game's build list automatically.
