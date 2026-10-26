# Current state — 0.2.8 validate candidate — 2026-10-26

- Production baseline: main 0.2.7.2 (`173d2e9426718ceef626e67a3b86ce71e983538b`). Reference ZIP DLL hashes match published 0.2.7.2 audit.
- Native explicit confirmation from 0.2.7.3 added for storage/retrieval. Legacy informational dialogs unchanged.
- All structure refreshes follow 0.2.7.2; cache-only preview optimization excluded.
- Source comparison now logs the affected relative file and mismatch type. Copy, hash refusal, archive status, recovery and removal rules unchanged.
- Both components: 0.2.8 (CLR 0.2.8.0). Existing settings and archives retained. Update both modules together.
- Automated simulated tests and binary audit: [results](docs/qa/RESULTS_0.2.8.json).
- qh:mark now places a fixed point 30 meters ahead using horizontal PlayerInfo heading; no backward movement needed. Retrieval bounds allow 1 cm float rounding, storage thresholds unchanged.
- Marker refusal reported; exact message not yet provided. MARK_SET/MARK_CHECK/DESTINATION_CHECK diagnostics added. The new forward-point flow is simulated successfully; exact earlier marker error remains unidentified.
- Real Windows/server startup, UI and cargo restoration: NOT_RUN. Heartbeat loss and engine file-write causes remain unproven. Terrain height remains to validate; ship ID still changes.
- [Installation / rollback / test scope](docs/REBUILD_0.2.8.md).
