# Choose Your Trees requirements

## Goal

Let players prioritize cutting individual trees while preserving normal cutting designation, reachability, and other eligibility rules. Lumberjacks should choose eligible higher-priority trees before lower-priority ones.

## Tree panel

- Add a **Lumberjacking** section to the selected tree's panel.
- Provide a priority selection row similar to the game's workplace priority row.
- Show chopping progress in a progress bar if the game exposes reliable progress for an individual tree.

## Assign Priority tool

Extend the existing **Assign Priority** tool to set tree-cutting priority, including area selection, while retaining its existing construction behavior.

## Configuration and priority lifetime

- Provide a setting through Mod Settings to choose whether priority belongs to a tile or an individual tree. Default to **tile**.
- In tile mode, priority belongs to the tile and applies to trees that grow there later, including after a tree is cut.
- In tree mode, priority belongs only to the individual tree; once it is chopped, a replacement starts at normal priority.
- When switching modes, convert all active priorities to the new mode, preserving their priority values. Both the tree panel and Assign Priority tool must reflect the converted priorities.
- Both the tree panel and Assign Priority tool must respect the selected mode.

## Persistence and scope

- Tree priority and any displayed chopping progress must remain correct across save and load.
- Keep mod-specific source, configuration, documentation, and other project files in this folder.
- Verify implementation details and feasibility against the installed game and its current version. Do not promise a progress bar until its data is confirmed.
