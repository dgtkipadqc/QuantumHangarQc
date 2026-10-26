> **0.2.7.3 validate update:** [Download UPDATE_ONLY](binaries/QuantumHangarQc_0.2.7.3_CONFIRMATION_UPDATE_ONLY.zip) — native storage/retrieval confirmation and a lighter initial check. Replace both QH DLLs. [Installation and test status](docs/CONFIRMATION_0.2.7.3.md).

> **Download:** [Complete QH 0.2.7.2 package from main](binaries/QuantumHangarQc_0.2.7.2_COMPLET_MAIN.zip) — existing ModLoader required. [Installation and known limitations](binaries/README.md).

> **0.2.7.2 validate candidate:** fixes planet height calculation and displays **between 30 and 50 meters** explicitly. Items previously reported missing have since reappeared in the player test; the timing/cause is not established. Ship ID still changes. [Update notes](docs/TERRAIN_0.2.7.2.md).



> **VALIDATE 0.2.7.2 — configuration par sauvegarde / per-save settings.**
> [Réglages et validation / Settings and validation](docs/CONFIGURATION_PAR_SAUVEGARDE.md).
> La limite de 10 indiquée ci-dessous est désormais la valeur par défaut.

# Quantum Hangar Qc — BETA 0.2.7.3

![Quantum Hangar Qc](assets/QH_Banniere_BETA.jpg)

[Français](README.fr.md) · [Player guide](docs/PLAYER_GUIDE_EN.md) · [Installation](docs/INSTALLATION_FR_EN.md) · [Localization](docs/LOCALIZATION_FR_EN.md)

<img align="left" src="assets/QH_Icone_Sans_Lettres.jpg" alt="Quantum Hangar Qc" width="64" height="64" hspace="12">

Take your fleet on your next adventure. Quantum Hangar Qc is a community mod for
Empyrion — Galactic Survival, with a personal virtual hangar for up to **10 ships**.
Store a ship, choose a return location, and retrieve it when you need it.

<br clear="left">

## Current status

**Public BETA 0.2.7.3 — testing in progress.** Players and server administrators
are welcome to try the mod and report their feedback.
This `validate` version implements eight local catalogs and persistent per-player
`qh:lang` selection. Automatic game-language detection is **BLOCKED / unverified**.
Windows startup, fonts, cargo and native chat localization still require real-game tests.
All translations are initial drafts awaiting human review. See [validation](docs/VALIDATION.md).

## Features

- 10 personal hangar slots, linked to the player's Steam identity.
- Store / list / mark / load commands available to ordinary players.
- Archive integrity, ownership, distance, live power and occupancy checks.
- Ship export and saved files retained for restoration; cargo preservation must
  be checked in your own game/scenario during beta testing.
- Clickable stored-ship selection with a command fallback and confirmation.
- Saved hangar data survives normal server restarts.

## Commands

| Command | Purpose |
| --- | --- |
| `qh:help` | Help and operating conditions |
| `qh:list` | Hangar list; select an available ship |
| `qh:store:SHIP_ID` | Store in the first free slot |
| `qh:store:SHIP_ID:SLOT` | Store in a selected slot |
| `qh:info:SHIP_ID` | Distance, position and ownership diagnostic |
| `qh:mark` | Set a return point valid for five minutes |
| `qh:load:SLOT` | Retrieve a stored ship |
| `qh:lang` | Clickable language list; saved personal choice |
| `qh:lang:CODE` | Save a personal language choice |
| `qh:lang:auto` | Remove the choice; currently use the server default |

Use the commands without a leading slash. Before storage: power OFF, cockpit
empty, no occupants or docked ships, private ownership, within 250 m of the
ship's API position. For retrieval, mark a clear location and move **30–50 m**
away, in the same playfield, before loading. Follow the in-game confirmation.

## Install and build

This repository contains QH source, documentation and artwork. Install
[EmpyrionModHost](https://github.com/GitHub-TC/EmpyrionModHost) separately.
Game DLLs and third-party loader binaries are not redistributed here.

See [installation instructions](docs/INSTALLATION_FR_EN.md). Do not replace a
server's complete `DllNames.txt` with a QH-only list.

From the **server root**, with this repository extracted as `QuantumHangarQc`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\QuantumHangarQc\Build.ps1 -ServerRoot (Get-Location).Path
```

Build prerequisites: Windows, .NET Framework 4.7.2 targeting pack, an installed
Empyrion server, and EmpyrionModHost. Build output is `dist/Content/Mods` inside
the repository; building does not install or restart the server.

## Support development

**[❤️ Support Frank ( Dgt Kipad QC ) — optional donation](https://paypal.me/FrankOuellet)**

## Feedback and contributions

Report the QH version, game build, scenario, command, expected result and actual
result. Redact player identifiers and private paths before sharing logs.
Translation contributions are welcome in `localization/*.xml`.
See [the roadmap](docs/LOCALIZATION_FR_EN.md) before advertising language support.

## Ownership and publication

Project: Frank ( Dgt Kipad QC ) / Le Spot Studio. This is an independent community project,
not an official Eleon release. Use on your own Empyrion server, including multiplayer, is permitted.
Frank ( Dgt Kipad QC ) retains distribution of versions and updates; redistribution requires
his prior written permission. See `LICENSE.md` and `THIRD_PARTY_NOTICES.md`.
