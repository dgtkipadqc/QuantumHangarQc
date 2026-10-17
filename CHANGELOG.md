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
