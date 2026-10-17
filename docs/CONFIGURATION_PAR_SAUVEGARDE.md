# Configuration par sauvegarde — 0.2.6.1 VALIDATE

Le chargeur démarre le Host avec ses dépendances incluses. QH demande ensuite
au client de CE Host le chemin de la partie active (`GetPathFor(SaveGame)`).
Aucun nom de sauvegarde à saisir et aucune dépendance à AtlantisPlayerCommands.
Si le client ne répond pas ou retourne un chemin invalide, QH se désactive :
il ne sélectionne jamais une autre sauvegarde au hasard.

Au premier démarrage, QH crée dans la partie active :
`Mods/QuantumHangarQc/Configuration.xml`.
Les valeurs initiales viennent du fichier du même nom dans le dossier du mod.
Une configuration déjà créée pour une partie reste prioritaire et est conservée.

```xml
<Settings>
  <MaxShipsPerPlayer>10</MaxShipsPerPlayer>
  <DefaultLanguage>en</DefaultLanguage>
</Settings>
```

- `MaxShipsPerPlayer` : 1 à 15. Mettre 5 pour limiter les nouveaux dépôts à
  cinq vaisseaux par joueur. Les réservations et quarantaines comptent aussi.
- Défaut du paquet : anglais (`en`). Les réglages existants ne sont pas écrasés.
- `DefaultLanguage` : fr, en, de, el, it, pt-BR, ru ou zh-Hans. Le choix
  personnel enregistré avec `qh:lang` reste prioritaire.
- Arrêter le serveur, modifier le fichier de la partie, puis redémarrer.
- Réduire 10 à 5 ne supprime aucune archive. Les anciennes places jusqu'à 15
  restent accessibles pour restitution; aucun nouveau dépôt au-delà du quota.
- Chaque sauvegarde a ses réglages. Utiliser une installation de serveur et de
  ModLoader distincte par serveur simultané (configuration du chargeur incluse).
- Les dépendances du chargeur sont fournies dans le ZIP. Ce n'est pas un
  installateur automatique de Windows, du jeu ou de .NET Framework 4.8.

Installer ensemble les deux DLL QH 0.2.6.1 et les catalogues mis à jour dans
les deux dossiers : le contrat des messages affiche maintenant la limite réelle.
Le paquet complet vise une installation neuve. Sur un ModLoader existant,
ne pas remplacer les listes de modules ni la configuration du chargeur.

## Validation

Compilation Host/State avec les références Microsoft .NET Framework 4.7.2.
Tests locaux : 528 assertions Host, 1226 localisation, 53 configuration/protocole.
Le protocole utilise les classes du chargeur livré; le client de test simule
sa réponse, pas le transport Windows ni le moteur Empyrion.
Les tests State/GUI complets n'ont pas été relancés dans cet environnement.
Le lancement Windows et les essais en jeu restent à faire sur le serveur test.

## English

QH asks the client connected to its own Host for the active save path. It never
selects a save by name, modification time or another mod's configuration.
Each save gets `Mods/QuantumHangarQc/Configuration.xml` on its first start.
Set `MaxShipsPerPlayer` to 5 or another value from 1 to 15, then restart.
Defaults are copied from the mod's configuration only when that save has no
configuration yet. Existing archives remain accessible after lowering the quota.
Install both updated QH DLLs and both language folders together. Use separate
server/loader installations for concurrent servers. Bundled dependencies are
included; Windows, Empyrion and .NET installation are not automated.
Windows startup and real game tests remain pending.

## Choix personnel / Player choice

Dans le chat, `qh:lang:fr` (ou `qh:lang fr`) choisit le français ;
`qh:lang:en` choisit l’anglais. `qh:lang` affiche le choix effectif.
Le choix est enregistré par SteamID dans la sauvegarde active :
`Mods/QuantumHangarQc/Preferences/<SteamID>.xml`. Il reste prioritaire
après reconnexion et redémarrage ; un autre joueur garde son propre choix.
`qh:lang:auto` efface ce choix et reprend le défaut de la sauvegarde.
Changer la langue du jeu ne change pas automatiquement celle du mod.

The package defaults to English. Use `qh:lang:fr` or `qh:lang fr` to
save French for your SteamID in this save; reconnecting or restarting
does not reset the choice. Existing save settings and preferences are preserved.
