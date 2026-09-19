# Blockout starter

Open `Assets/Blockout/Scenes/Blockout.unity` in Unity and press Play. Click the Game view to capture movement input. The scene contains the UHFPS player and its wired manager, a 40 x 40 metre solid foundation, and neutral lighting. Your original SampleScene is untouched.

## Drag-and-drop folders

- `Prefabs/Building`: floors, walls, ceiling, doorway, pillar, cover, cube, stairs and ramp. All have collision. Root scale is one; pivots are at floor level. Floors extend below their pivot, walls extend upward. The ceiling underside is 3 m above its pivot. Stairs and ramp climb along local +Z, rising 3 m over 6 m. Use 0.25 m position snapping and 90 degree rotation snapping for walls.
- `Prefabs/Setup/Player Setup`: the complete player AND manager, with camera and player references connected. Use only ONE per scene. The supplied scene already has it. Move its HEROPLAYER child to change the starting position; keep its feet just above the floor.
- `Prefabs/Setup/Point Light`: optional light for enclosed spaces; raise it above floor level after placing it.
- `Prefabs/Kit`: categorized prefab variants of the kit's Props, Interact, NPC, CCTV, Particles and Misc collections. These stay linked to their original prefabs; the originals are unchanged. UI implementation prefabs are intentionally excluded from the world-object palette.

Kit items retain their original behavior. Doors, pickups and props are useful for testing; puzzles, locks, CCTV, generators and NPCs may require scene-specific targets, matching item IDs, patrol routes or a baked navigation mesh. Being in this folder does not automatically wire a puzzle or bake navigation. Start your layout with Building pieces, then add gameplay as needed.

## Why the initial scene was troublesome

UHFPS is an interconnected game framework. Its player prefab is not a standalone FPS controller. The official `Tools > UHFPS > Setup > Add HEROPLAYER` command creates both HEROPLAYER and GAMEMANAGER, assigns the player's MainCamera, and assigns the manager's Player. The manager also handles UI, input, inventory and the startup fade/unlock sequence.

The saved SampleScene used the demo HEROPLAYER and GAMEMANAGER prefabs. The player's camera was assigned, but GAMEMANAGER's Player was null in the prefab and the scene had no override assigning it. PlayerPresenceManager accesses Player during Awake and freezes/unlocks its components, so that missing reference can break startup. The scene also included a zombie, which is unnecessary for a blank layout scene. This starter uses the kit's canonical Resources/Setup prefabs and packages their references together to prevent that wiring mistake.

If setting up another scene manually, use the kit's setup command and remove any unrelated default camera. Alternatively drag in the combined Player Setup prefab once. Leave the manager and HUD in place even if you are only testing movement. Avoid Reload Setup on customized objects unless you intend to replace them.

The installed kit's Ultra Quality render-pipeline asset also had Dynamic Batching enabled. Unity 6.6 reports this removed feature as an error. This obsolete setting has been disabled; it no longer had a rendering effect in this Unity version.

`Setup Report.txt` records the generation checks. `Tools > Blockout > Create Starter Assets` can create the initial assets if import was interrupted; it refuses to overwrite an existing blockout scene.
