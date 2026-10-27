# 0.2.8.2 — validate candidate

- Remove QH Request_GlobalStructure_Update sends. Merge a fresh complete native sector snapshot into the cached global list for safety checks and presence polling.
- Correlate protocol, token, player, Steam, client, sector and timestamp; reject partial/invalid/expired snapshots with no blocking fallback.
- Bound native capture and process one sector request per State update. Both modules must be updated.
- Preserve storage/restore protections and 0.2.8.1 quarantine handling. No new cooldown.
- Prevent an answered UI request from reopening while the Host consumes its reply and cleans exchange files.
- Add Host and native-sector simulated regressions; actual server heartbeat/cargo validation pending.

# 0.2.8.1 — validate candidate

- Compare native structure counts at both storage and restore; require two consecutive ready snapshots with bounded polling.
- Reconcile one already-spawned quarantine per qh:list in the current sector after identity, lineage, archive hash and native checks. Preserve the entire archive in Retired; never replay spawn or alter the live ship.
- Log expected/actual restore counters. No cooldown added. Cargo contents remain outside automated verification.
- Complete QH package and regression coverage; actual server testing pending.

# 0.2.8 VALIDATE candidate — 2026-10-26

- Rebuilt from the 0.2.7.2 production baseline; native store/retrieve confirmation selectively carried over from 0.2.7.3.
- Restores the 0.2.7.2 fresh preview and subsequent structure-refresh order; no cache-only preview optimization.
- Source-change refusals identify affected files in local logs without weakening hash checks or changing recovery/copy behavior.
- qh:mark now creates a fixed point 30 meters ahead on the horizontal player heading. Player can retrieve while stationary. Terrain/obstacle checks retained; retrieval-only 1 cm coordinate-rounding tolerance. Eight catalogs updated.
- Marker diagnostics record point, current distance, age and session match; reported real-game marker error remains unidentified.
- Clickable ship/language lists, language persistence, settings, terrain placement and archive format retained.
- Compiled against real game and net472 references; simulated regression results in docs/qa/RESULTS_0.2.8.json. Windows/game testing still pending on validate.

# 0.2.7.3 VALIDATE candidate — 2026-10-26

- Storage/retrieval confirmations use the native dialog bridge already used by clickable lists, avoiding the legacy dialog request that timed out in the inspected attempts.
- Explicit buttons only: Escape cancels; Enter, links, stale/foreign callbacks and replayed replies cannot confirm.
- Skip the usual pre-confirmation structure refresh for a known ship in the same sector; live position/state checks remain. Mandatory post-confirmation, pre-removal and absence refreshes remain.
- Add a translated confirmation-specific failure message and correlation logs. Update both QH components together.
- Local regressions pass; real-server validation is still required. Cargo, terrain and ship-ID behavior unchanged.
- [Update instructions and evidence](docs/CONFIRMATION_0.2.7.3.md).

# 0.2.7.2 VALIDATE candidate — 2026-10-18

- English marker/store/help messages spell out "between 30 and 50 meters"; other catalogs replace the compact dash range with words.
- Planet spawn Y now equals sampled highest terrain minus the signed lower hull offset plus configured clearance. Marker altitude no longer raises the ship; positive lower hull offsets are handled.
- Host accepts a planet position below marker altitude only with the new placement protocol and consistent ground/hull/clearance values. Old State planet replies are refused; update both modules.
- Logs include marker, ground, lower hull, clearance and protocol version to diagnose remaining game-specific placement issues.
- User reports container items have reappeared; tank contents were also preserved. Cause/timing unconfirmed; no cargo logic changed. Ship IDs still change.
- Simulated tests pass; real-game height needs confirmation on validate before main.

# 0.2.7.1 VALIDATE candidate — 2026-10-18

- Retrieval now requires the requesting player to be 30–50 m from qh:mark, checked again after confirmation. Spawn proximity checks use a 30 m minimum.
- Default SpawnClearanceMeters reduced from 5 to 0.5 m; accepts 0.25–50 m. Explicit existing settings are preserved.
- Newly captured hull bounds include half-block edges instead of a whole-block expansion. Previously archived bounds remain unchanged.
- Container item loss reported on 0.2.7 remains unresolved. Tank oxygen/fuel/pentaxid were reported preserved in the same player test.
- Empty test ships only. This candidate is not ready to merge into main. Ship ID still changes on retrieval.
- See [installation and validation](docs/TERRAIN_0.2.7.1.md).

# 0.2.7 VALIDATE — 2026-10-17

- Measures terrain under the expanded hull footprint before retrieval and again after confirmation.
- Stores native hull bounds and orientation; adds per-save SpawnClearanceMeters (default 5, range 1–50).
- Space positions remain unchanged. Legacy archives without hull dimensions must first be retrieved and re-stored in space.
- Missing, invalid or expired terrain replies refuse retrieval before allocating an entity ID; stored archive retained.
- Preserves clickable ship/language menus and persistent language preferences.
- Local simulated tests pass; terrain placement needs testing in the game before merging to main.

> **0.2.6.2 VALIDATE : `qh:lang` ouvre la selection au clic, comme `qh:list`.**
> [Installation et tests](docs/LANGUAGE_MENU_0.2.6.2.md). Host + State a installer ensemble.

## Validate — 2026-10-17 — langues

- Le modèle de configuration utilise `en`, conformément au défaut du code.
- `qh:lang fr` accepté en plus de `qh:lang:fr` ; sélection par SteamID inchangée.
- Non-régression : choix FR après reconnexion/redémarrage, isolation entre joueurs,
  retour à EN et ancienne configuration sans langue. Aucun écrasement des données.
- Corrige l'inclusion de `ActiveSavePath.cs` dans le vérificateur historique.
- Ajoute `tools/test_player_languages.py` pour rejouer les contrôles ciblés.
- Tests locaux PASS ; essai Windows/jeu non effectué. Pas de publication main.

# 0.2.6.1 VALIDATE — 2026-10-17

- Détection de la sauvegarde active via le client connecté au Host.
- Configuration par partie, quota de 1 à 15 et langue par défaut.
- Conservation des archives lors de la réduction du quota.
- Affichage de la limite réelle dans les huit langues.
- Validation locale; démarrage Windows et essais en jeu à effectuer.

# Changes

## BETA 0.2.6 — validate, 2026-10-08

- Eight UTF-8 catalogs and persistent per-player qh:lang commands.
- Shared validated resolver and embedded English fallback in Host/State.
- Localized dialogs, selection list, notifications and semantic error messages.
- Language selection during a confirmation changes subsequent messages without replacing the pending dialog.
- Versioned language field in selection bridge; old requests use English.
- .NET Framework 4.7.2 compilation and Queue<T> reference checks, executable regression/localization tests.
- Automatic game-language detection remains blocked; native chat laboratory helper is excluded from production.
- Ship capacity, archive schema, marker timing/geometry and ship protections preserved.
- All translations await review and game layout checks; no live-game PASS claimed.

## BETA 0.2.5 — historical baseline

See v0.2.5-beta and the preserved historical evidence in documentation.
