# Facility lights and night setup

The Blockout scene now uses the kit's NightSkybox07, copied to `Materials/Facility Night Sky.mat` with reduced exposure. Its existing directional light has dim blue moonlight; its position and rotation are unchanged. Ambient light and reflections are reduced for night.

`Facility Night - Global Volume` uses `Profiles/Facility Night.asset`: restrained bloom, slightly cool color, mild contrast/desaturation, ACES tone mapping and reduced grain/chromatic aberration. Its priority is 0.5, between the existing kit GlobalVolume (0) and HealthVolume (1). The original player, camera, health and gameplay-effect profiles are unchanged. No fog, depth of field or motion blur was added.

## Place your lights

Drag any of the five prefabs from `Assets/Blockout/Facility/Prefabs` into the scene:

- **Exit Light - Red**: red illuminated housing with white EXIT lettering.
- **Enter Light - Green**: green illuminated housing with white ENTER lettering.
- **Strip Light - 2m** and **Strip Light - 4m**: cool-white strip fixtures with metal housings and end caps.
- **Ceiling Panel - 1.2m Twin Strip**: a wider, two-strip ceiling fixture.

Sign pivots are at the back mounting surface; the face points along local -Z. Ceiling fixtures have their pivot at the ceiling surface and shine down local -Y. All fixtures have real-time spotlights and emissive diffusers. Emission makes the fixture glow; the child Light components illuminate nearby geometry. Adjust child Light intensity/range to suit the room. Fixtures have no collision, so they do not obstruct movement. They are prefabs only; none were positioned in your layout.

## The barricaded door

`DynamicDoor_Locked2_2/Door` now references a **BarricadeHandler** component on `DynamicDoor_Locked2_2`. That handler's Barricades list contains the existing scene instance of `SM_Plank_01`. Neither object was moved, reparented, resized or rotated; the plank's components/settings were unchanged.

The slot rejects a bare plank because it requires **IDynamicBarricade**. The plank has **BarricadeObject**, which handles removal, while **BarricadeHandler** implements the required interface and tells the door whether any linked planks remain. For more planks, add their scene BarricadeObject components to that handler's list. You do not need to parent or move them.

The door's pre-existing **Locked / Manual** settings are preserved. Removing the plank clears the barricade only. A manual lock still needs its existing unlock event/script; changing that behavior was outside the requested reference fix.

## Verification

All 2,556 existing transforms and 3,410 unrelated existing components matched the pre-change snapshot. A saved-scene comparison against the initial file found changes only to the door prefab instance, directional light, RenderSettings, scene root list, and newly added handler/volume records. No existing object records were removed. Checks were performed in edit mode, without running the AI or physics in the working scene.

Reports and the pre-change scene copy are under `Logs/FacilitySafety` and `Logs/FacilityResult.txt`.
