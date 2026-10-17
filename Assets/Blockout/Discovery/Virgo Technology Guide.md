# Virgo technology prototype

Prefabs: `Blockout/Discovery/Prefabs/Virgo Technology`.
Three beige CRT workstations share one editable archive. Reception has clearance 0, Staff 1, Director 2. Restricted entries are excluded from searches and record lists on lower-clearance computers. Place the Director model in Pierce's office when that reveal is ready. No technology objects have been placed in your map.

Use a workstation to enter its desktop. Click icons, drag title bars, search records, dial telephone extensions, play Relay Break or tune the Line Monitor. Escape or Leave Workstation returns player control. The world keeps running. The model includes a horizontal case, deep CRT housing, keyboard and mouse based on the supplied beige-computer reference. This is blockout geometry, with a full-screen interactive desktop during use.

Edit **Data/Virgo Archive.asset** to change record titles, text, clearance, extensions, call transcripts, carrier frequency and decoded message. Text is draft adaptation of the supplied narrative. No production formulas or medical procedure information is represented. Default carrier: **96.40 MHz**. The public maintenance record gives that clue. Hold a clean signal for two seconds, then Decode. `OnSignalDecoded` and `OnCircuitSolved` can drive doors, objectives or other Unity events. Currently they do not force story progression.

Relay Break is a 4x4 lights-out puzzle: each relay flips itself and its four neighbours. Turn all off. It is generated from a solved board and is always solvable. There is no time limit or punishment. It gives staff a mundane pastime with a slightly inappropriate corporate theme.

The telephone prefab opens the internal exchange directly. Extensions 204, 101, 309 and 000 have draft responses. Calls use tones and readable transcripts, not recorded speech. Computer typing, mouse clicks, boot and telephone sounds are synthesized locally. Authored AudioClip replacements and sound volume are exposed on the terminal. Progress remains in memory for this play session; integration with the horror pack save system is not implemented yet.

Facility changes: `FacilityPowerSequence.BeginOnEmergencyPower` defaults to true. The existing level begins with the restore-power objective, three-fuse progress and a screen marker for the existing fusebox. White facility lamps become dim amber reserve fixtures, preserving their configured color/intensity for restoration. Red/green emergency lamps retain their colors. The third fuse restores original lighting; the security lever still operates its doors. No scene geometry or positions are edited by this update. Disable BeginOnEmergencyPower to retain the older lever-triggered reserve-power timing.

## Narrative mechanics to expand next

- **Staff handsets / pagers:** a late-1990s internal cordless handset or pager feels more appropriate than a modern smartphone. A physical extension directory can reveal who worked where. Let players choose ring, vibrate or mute; ringing should have a clear warning before it attracts a hearing-based enemy.
- **Remote extension distraction:** connect a known room's extension, ring its phone, and use the sound to draw a hearing-based enemy away. Require line power and show which extension is being called. This needs enemy hearing/event integration; current phones do not lure enemies.
- **Frequency evidence:** find carrier numbers on maintenance labels or station logs. Tune and hold through interference to hear or read fragments. Give a visual signal meter and captions so audio is not the sole requirement.
- **Two-channel reconstruction:** alternate between two carriers to combine a date, room number or message. Do not require guessing arbitrary frequencies. Environmental clues should supply the range.
- **Personnel reconciliation:** compare employee departures with access logs, mugs and ID cards. Let the player flag mismatches; reveal Pierce's intake records later rather than exposing the twist at reception.
- **Records sorting / switchboard duty:** small filing or routing tasks establish routine at the station. Repeat them underground with disturbing contents to reframe the same interaction.
- **Otto's final call:** reserve a scripted director-terminal map takeover for the ending. This prototype deliberately does not reveal the global network at reception and does not implement the ending call.

Avoid making every computer a puzzle gate. Some should offer optional records or a moment of normality. Keep the power panel as the level's primary objective, with computers supplying context and clues.
