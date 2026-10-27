# Current state — 0.2.8.2 validate candidate — 2026-10-27

- Baseline: validate 5010506796c858b671723fa7ab92125fd02cb0d4.
- All QH global structure Update calls removed. Fresh native sector inventory now replaces current sector in the cached global list, with no fallback if native observation fails.
- Both modules 0.2.8.2 required; State directly under Content/Mods.
- Same storage/cargo-copy/terrain/owner safeguards, language persistence and quarantine handling. No new operation cooldown or archive format.
- [Installation and private-server test](docs/COMPLETE_0.2.8.2.md); [automated evidence](docs/qa/RESULTS_0.2.8.2.json).
- Windows/server execution and actual cargo serialization NOT_RUN. The suspect call is removed; engine root cause and real heartbeat stability remain unconfirmed. Main unchanged.
