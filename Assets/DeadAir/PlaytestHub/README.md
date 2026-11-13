# Dead Air playtest hub

Open **Scenes/Playtest Menu.unity**. This is the first enabled build scene. The menu loads three separate experiences, optional feedback, and a separate results scene. **F6** opens the experience switcher during gameplay. F8 remains the existing detailed recorder/replay viewer.

## Experiences

- **AI + Discovery** loads `Scenes/AI Discovery Trial.unity`, copied from your built Discovery Blank facility. Your `D LOOP + A LOOP` file was an almost-empty scene, so it is not used as the playable map. Three native fuse returns finish the run. The facility layout and existing placements are preserved.
- **Security Terminal** loads a separate copy of that facility. `SECURITY TERMINAL - Assign Camera Feeds` has a Security Trial Terminal component. Drag your regular Unity cameras into **Camera Feeds**; only the selected feed is enabled while operating it. Previous camera targets/enabled states are restored on exit. Assign **Remote Doors** and **Remote Lights** for controls. Camera resolution is 640 × 360. The world continues running at the terminal. The default goal is 60 cumulative seconds of operation, editable on `PLAYTEST EXPERIENCE - Settings and Enemy Slot`.
- **Three Doors** is a small separate room with identical sealed doors, one lever per door, green/red/flickering-white lighting, and no enemy. Doors never open or reveal their interiors. First lever choice, distinct lever order, and times are recorded. Trying all three levers finishes after a five-second observation window. The white fixture and its light flicker together. F6 also lets you finish or end early. Edit the Prototype Light components and fixture materials to tune conditions. Conditions have fixed positions: swap positions between testers if you want to separate colour preference from left/right preference. This is not a controlled brightness comparison; the conditions are intentionally editable.

## Your enemy

No pack zombie is substituted. Both facility scenes have an empty `ENEMY SLOT - Place your enemy here`, referenced by Playtest Experience. Put your enemy instance under this slot or assign its scene root to **Enemy Root**. Add your AI's own patrol/navigation setup. The slot does not implement enemy behaviour or generate a navigation mesh. Until your enemy is added, these two experiences have no active enemy.

## Computers and new levers

The existing Virgo computer prefabs now use **Assets/CRTMonitor.fbx** in place of the placeholder CRT shell. Existing keyboard/mouse and records, phone, tuner, and mini-game behaviour remain. The separately placed CRT model is also interactable. The full desktop and idle screen use **Assets/hamterbackground.png**. The idle image is a material on the model's actual blue screen submesh, following its surface; there is no floating image panel. The dedicated CRT screen material projects the image across the screen geometry. **Desktop Wallpaper** controls the interactive full desktop; the physical screen texture is in **Materials/CRT Screen - Hamster.mat**. Existing monitor orientations are preserved.

Levers in Discovery Blank and both generated facility trials use the imported **Assets/Lever.fbx** model with an animated handle and status lights. `Lever_7` is given an interaction collider and, if its targets were empty, the nearest wired lever's targets. Existing target links and jam flags remain. The reusable version is **Prefabs/Lever_7 - Interactive.prefab**; assign **Targets** after placing it. Upper/lower lights show lever state; both show red when jammed. Original object transforms are preserved; old placeholder renderers are hidden.

## Feedback and quick switching

Completion, early ending, and switching save the attempt before leaving. Feedback is optional. **Skip / already answered** continues without requiring text. **Demo mode** skips feedback automatically and labels the run as a demo. Previously submitted feedback is not erased by skipping. If a switch requests another experience, submitting or skipping feedback loads that selected experience directly.

The switcher offers Resume, Finish Test (manual completion), End Early, another experience, main menu, and results. An unfinished experience switched away from is labelled `switched`, not `completed`. Falls/reloads are retained as restarted attempts; app closure records an exit/interruption.

## Booklet and storage

**Scenes/Playtest Results.unity** shows one attempt per page, with previous/next navigation, experience filters, outcome, active duration, total elapsed duration, fuse returns, deaths, interactions, terminal usage, lighting order, ratings, comments, version, and run ID. Add/edit feedback on any page. CSV export includes all booklet runs and their feedback.

Data stays on this machine in `Application.persistentDataPath/DeadAirPlaytests/<run-id>/`. Each attempt has `hub-run.json` beside the existing `session.json`, `positions.bin`, and `events.jsonl`. CSV files go into `DeadAirPlaytests/Exports`. Active time starts when the player is unlocked and excludes pause/switcher/developer-menu time; total elapsed includes those pauses. Terminal operation counts as active play. Startup loading, feedback, and menu time are excluded. Nothing is uploaded. Collect the folder from external testers to review their files on another machine.

The existing recorder is enabled for release builds so standalone playtest builds collect data. Disable **Allow In Release Builds** in DeadAirPlaytestSettings when preparing a non-playtest release.

`Resources/DeadAirPlaytestHub.asset` controls scene destinations and experience descriptions. Scene generation runs through **Tools → Dead Air → Set Up Playtest Hub**. Existing generated scenes are preserved on later runs. Safety scene copies are under `Logs/PlaytestHubSafety`. Automated validation recordings are moved to `DeadAirPlaytests/Validation` so they do not count as player playthroughs.
