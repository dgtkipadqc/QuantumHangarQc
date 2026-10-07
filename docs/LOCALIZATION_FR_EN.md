# Localisation — proposition pour la prochaine version

Status: PLANNED / NOT IMPLEMENTED. Baseline runtime remains 0.2.5.

## Constat vérifié

L'inspection des métadonnées du Mif.dll livré avec le chargeur QH et du ModApi.dll
du dépôt EmpyrionScripting ne trouve aucun champ de langue dans PlayerInfo ni
propriété de langue dans IPlayer. IApplication et IModApi inspectés n'exposent
pas non plus de langue par joueur. Voir `API_LANGUAGE_AUDIT.json` pour les
versions, empreintes et membres inspectés. Cela ne démontre pas l'absence de
tout mécanisme interne au jeu ; cela bloque une promesse d'auto-détection avec
les interfaces actuellement vérifiées.

La langue du processus serveur, CultureInfo, le pays, l'adresse IP et le nom
du joueur ne doivent jamais être utilisés comme langue du client Empyrion.

## Architecture à implémenter

1. Extraire tous les textes visibles des commandes, confirmations, refus,
   notifications et de la liste cliquable vers des clés de catalogue stables.
   Conserver les codes internes et les commandes inchangés.
2. Une préférence linguistique par identité Steam, indépendante de l'entity ID
   et de la connexion. `cb:reset` ne doit pas effacer cette préférence.
3. Proposer `qh:lang`, `qh:lang:fr`, `qh:lang:en`, `qh:lang:auto` comme secours.
   Ces commandes ne sont PAS encore reconnues par la version 0.2.5.
4. Priorité : choix manuel explicite > langue du jeu vérifiée pour la session >
   langue par défaut du serveur > anglais pour une clé manquante.
   Une langue inconnue ne doit pas déclencher une fausse détection.
5. Pour l'automatique : rechercher une source client documentée et un transport
   de langue lié au joueur authentifié. Si un composant client devient nécessaire,
   vérifier son installation, sa compatibilité et son fonctionnement avant de
   l'annoncer. N'accepter aucun identifiant joueur fourni comme autorité par le
   client : l'identité provient de la connexion serveur. Limiter taille/fréquence
   des annonces de langue. Aucun droit de jeu ne dépend de cette annonce.
6. Transmission du code de langue dans la demande de dialogue Host -> State.
   Affichage des statuts traduit sans changer STORED/QUARANTINE dans les archives.
7. Catalogues UTF-8 locaux, aucune traduction web pendant une opération de
   vaisseau. Même ensemble de clés, placeholders validés, texte échappé pour TMP.
   Ne pas traduire les noms personnels de vaisseaux.

## Recherche complémentaire du 7 octobre 2026

Voir [LANGUAGE_RESEARCH_FR_EN.md](LANGUAGE_RESEARCH_FR_EN.md) : le chat dispose
du drapeau IsTextLocaKey, mais les fenêtres QH inspectées affichent du texte brut.
La traduction du chat par le jeu peut éviter une détection serveur pour ce canal.
Cette piste doit être testée avant de rendre un composant client nécessaire.

## Langues à couvrir

La liste exacte doit être relevée dans l'en-tête de
`Content/Extras/Localization.csv` de la version de jeu ciblée. Ce fichier n'était
pas disponible pendant cette préparation. La page Steam officielle annonce
anglais, allemand, français, russe, italien, portugais brésilien, chinois simplifié
et grec. Vérifier séparément les noms de colonnes CSV et les langues du scénario.
FR et EN seront la première paire de validation, puis les autres langues du jeu.
Une traduction proposée reste « à relire » jusqu'à validation humaine et en jeu,
notamment pour les polices, accents et caractères non latins.

## Critères d'acceptation

- Deux joueurs simultanés avec deux langues différentes, sans mélange.
- Changement de langue, reconnexion, redémarrage et reset du personnage.
- Absence/échec de détection explicite et secours compréhensible.
- Aide, mark, info, list, store, load, annulation et tous les refus traduits.
- Clé manquante et pack invalide : message anglais sûr, sans planter l'opération.
- Les noms de vaisseaux contenant `<`, `>` ou des accents restent du texte.
- Aucune modification des contrôles de propriété/distance ni de l'archivage.
- Vérifier la vraie langue Empyrion, et non la langue Windows/Steam supposée.
- Essai Windows en jeu obligatoire avant le statut « automatique validé ».

## English

The inspected server-facing interfaces do not expose per-player game language.
Automatic detection is an unresolved integration requirement, not a feature of
this preparation. A local catalog system and persistent per-player override are
the proposed fallback. Exact supported game languages must be read from the
target game's Localization.csv header. The planned `qh:lang` commands are not
implemented. No runtime binaries were changed for this roadmap.
