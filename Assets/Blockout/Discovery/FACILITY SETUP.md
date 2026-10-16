# Facility setup

All new assets are in **Assets/Blockout/Discovery/Prefabs**. Search the Unity Project panel for `Facility Light` or `Facility Window`.

## Steady lights

- **Facility Light - White Steady**: normal facility circuit.
- **Facility Light - Red Steady**, **Yellow Steady**, **Green Steady**: emergency circuit by default.

These are 2 m ceiling fixtures with a visible housing and diffuser. The root mounts at the ceiling surface and the spotlight shines downward. Each uses one short-range spotlight with shadows disabled. There is no flicker or per-frame light animation, and the emissive material does not bake permanent illumination that would remain after a power cut. Set Color, Intensity, Range, Glow and Emergency Supply on **Facility Lamp**. Use a few fixtures per room and avoid overlapping many light ranges. Performance still depends on the total scene and hardware; this is a lightweight setup, not a measured frame-rate guarantee.

New fixtures are prefabs only: none were placed in your layout. The existing world lights have realtime shadows disabled. The night sky is **Discovery Night Sky.mat**, with subdued moonlight, reflections and ambient light. Existing light transforms were preserved. Two emergency point lights are components on the existing security and storage levers; no new scene objects were added for them.

## Security and power sequence

**LEVER1** targets both objects named **BOTH OF THESE ACTIVATE BY LEVEL 1**. Both leaves slide sideways into their existing pockets. Only their moving leaf transforms were fitted to the existing jamb/header opening; door roots, frames and pockets were unchanged. Speed is 0.55 m/s, acceleration 0.35 m/s². Adjust the Prototype Mover fields to tune the movement.

The **Facility Power Sequence** on LEVER1 waits until both doors reach their open endpoint. It then cuts the normal light circuit and ambient work lighting, enables red emergency lights at the existing levers, and reveals the three-fuse objective. Closing the doors or using the lever again does not restart the blackout. The existing walk-through fuse-objective trigger is gated until the blackout, so it cannot announce that objective early.

The existing horror-pack **Puzzle_Fusebox** is connected to the objective. Its fourth socket has a preinstalled fuse, leaving three to collect. Native pack pickups and the inventory selector remain in use. Returning the first fuse restores white light near LEVER1; the third restores the facility circuit and completes the objective. The pack fusebox can insert multiple carried fuses in one selection, firing the corresponding progress events. The earlier example return panel also remains functional.

**STORAGE LEVER** targets **STORAGE PLATFORM**. Its existing position, orientation, scale and travel distance were preserved; Automatic mode is off so the lever controls the outward/return motion.

## Observation windows

**Facility Window - 4m / 8m / 12m Observation** are 1.6 m high, with reinforced frames, mullions every 2 m, tinted transparent glass and collision. The root is centered horizontally at the bottom of the frame. Depth is 0.3 m. No realtime reflection cameras or expensive refraction effects are used. These are standalone window assemblies; position them in wall openings when you build the area. They do not cut holes in existing walls.

## Preservation and validation

The setup captured the live scene before editing and compared all 1,773 existing transforms. Only the two authorized moving door leaves changed position/scale. No scene objects were added, removed or reparented; frames and the surrounding layout stayed unchanged. The saved backup and transform snapshot are in **Logs/FacilityDiscoverySafety**. The authoring report is **Logs/FacilityDiscoveryResult.txt**; the separate play-check result, when run, is **Logs/FacilityDiscoveryCheck.txt**.

This remains a session-only prototype. The power sequence and objective count are not yet integrated with persistent save/load.
