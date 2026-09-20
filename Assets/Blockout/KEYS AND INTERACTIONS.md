# Keys, examination text, and barricades

## Rename a key the player sees

1. Select the key in the Hierarchy. On **Interactable Item**, inspect **Pickup Item** to identify the inventory entry it uses.
2. Select `Assets/ThunderWire Studio/UHFPS/Scriptables/Game/Inventory/(Inventory) Demo.asset` in the Project window. Open its **Inventory Builder**.
3. Select that key entry and edit **Title** and **Description**. Example: `Observation Room Key` / `A brass key tagged “Observation.” The observation room is downstairs.` Use wording that matches your level.
4. Keep the key's existing GUID. The door's required inventory item must reference the same entry as the pickup. A display-name change does not require reconnecting the door.
5. On the pickup, **Use Inventory Title** uses the inventory name for the interaction prompt. **Examine Inventory Title** uses it while examining. Disable either only when you want a separate **Interact Title** or **Examine Title**.

Renaming the Hierarchy GameObject only organizes the scene. It does not rename the inventory entry. Several pickups using the same entry share the same displayed title. Create separate database entries for genuinely different keys and assign each pickup and corresponding door to the correct entry.

### Localization

The demo entries include localization keys such as `item.title.key01` and `item.description.key01`. If localization is enabled, those translations can replace the plain Title/Description when the game starts. Edit the matching localization entries too. For deliberately nonlocalized text, the kit supports a leading `*` in its localized string field, e.g. `*Observation Room Key`; the `*` is removed on display. Keep the ordinary title/description consistent as well.

## Change reading/examination information

On the scene pickup's **Interactable Item** component:

- **Examine Title** controls the examination heading when **Examine Inventory Title** is disabled.
- **Paper Text** supplies the text shown by the examination reading action. This is separate from the inventory Description. The kit shows it for **Interactable Type = Examine Item** with **Is Paper** enabled. Filling in Paper Text alone does not make a normal inventory pickup readable. For ordinary keys, use their inventory Description unless you deliberately configure paper-style examination.
- **Hint Message** is another separate interaction hint, rather than the database description.

For examination launched from the inventory, the database item must have **Is Examinable** enabled and a valid examination **Item Object**. The demo key entries currently have Is Examinable disabled and no Item Object. Their inventory descriptions can still guide the player without enabling 3D inventory examination.

## Inventory controls

Default bindings in this project: **Tab** opens inventory; hover an item and press **Space** to pick it up, move the mouse, and press **Space** again to place it. While moving a nonsquare item, **Left Shift** rotates it. **Left-click** opens the item's context menu. Square items do not change footprint on rotation.

The repaired inventory uses a fixed six-column grid matching its logical six-by-six array. The panel fits all 36 cells. Movement and rotation now use unscaled time, so pausing the game does not freeze their animation. Canceling movement returns items in the correct coordinate space.

## Barricaded doors

All four doors whose path contains `Barricade Door` have a **Barricade Handler** connected to **Dynamic Object > Barricade Script**. Each handler lists its own UHFPS plank. Hold the normal Use action for 1.5 seconds to remove a plank. These doors have no separate key lock after the boards are removed.

The object slot expects an **IDynamicBarricade** implementation: use the **Barricade Handler**, not a bare plank mesh or the plank's Barricade Object component. Put the plank in the handler's **Barricades** list. Three boards were added for the previously empty doors; the existing SM_Plank_01 kept its placement.

## Lighting

Fixed facility and directional lights are configured as Baked. Player equipment, examination, pickups, and puzzle indicator lights retain realtime behavior. Static scenery receives lightmaps; the separate **Adaptive Probe Volume** object supplies lighting samples for moving objects, using the project's existing adaptive-probe rendering mode. Re-bake after moving walls or fixed lights. Baked fixture illumination cannot be switched off at runtime merely by disabling its Light component.

Unity's background and moving-object lighting guidance: https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/direct-and-indirect-lighting/light-probes/light-probes

The pre-repair scene snapshot is `Logs/Blockout.BeforeRequestedRepairs.unity`. Bake completion is recorded in `Logs/FacilityBakeStatus.txt`; a configured Baked mode alone is not proof that lightmaps were generated.
