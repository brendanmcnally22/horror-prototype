# HALVERN RESEARCH

Scale: 1 stud = 0.28 m. Blender coordinates: X along corridor, Y across building, Z up. Positions are part centres. Roblox conversion: (X,Y,Z) Blender -> (X,Z,-Y) Roblox, divide metres by 0.28. Interior floor 60 x 40 studs, ceiling 10 studs, exterior walls 1 stud thick. Doorways 4 x 8 studs; corridor 6 studs clear.

Room clear extents (studs):
| Space | X | Y | Clear size |
|---|---|---|---|
| Decon airlock | 0–9.5 | 17–23 | 9.5 x 6 |
| Corridor | 10.5–60 | 17–23 | 49.5 x 6 |
| Control | 0–29 | 24–40 | 29 x 16 |
| Living / kitchen | 30–60 | 24–40 | 30 x 16 |
| Storage | 30–46 | 0–16 | 16 x 16 |
| Med bay | 47–60 | 0–16 | 13 x 16 |
| Sealed utility reserve | 0–29 | 0–16 | 29 x 16 |

Door centres along corridor: control X20, living X37, storage X40, med bay X53. This orders discovery along the corridor; rooms are side branches. Four double bunks provide eight beds. Sealed reserve is inaccessible background structure.

Every primitive and lettering object is listed in the Blender Text Editor document HALVERN_PARTS.json, with size, position, rotation, material, and RGB color. Roof is hidden for cutaway presentation; enable it for closed interior use. Exterior terrain is a simple massing proxy.

Lighting: fluorescent points at (5,20,9.1), (16,20,9.1), (29,20,9.1), (43,20,9.1), (55,20,9.1), (15,32,9.1), (44,32,9.1), (38,8,9.1), (53,8,9.1); RGB (0.66,0.89,0.87). Power 35W for indices 0,3,6, otherwise 85W. Emergency points at X10,32,58 / Y17 / Z7.5, RGB (1,0.06,0.025), 22W. Exterior moon sun RGB (0.55,0.72,1), strength 2, plus presentation fill at (-9,20,14), RGB (0.6,0.78,0.82), 900W. Broken floodlight emits nothing. Watt values are Blender settings, not Roblox brightness equivalents.

Gameplay flicker specification: independently alternate 100%, 15%, 0%, 100% at 0.08–0.2-second steps, with 3–9 seconds steady between events. Implement light changes in Roblox; geometry export does not carry gameplay scripts.
