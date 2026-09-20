# Blockout optimization

## Your area switching controls

No triggers or startup controllers have been placed in Blockout. Neither prefab has any assigned targets. No scene areas were disabled or selected automatically.

### Area Activation Trigger

Drag `Prefabs/Area Activation Trigger.prefab` into your scene. It is an empty object with a Box Collider set to Is Trigger, a kinematic Rigidbody, and Area Activation Trigger. There is no visible mesh.

1. Position and size the Box Collider across the passage you choose.
2. Drag scene objects or area parent objects into **Enable On Enter**.
3. Optionally add objects to **Disable On Enter**. These are disabled when you enter, not when you leave.
4. Leave **One Shot** off for repeat crossings. Enable it for a once-per-play-session trigger.

For your downstairs example, you decide what belongs to each area. A trigger you place on the downstairs side can enable your downstairs objects and disable selected radio-station objects. Place a separate return trigger where appropriate with the lists reversed. Keep both triggers outside anything either trigger disables. Avoid overlapping the two trigger volumes.

The trigger recognizes the UHFPS PlayerManager on the entering collider or its parents; props and NPCs do not activate it. Enable wins if the same object appears in both lists. Empty entries are ignored. Self/ancestor, player, and game-manager targets are protected from disabling.

Disabling a GameObject also stops its child scripts, audio, colliders and AI. To hide only background visuals while keeping gameplay alive, assign a dedicated visual-only parent that you have organized yourself. This system does not unload scene memory or save visibility state across game loads. The initial state runs again each time the scene starts.

### Disable Objects On Startup

Either add **Blockout > Optimization > Disable Objects On Startup** to an active game manager, or drag in `Prefabs/Startup Visibility.prefab` as a separate active root object. Fill **Objects To Disable** with your chosen scene objects.

It runs early in Awake, then does no per-frame work. Keep its host active and outside its target objects. The list is intentionally empty. Later, your activation trigger can enable the same objects.

## What was repaired

The four existing LOD groups had empty levels, out-of-order thresholds, repeated renderers, and references to unrelated ammo, batteries and the player's jumpscare zombie. Each group now owns only its own local prop renderers, with recalculated bounds and culling below 0.5% screen height. The invalid levels were not real simplified versions of the prop.

Ten generated mesh assets now provide actual reduced-detail levels for 38 heavier static mesh renderers, including tables, ammo meshes and larger props. Original imported meshes and all collider meshes are unchanged. Original LOD0 triangle counts were checked before assignment. Player-held models and animated/AI meshes were excluded.

These use Unity 6.6's **Mesh LODs**, so they do not need additional child objects at different positions. Mesh LODs and the repaired LOD Groups are separate mechanisms. The original mesh is used up close; lower triangle levels are selected as its screen footprint shrinks. For example, the table's levels are 13,652 / 7,163 / 3,581 / 1,792 / 896 / 448 / 319 triangles.

Generation uses Unity's [MeshLodUtility.GenerateMeshLods](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/MeshLodUtility.GenerateMeshLods.html). Generated assets are in `Meshes`; references apply to this scene, not the source kit prefabs.

## Why the counter can show millions

The audit counted 548,020 mesh triangles across the scene, including inactive player equipment. It also found 69 active lights, 63 casting shadows. Mesh totals and the Game view rendering counter measure different things: shadow and other rendering passes can submit geometry repeatedly. The shadow-light count is a likely major contributor to the 8–11 million counter, but no GPU profiling breakdown or before/after frame-rate claim is made here.

Lighting positions, shadow settings and the rendering-pipeline asset were not changed. The 2m and 4m fixture prefabs created earlier contain multiple shadowed spotlights; repeating many fixtures compounds that cost. Once you choose the area lists, switching distant areas off will also stop their lights if those lights are inside the assigned parents. A separate shadow-budget pass would be the next performance step.

## Radio inspection lighting

The scene radio has **Radio Examine Lighting**, connected to its existing examine-start and examine-end events. While examining the radio it raises the Focus-only inspection light from 0.03 intensity / 0.65m range to 0.65 / 1.5m. On exit or disable it restores the exact prior intensity and range. This does not brighten the room, move the radio or change the player's default light settings. Adjust those two fields on the radio if you prefer a softer effect.

The saved-scene verification confirms no object transforms, activation states, collider settings, AI settings, existing lights or environment settings changed. The complete player prefab instance matches the pre-change file. Reports and backups are under `Logs/OptimizationSafety` and `Logs/OptimizationValidation.txt`.
