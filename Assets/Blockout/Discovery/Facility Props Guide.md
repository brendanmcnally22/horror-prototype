# Facility placeholders

Find the kit in **Assets > Blockout > Discovery > Prefabs > Facility Props**.
Drag prefabs into your map yourself. The generator does not place anything or save your scene.

- Furniture: bench, examination table, desk, chair, shelves, lockers, cabinet, freezer, opaque containment pod, console, crate, sealed bin, divider and static gurney.
- Carts: supply and equipment variants. Stand at the yellow rear handle, hold your Use button and move forward/back to push or pull. Release Use to let go.
- Debris: rubble, broken wall panels, fallen shelf, scattered crates, cables and floor grate.

These are simple editable blockout meshes with shared materials, no lights and no shadow casting. Debris stays static; the cable bundle has no collision. Other debris uses simple box collision.

Cart tuning: expand **Cart Body - Push Settings > Movement adapter**. Adjust Object Weight, Walk Multiplier, Hold Distance and Move Direction. The defaults are straight-line movement on level floors with rotation locked. Rotate the prefab to choose the travel direction. Keep its scale at 1; resize its editable visual children instead. Its main box collider defines the obstruction size. Collision checks include Default, Ground, Slope and NPC layers; other Interact-layer props are excluded by the native pack implementation. These are guided puzzle carts, not free-rolling physics carts.

Furniture geometry can be resized, recolored or replaced inside each prefab. The containment pod is opaque for inexpensive prototyping.

To recreate missing kit prefabs, use **Tools > Blockout > Create Facility Furniture and Debris Prefabs**. Existing prefabs are preserved.
