# Current state — 0.2.8.1 validate candidate — 2026-10-26

- Based on validate d9dc2f7dd5b306eb9d44f384559dc43129caf641 (0.2.8).
- Native post-spawn validation replaces comparison with global-list counts; bounded readiness polling.
- qh:list conservatively reconciles at most one existing spawned quarantine per call; exact ID, sector, current owner, native counts, archive hashes and lineage checked. Original archive and manifest retained in Retired; no live ship writes.
- Both modules version 0.2.8.1. No new operation cooldown, no save format change.
- [Install / rollback](docs/COMPLETE_0.2.8.1.md) and [simulated test evidence](docs/qa/RESULTS_0.2.8.1.json).
- Actual Windows/game execution remains NOT_RUN. Container contents are not automatically verified; CargoVerified stays false.
- Forward marker, settings, UI, language persistence, archive copying and terrain unchanged. Ship ID still changes on restore. Main not modified.
