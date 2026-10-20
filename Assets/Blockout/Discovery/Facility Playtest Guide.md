# Facility interactions and playtest tracking

## Current level

The Hero Player has `Facility Player Rules / Disable Jumping` enabled. Jump input cannot enter the Jump state. This is a level setting, not a change to every UHFPS player prefab.

`JAMMED LEVER` and `Jammed Lever 2` have `Prototype Lever / Jammed` enabled. Trying them gives a small handle recoil and a message, without changing any targets. Optional `Jam Sound` accepts an audio clip.

Use the electrical panel once to open its cover, then use it again while carrying fuses to insert them. The three existing Horror Pack pickups use the native inventory. The fourth panel socket is already filled; the three empty sockets count toward facility restoration. You can return each fuse separately, or insert several carried fuses together. The cover interaction lives on the child `Panel interaction - Open then Insert`.

## Platform safety prefabs

Find both prefabs in `Assets/Blockout/Discovery/Prefabs/Platform Safety`.

- `Platform Safety Cage - 3m - Entry minus Z`: three railing sides with solid collision and a two-leaf sliding entrance gate. Sized for a 3 × 3 metre deck. Parent it under the platform's **Carriage**, set local position to `(0,0,0)`, and rotate around Y to face the landing. The default entrance faces local **-Z**. Assign `Mover` to the platform root if it cannot be found automatically. Keep the root scale at one.
- `Starting Elevator - Sliding Doors 2.55m`: already added to STARTING ELEVATOR's Carriage, fitted to its existing opening. Starts closed and opens at the lower landing.

The mover waits for the gates to close before traveling. A player in the entrance prevents departure and receives a clear-doorway message. Gates reopen at configured endpoints. Tune `Door Speed`, `Slide Distance`, `Open At Start`, and `Open At End` on the gate. Tune travel and speed on the existing mover. Leave room beyond the doorway for the leaves to slide outward. Put the entire cage under the moving Carriage, not the stationary platform root.

## Empty tracking triggers

Find these in `Assets/Blockout/Discovery/Prefabs/Playtest Tracking`:

- **Tracking - Zone Visit and Dwell:** logs entry, exit, and time spent inside. Drop in the level, give `Zone Id` a unique meaningful name, and resize its Box Collider. It is invisible during play.
- **Tracking - First Visit Milestone:** records only the first entry per play session. Useful for first seeing the panel, reaching storage, or discovering a shortcut.
- **Tracking - Session Logger:** optional explicit session logger. Existing player and interaction tracking already starts one automatically, so it is not required in this scene.

Trigger boxes must intersect the player's body. Do not disable the Box Collider or its Is Trigger option. The component's `LogEvent(string)` can also be called from a UnityEvent for an arbitrary milestone.

The level automatically tracks player movement samples, stationary time, deaths, lever uses, jammed attempts, fuse pickups, panel opening, fuse returns, and safety-gated platform arrivals. `movement_sample.value` is distance since the previous sample; `duration_seconds` is stationary time during that interval. Teleports greater than 5 metres in a frame are excluded. Platform riding is currently included in movement distance. Zone exit duration is time since entry. Fuse return value is the cumulative returned count.

## Collecting results from builds

Each **play session**, including a standalone build, writes a separate local CSV with session ID, timestamps, build version/GUID, scene, event, zone, world position, value, and duration. Set a distinct Player Settings version for each design iteration. Editor sessions use Unity's editor build GUID, so compare standalone builds by their build GUID/version.

Windows location for this project's current company/product settings:

`%USERPROFILE%/AppData/LocalLow/DefaultCompany/Horror Test/PlaytestLogs/`

The generic location is `Application.persistentDataPath/PlaytestLogs`. Files flush after each event and close on exit; an interrupted session may lack a final session_end row. Logging remains local. Nothing is uploaded automatically.

In Unity, **Tools → Blockout → Export Playtest Logs** copies local CSVs into the project's `PlaytestExports` folder. For external testers, collect their PlaytestLogs folder after playing and combine the CSVs in Excel, Sheets, or your analytics tool. Merely building the game does not produce play data; each run of that build does.

Useful comparisons: time from level_start to panel_opened, first fuse time, intervals between returns, repeat jammed attempts, zones with long dwell times, deaths by position, and sessions ending before three returns.
