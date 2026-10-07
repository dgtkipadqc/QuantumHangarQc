# Quantum Hangar Qc — BETA 0.2.5

![Quantum Hangar Qc](assets/QH_Banniere_BETA.jpg)

[Français](README.fr.md) · [Player guide](docs/PLAYER_GUIDE_EN.md) · [Installation](docs/INSTALLATION_FR_EN.md) · [Localization plan](docs/LOCALIZATION_FR_EN.md)

Take your fleet on your next adventure. Quantum Hangar Qc is a community mod for
Empyrion — Galactic Survival, with a personal virtual hangar for up to **10 ships**.
Store a ship, choose a return location, and retrieve it when you need it.

## Current status

**Public BETA 0.2.5 — testing in progress.** Players and server administrators
are welcome to try the mod and report their feedback.
The game UI is currently mainly French with some bilingual dialogs. Automatic
language detection and `qh:lang` are **planned, not implemented in this version**.
The Windows startup fix was checked locally; this package does not certify a new
live-server test. See [validation](docs/VALIDATION.md).

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

Use the commands without a leading slash. Before storage: power OFF, cockpit
empty, no occupants or docked ships, private ownership, within 250 m of the
ship's API position. For retrieval, mark a clear location and move **50–1000 m**
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
Translation contributions are welcome once the message catalog is integrated.
See [the roadmap](docs/LOCALIZATION_FR_EN.md) before advertising language support.

## Ownership and publication

Project: Frank ( Dgt Kipad QC ) / Le Spot Studio. This is an independent community project,
not an official Eleon release. Use on your own Empyrion server, including multiplayer, is permitted.
Frank ( Dgt Kipad QC ) retains distribution of versions and updates; redistribution requires
his prior written permission. See `LICENSE.md` and `THIRD_PARTY_NOTICES.md`.
