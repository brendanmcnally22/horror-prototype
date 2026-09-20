# Facility progression

The scene uses UHFPS's Objective Manager, objective notification, Hint Trigger, inventory, examination reading panel, and Dynamic Object custom-unlock interface.

## Player sequence

1. Open **Experiment Room Fusebox** downstairs. The top HUD shows **RESTORE FULL POWER TO THE EXPERIMENT ROOM**, and the objective is added to the kit's inventory objective list.
2. One fuse starts installed. Collect and insert the other three existing fuse pickups.
3. Before all four sockets are filled, trying **OBSERVATION DOOR** gives the power hint, including when carrying the patient keycard.
4. Filling all four sockets completes the objective, changes the five **Experiment Room Lights** to white, and prompts a return to the experiment room. Without the keycard, the hint also points to the patient's body on the table.
5. The door opens only with restored power and the green **Observation room Keycard**. The card is retained.
6. Walking through the door into the small exit trigger shows **LEVEL COMPLETE** and pauses gameplay. **Continue Exploring** dismisses the completion panel and resumes the blockout; the win stays recorded.

## Edit text and objectives

Select **Facility Progression - Power and Observation** in the Hierarchy to edit the objective and conditional hint wording. Its references identify the door, fusebox, patient keycard, top label, win panel, and five lights.

The existing Hint Trigger on **OBSERVATION DOOR/Door** now uses **Event**, repeats on attempts, and is called by the door's Locked Event. It previously expected a physics trigger-enter event on the ordinary door collider. Literal hint text is now supplied correctly instead of being interpreted as a localization key.

`Facility Objectives.asset` can be edited using the UHFPS objectives Inspector. The kit's database entry provides the inventory objective-list wording. If changing the objective title, update both this asset and the progression component's **Power Objective** field.

## Change the power hall key's reading text

Select **Power Hall Key Reading.asset** in this folder. Edit **Title** and **Reading Text**. Both the scene pickup and **Power Hall Key - Examine.prefab** share this asset. No code changes are needed.

Examine the key and use the kit's displayed **Read** control to toggle the reading panel. Before pickup, put it back when finished reading, then use the normal pickup control. After pickup, choose **Examine** from the key's inventory context menu.

The scene uses **Facility Inventory.asset**, a copy of the original database with the same item IDs. The power hall key entry is examinable and references the new examine prefab. Inventory display title/description can be changed separately in this database's UHFPS Inventory Builder. Keeping the original item IDs preserves existing key-to-door links.

The kit's examination reader was extended to accept inventory pickups explicitly marked **Is Paper**, as well as normal paper examination objects. Ordinary unmarked items retain their behavior.

## Stairs

`Assets/Blockout/Prefabs/Building/Stairs_L_Compact_Exit.prefab` is a reusable 1.2 m wide L-shaped stair with a 2.91 m rise, seventeen approximately 17 cm risers, and a turning landing. An instance reaches the exit-sign doorway shown in the reference image. Existing walls, doors, props, and player placements were preserved. The stairs have physical step colliders and contribute to baked lighting.

## Lighting and saves

The five color-changing room fixtures stay realtime and use their own non-baked emissive diffuser material, so red lighting is not permanently embedded in the lightmaps. Other eligible fixed fixtures are baked. Names containing **DO NOT REBAKE**, **DONT REBAKE**, **DO NOT CHANGE**, or **DONT CHANGE** are excluded from light-setting changes. Player equipment and inspection lights remain realtime.

The progression state is registered with the UHFPS Save Game Manager. Saves created before this new progression setup do not contain its new state token; use a fresh playthrough/save for testing this version.

Verification: `Logs/FacilityProgressionValidation.txt`, bake result: `Logs/FacilityProgressionBake.txt`. Backup of the scene before setup: `Logs/Blockout.BeforeProgression.unity`.
