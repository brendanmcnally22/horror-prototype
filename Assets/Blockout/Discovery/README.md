# Blank discovery prototype

Open **Scenes/Discovery Blank.unity**. The wired HEROPLAYER, foundation and white lighting are joined by a small **EXAMPLES - Elevator Door Fuses** group. The rest of the floor is free for your new map.

Walk forward through the invisible objective trigger. The elevator is ahead-left, the fuse panel and three fuse plinths are ahead-center, and the heavy security door is ahead-right. These are separate examples, not a completed facility layout.

## Wired examples

- **Elevator - 3m travel** has an onboard lever and controls at both landings. Board before using the onboard lever. Controls follow the target state, so either landing can call it back.
- **Security Door - Heavy Horizontal Slide** is a 55 cm thick armored leaf with reinforcement ribs, a frame and a side pocket. Its levers on either side open/close it. Travel is 3.4 m horizontally, speed is 0.55 m/s, and acceleration is 0.35 m/s². Tune those fields on Prototype Mover. The standalone **Security Door - Heavy Slide** prefab is reusable.
- **Objective Trigger - Walk Through** is an empty GameObject with an invisible Box Collider. Resize its Size or move it to define the activation zone. Edit Objective Text, Trigger Once, and Show Fuse Progress. Assign the scene's **Objective Display**, or let it find one automatically. The cyan box appears only in the editor. The display starts hidden and remains visible after entering the trigger.
- The return panel uses the horror pack's inventory fuse. Three original-size pack fuses sit on plinths. The first return turns the nearby red light steady white; the objective tracks each return. Further return events remain editable for your own map.

## Place and tune mechanics

Reusable prefabs are in **Prefabs**. Edit an instance's Inspector to tune that instance, or open its prefab to change the defaults.

- **Light - Red/Yellow/Green Flicker**: color, intensity, range, minimum brightness and random flicker interval. **Light - White Steady** uses the same settings without flicker. These are light sources; add your own fixture meshes as needed.
- **Door - Sliding**: move the root to position it; resize the geometry child to fit. Set **Travel** (local metres) and **Speed** (metres/second). Keep the root and Carriage scale at one.
- **Platform - Elevator / Side to Side**: set Travel, Speed, End Pause and Start At End. Enable **Automatic** for continuous back-and-forth motion, or leave disabled for lever control. The cyan gizmo shows the route. Platforms carry the hero using the framework's elevator parenting. If resizing the deck, resize the Rider zone too. Keep routes clear of ceilings and walls; this prototype has no obstruction/crush detection.
- **Lever**: add scene door/platform components to **Targets**. On moves targets to the end; off returns them to the start. One lever can control several targets. Use **On Changed** for extra effects. Avoid Automatic mode on lever-controlled movers. **Follow Target** synchronizes multiple controls against the first target.

## Three-fuse discovery loop

1. Place one **Fuse Return Panel - One per Map** at the return location, approximately chest height. Its front faces local -Z.
2. Place three **Fuse - Optional Pickup** instances. This is now a variant of the horror pack's **PuzzleItem_Fuse**, retaining its mesh, material, examine/pickup behavior and inventory item ID. The original horror pack prefab also works directly. No per-fuse Loop connection is needed.
3. On the panel's **Discovery Loop**, assign **First Return Lights** to restore steady white light on return one, **Second Return Shortcuts** to open doors/platform routes on return two, and **Final Return Doors** to open an optional exit on return three. Arrays are empty until you build and connect your area.
4. Customize **On First Returned**, **On Second Returned**, **On Returned (count)** and **On Completed** events for your own changes. Required Fuses defaults to 3. **Use Pack Inventory** is enabled on the panel. The pack's inventory controls carrying capacity; you can carry several fuses or return after finding one. Every panel interaction consumes exactly one inventory fuse. Carry Capacity applies only to the legacy placeholder pickup mode.

Use the hero's existing interact binding when looking at a fuse, lever or panel. Pickups require deliberate interaction; walking past never collects or stops the player. The panel reports returned/carried counts in its interaction prompt. Completion does not freeze the player or end play. Keep a route through your layout accessible without fuses if exploration should remain optional.

The return count, objective display and mechanisms are session-only prototype state. Native pickups use the pack inventory, but this loop does not yet persist its progression through save/load. Do not use the old four-fuse progression controller in this map.

**Tools > Blockout > Create Blank Discovery Map and Mechanics** recreates missing initial setup only when the new scene does not exist; it preserves an existing scene. Play checks temporarily instantiate mechanics and do not save them into your blank map.
