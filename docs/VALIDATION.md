# Validation BETA 0.2.6 — 2026-10-08

Base: main `623c0bc2156cd957ee514193c71d248a9fb936e8`. Target: validate.
Current evidence: [local results](qa/LOCAL_RESULTS_0.2.6.json),
[binary audit](qa/BINARY_AUDIT_0.2.6.json), typerefs/assemblyrefs and message inventory.

Compilation used Mono mcs 6.8 with `-noconfig -nostdlib` and genuine Microsoft
.NET Framework 4.7.2 targeting references (NuGet net472 1.0.3). Host Mif and State
Mif/ModApi/Unity assemblies came from the upstream projects, with hashes recorded.
None are stubs or redistributed. Host Queue<T> resolves to System.dll; State has
no Queue<T> reference. Windows compiler execution and game startup NOT_RUN.

Executed: 528 Host regression assertions (Mono), 34 State and 21 selection
assertions (mocked game API on .NET 8, needed for the external Unity netstandard
2.1 reference), 1226 localization assertions and 10006 fallback/cache assertions
(Mono). The last count includes 10000 repeated renders after deleting a catalog;
it is a cache smoke check, not a representative game performance benchmark.
No game renderer, cargo engine, Windows server or multiplayer session was run.

Initial local attempts exposed missing test runtime protobuf-net and Unity
netstandard facades; both were addressed in the test harness. A help assertion
was updated from 0.2.5 to 0.2.6. The isolated native chat helper required qualified
Eleon enum names and was recompiled successfully. Production DLLs are the ones
from the final successful local build; no repeated CI was triggered.

## Master test matrix

PASS is limited to the method/evidence in this table. Game column is a separate
result. No PASS local is promoted to PASS in game. Version: 0.2.6 for every row.

| Test | Local status / method / evidence | Real game |
|---|---|---|
| T01 catalogs | PASS — LocaleTests: 8 × 73 keys, strict UTF-8, parameters and rendering | NOT_RUN |
| T02 player text inventory | PASS — static inventory: semantic catalog calls and 107 tagged throw sites; diagnostic internals excluded | NOT_RUN |
| T03 choices/aliases/unknown | PASS — LocaleTests runs eight actual command choices, normalization and unknown preservation | NOT_RUN |
| T04 precedence | PASS — manual > server > English unit checks; verified-auto stage BLOCKED (no source) | BLOCKED for auto |
| T05 auto clears manual | PASS — command test removes override, reports unavailable detection | NOT_RUN |
| T06 two FR/EN players | PASS — simultaneous State selection bodies/buttons; sequential Host language isolation checks | NOT_RUN simultaneous full game |
| T07 persistence/reset | PASS — fresh Preferences instance, Steam-linked identity across simulated character reset | NOT_RUN reconnect/server/cb:reset |
| T08 entity/session reuse | PASS — another Steam with reused entity gets default; existing session/click guards regression | NOT_RUN |
| T09 language during dialog | PASS — LocaleTests: old confirmation sequence retained, exactly one export, next help in new language | NOT_RUN |
| T10 corrupt/missing/format | PASS — FallbackTests + LocaleTests: invalid catalog/key/args and corrupt preferences | NOT_RUN |
| T11 rich-text names | PASS — accents, newline, brackets and <link> are text; selection links are code-generated | NOT_RUN renderer |
| T12 Host/State correlation | PASS — UI and Host regression token/session/parallel-player tests; protocol 2 in source | NOT_RUN real IPC |
| T13 store/load cargo | PASS for mocked byte files and archive invariants; engine cargo comparison not simulated | NOT_RUN |
| T14 ship refusals | PASS — Host/State regression ownership/power/range/occupants/docking/playfield | NOT_RUN |
| T15 quarantine/cancel/expiry/click | PASS — existing regression guards and file preservation | NOT_RUN |
| T16 offline/no client | PASS — local resolver/cache/source inspection; no network path or client plugin dependency | NOT_RUN disconnected game |
| T17 framework/startup | PASS — both assemblies compiled with net472 references and inspected | NOT_RUN Windows startup |
| T18 update package | PASS — package inspection: two root folders, two QH DLLs, eight catalogs each, no loader/config/save data | NOT_RUN install |
| T19 update/rollback | NOT_RUN — bilingual procedure supplied | NOT_RUN |
| T20 native chat | PASS compilation only — laboratory helper; behavior BLOCKED by absent scenario data/clients | BLOCKED |
| T21 channels separately | PASS simulated confirmations/notifications/list; native chat separate and unverified | NOT_RUN |
| T22 fonts/layout/eight languages | PASS string rendering only; all human translations NOT_REVIEWED | NOT_RUN |
| T23 actual game setting | BLOCKED — no verified language source, no game clients | BLOCKED |
| T24 client announcement | NOT_RUN — no client bridge shipped; not applicable to manual mode | NOT_RUN |
| T25 load/cache | PASS — 10000 renders after deleting a file; no translation I/O per tick/network in source | NOT_RUN game performance |

Manual multilingual beta: local checks PASS, ready for server testing.
Automatic chat/full automatic: BLOCKED / not delivered as a production feature.
Complete master acceptance: **NOT COMPLETE**, pending the essential game checks,
automatic evidence and human translation review. Clickable language picker not delivered.

## Test court pour Frank / Short test procedure

1. Serveur + playfields + ModHost arrêtés : sauvegarder les deux composants QH et
   la sauvegarde test. Fusionner les deux dossiers du ZIP dans Content/Mods, puis
   redémarrer. Relever QH 0.2.6 dans les deux journaux et la version du jeu/scénario.
2. Deux joueurs : `qh:lang:fr` et `qh:lang:en`, puis chacun `qh:help`, `qh:info:ID`
   et `qh:list`. Vérifier fenêtres, boutons, refus et notifications sans mélange.
3. `qh:lang`, langue inconnue, `qh:lang:auto`, reconnexion et redémarrage : vérifier
   origine et persistance. Tester cb:reset uniquement sur personnage de test.
4. Vaisseau test avec inventaire photographié : dépôt, confirmation/annulation,
   sortie et comparaison de tous les coffres, réservoirs et appareils. Tester
   puissance, cockpit, passagers, arrimage, propriété, distance et secteur.
5. Pendant une confirmation, `qh:lang:de` : fenêtre intacte, un seul effet au clic,
   dialogue suivant en allemand. Répéter les huit langues et vérifier les polices,
   retours ligne et boutons. Une chaîne illisible est FAIL, même si le catalogue charge.
6. Retour arrière sur sauvegarde test suivant INSTALLATION_FR_EN.md ; vérifier
   archives et autres mods. Conserver les listes de chargement existantes.

Report PASS/FAIL with command, QH/game/scenario version, client language,
screenshot and redacted logs. Local evidence does not replace these tests.

## Historical reference

The 0.2.5 API audit, STRING_REVIEW_INVENTORY.json and REPOSITORY_CHECKS.json remain
historical records, not a new proof of this delivery. The 0.2.6 counts above were
actually rerun. No merge, production installation or release was performed.
