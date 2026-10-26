# Current state — 0.2.7.3 validate candidate — 2026-10-26

- Both components and State YAML: 0.2.7.3. Target branch: validate; real-game confirmation test pending.
- Storage and retrieval confirmations use the same native UI bridge as clickable ship/language menus, with explicit confirmation and cancellation.
- Known-ship preview avoids one expensive structure refresh. Fresh live state and all final checks remain required.
- Confirmation failures report error.confirmation before export/removal. Both DLLs must be updated together.
- 729 Host, 1332 localization, 175 language-menu and 99 confirmation assertions pass against simulated APIs and local files. Production assemblies compiled against real game and Microsoft net472 references.
- Windows/server startup and in-game UI rendering: NOT_RUN.
- Terrain height remains a known issue requiring real-game diagnosis. Ship ID still changes on retrieval. Cargo/tank copy code unchanged.
- Existing saved language choices and per-save settings retained.
- [Installation and validation](docs/CONFIRMATION_0.2.7.3.md).
