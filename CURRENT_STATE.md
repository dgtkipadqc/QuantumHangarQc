# Current state — 0.2.7.1 validate candidate — 2026-10-18

- Target branch: validate. Empty test ships only; do not merge into main while container loss is unresolved.
- Both components and State YAML: 0.2.7.1.
- Retrieval requires 30–50 m from qh:mark, including endpoints; rechecked after confirmation.
- Default ground clearance is 0.5 m (allowed 0.25–50). Explicit existing configuration is preserved.
- New hull captures use half-block edges; old archived geometry is unchanged. Physics may still settle or tip an unpowered ship.
- Container item loss was reported on 0.2.7 and is NOT fixed here. Oxygen/fuel/pentaxid tanks were reported preserved in the player's test. Actual game cargo is not validated by simulated tests.
- File-copy method comparison across older ZIPs found no removed copy mechanism; the precise loss stage and recoverability require the affected archive and log.
- Ship ID still changes on retrieval. No cargo replay or ID reuse is implemented.
- Clickable lists, persistent language preferences and English default remain. Automatic game-language detection is not implemented.
- Local checks: 688 Host, 24 terrain, 175 menus, 1316 localization and 56 configuration assertions. See QA results for runner details and limitations.
- [Installation and empty-ship test](docs/TERRAIN_0.2.7.1.md).
