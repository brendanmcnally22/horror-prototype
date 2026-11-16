# Security camera / door-control prototype

Open `Assets/Blockout/Discovery/Scenes/Security Camera Prototype.unity` and press Play.

1. Walk to the green security console and use the existing Interact control (E by default).
2. Select CAM 01 or CAM 02. The live feed shows that channel's door and its current state.
3. OPEN DOOR / CLOSE DOOR operate the assigned shutter. A1 is standard access. B1 requires the green keycard in the player's inventory.
4. Exit with the onscreen button or Escape. Pick up the green card on the left-hand table using the normal UHFPS interaction, then return to the console to authorize B1. The card is not consumed; both open and close commands require it for restricted doors.

Only the selected camera renders, and only while the panel is open. The existing player, inventory and interaction controls are retained. Camera feeds use UHFPS CCTV Camera components and a separate render texture; gameplay continues while watching the panel, so shutters move live. The scene is intentionally separate from the existing discovery and lever scenes.

## Reuse

This folder contains standard/restricted door prefabs and an unwired Security Control Panel prefab. After placing a panel, add Channels in its Inspector and assign a label, a UHFPS CCTV Camera (with Live Camera assigned) and a Security Panel Door. Add as many channels as needed; this initial UI is designed for the two-channel prototype.

On a door, set Requires Keycard, Required Card Id and Card Label. The configured green card uses the existing UHFPS inventory GUID `96cdd54e574046aeb73adf165af2a6f5`. Assign a different existing item's GUID for a different access card. The gate checks actual inventory each time; there is no permanent unlock or card consumption.

Prototype Mover controls travel distance, speed and acceleration. Security Panel Door's Safety Center/Size cover the doorway. A player in the volume blocks closing; entering during closure causes reopening. Resize this volume with the doorway. This prototype detects the player, not arbitrary loose props or future enemy characters.

On Access Denied, On Open Requested and On Close Requested are UnityEvents for authored audio, indicators or future alarm logic. No alarm, enemy behaviour or permanent save-state logic is implied by this prototype.

## Validation

`Tools > Dead Air > Test Security Camera Prototype` checks camera routing, denied/authorized commands, the actual card pickup, door travel, card retention, closing obstruction and safety reopening. Results: `Logs/SecurityCameraPrototypeCheck.txt`.

The scene builder preserves an existing prototype instead of overwriting it. The new scene is not automatically added to build settings.
