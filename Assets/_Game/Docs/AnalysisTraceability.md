# Clean-Room Analysis Traceability

## High-Level Concepts Used

- Data-driven characters, skills, formations and gacha banners.
- Deterministic, position-driven 5v5 automated team battle.
- Separate simulation, presentation, persistence and UI layers.
- Shared character view and attachment-point contract.

## Independent Implementation

- All code, names, roles, stats, formulas, probabilities, UI and placeholder visuals are newly authored for this project.
- Characters use original Unity-primitive geometry and generic color schemes.
- The demo uses local mock services with interfaces designed for future backend replacement.
- AI target locking, fixed-tick movement, attack range and target-death retargeting are independent BubbleMind rules; the read-only analysis supplied only high-level system boundaries and no recoverable PvP numbers.

## Intentionally Not Copied

- No proprietary characters, branding, assets, audio, UI artwork, animation, source code, Lua, configuration, numeric balance, endpoint or protocol is used.
- No encrypted material is decrypted and no discovered server is contacted.

## Deferred

- Production backend, authentication, IAP, remote content, PvP, live operations and final character art.
