# Current state — 0.2.7 validate — 2026-10-17

- Target branch: validate. Merge to main remains a separate action after the server test.
- Both components: assembly 0.2.7.0. Native State YAML: 0.2.7.
- Planet retrieval samples terrain under the expanded native hull bounds; configurable 5 m clearance and a second scan after confirmation.
- New archives save hull bounds/orientation before removal. Old archives without bounds remain retrievable in space; re-store with 0.2.7 for subsequent planet retrieval.
- Missing/stale/invalid terrain results refuse retrieval before allocating an entity ID. No guessed-height fallback.
- Clickable ship/language menus and persisted per-Steam language preferences retained, including compatibility with omitted native SteamId. Host session authentication still applies.
- Local simulated checks: 620 Host, 21 terrain, 175 language menus, 1316 localization, 56 configuration assertions passed.
- Real-game terrain placement, physics and cargo are not validated by those tests. Manual language selection was reported working in-game before this change.
- English remains the default for new configurations; existing server and player language settings are preserved. Automatic detection of the game's language is not implemented.
- Read [0.2.7 installation, migration and limitations](docs/TERRAIN_0.2.7.md).
