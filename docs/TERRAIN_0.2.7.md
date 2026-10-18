# QH 0.2.7 — validate

La restitution sur une planète mesure désormais le terrain sous l'emprise du vaisseau et place le bas de sa coque au-dessus du relief mesuré, avec une marge de 5 mètres par défaut. La mesure se refait après confirmation. Les listes cliquables et le choix de langue mémorisé sont conservés.

## Installation

Arrêter le serveur, ses playfields et ModHost. Sauvegarder les deux dossiers QH, puis fusionner le dossier `Content` du ZIP UPDATE ONLY avec la racine du serveur. Remplacer les deux DLL, leurs catalogues Languages et le YAML State. Redémarrer l'ensemble.

- Host : `Content/Mods/ModLoader/MODs/QuantumHangarQc/QuantumHangarQc.dll`
- State natif : `Content/Mods/QuantumHangarQcState/QuantumHangarQcState.dll`
- Les deux assemblages portent la version **0.2.7.0** ; le YAML porte **0.2.7**.

Aucune configuration existante, liste de chargement ni archive n'est incluse dans le correctif. Ne pas effacer les dossiers existants. Le chargeur reste un prérequis : ce paquet met à jour une installation QH existante.

## Réglage par sauvegarde

Dans `Saves/Games/<partie active>/Mods/QuantumHangarQc/Configuration.xml`, ajouter ou ajuster, sous `<Settings>` :

```xml
<SpawnClearanceMeters>5</SpawnClearanceMeters>
```

Valeurs acceptées : de 1 à 50 mètres. Une ancienne configuration sans ce champ utilise 5. Conserver les autres réglages. Redémarrer le Host après modification. Le quota de vaisseaux et les préférences de langue restent indépendants.

## Anciennes archives

Une archive créée avant 0.2.7 ne contient pas les dimensions de la coque. Sa restitution sur une planète est refusée avant toute création d'entité, avec un message explicite. Elle reste intacte et récupérable **dans l'espace** ; la redéposer avec les deux modules 0.2.7 enregistre ses dimensions pour les prochaines restitutions sur planète. Ne pas modifier le manifeste à la main et ne pas effacer le hangar.

## Contrôles et limites

Le State relève les huit coins des limites natives, élargies d'un bloc sur chaque axe, et les transforme avec `IStructure.StructToGlobalPos`. Les dimensions sont relatives à la position native de l'entité. L'orientation native est convertie de quaternion en angles Euler pour la restitution. Le manifeste enregistre cette géométrie avant le retrait.

Sur une planète, `IPlayfield.GetTerrainHeightAt(x,z)` mesure une grille couvrant cette emprise, avec un espacement maximal de 2 mètres. Le point le plus haut détermine la hauteur de sortie, compte tenu du bas de coque et de la marge. Le calcul ne descend jamais sous la hauteur du marqueur. Dans l'espace, la position reste celle du marqueur et aucune mesure du terrain n'est faite.

La mesure est répartie sur les mises à jour du playfield : au plus 256 points par passage, 16 384 points au total. Une emprise trop grande, un type de secteur inconnu, un capteur absent, un déchargement de secteur ou une réponse invalide/périmée bloque la restitution ; l'archive reste STORED et aucun ID de sortie n'est alloué. Une emprise trop grande reste récupérable dans l'espace. Les contrôles de session, propriétaire, autres joueurs, obstacles proches et non-duplication demeurent actifs. La position corrigée est enregistrée avant l'apparition.

Il s'agit d'un contrôle de relief échantillonné, pas d'une détection complète des collisions du moteur : un pic entre deux points, une modification ultérieure du terrain, un bâtiment, arbre, rocher, volume d'eau ou objet non représenté par cette API ne sont pas garantis dégagés. Choisir une zone ouverte. Le comportement physique après apparition doit être testé en jeu, notamment pour les vaisseaux hors tension.

## Validation locale et essai en jeu

- 620 assertions Host : conservation des archives, garde au sol transmise à la demande d'apparition, nouvelle mesure après confirmation, session changée, capteur absent, refus sans création d'ID et protections existantes.
- 21 assertions terrain : pente, sommet au centre, marqueur en hauteur, espace, anciennes archives, valeurs invalides, taille excessive, budget par mise à jour, quaternion/Euler et déchargement du secteur.
- 175 assertions des menus ; 1 316 de localisation ; 56 de configuration.
- Compilation des deux DLL contre .NET Framework 4.7.2 et les références réelles Mif/ModApi. Les tests utilisent des API simulées. Les petits types natifs de `tests/TerrainTests.cs` ne sont jamais inclus dans les DLL livrées.
- Le choix de langue a été signalé fonctionnel en jeu avant cette mise à jour. Le nouveau placement n'a pas été exécuté sur un serveur Empyrion ici.

Avant fusion dans main : déposer un vaisseau test avec 0.2.7, placer un marqueur sur une pente libre, s'éloigner de 50 à 1 000 mètres puis le sélectionner dans `qh:list`. Vérifier la coque, les coffres et le comportement après apparition. Refaire avec un HV/SV puis un CV, et confirmer que le choix de langue reste mémorisé après reconnexion. Examiner `TERRAIN_CHECK` et `QH_TERRAIN` en cas de refus. Ne pas retenter automatiquement une transaction QUARANTINE.

## English

Update both QH modules together. Planet retrieval now samples terrain across the saved hull footprint and applies `SpawnClearanceMeters` (default 5, range 1–50), with another scan after confirmation. Space positions are unchanged. Old archives without hull dimensions must first be retrieved in space and stored again with 0.2.7. Failed terrain checks preserve the stored archive before allocating a new entity ID. Sampling is not full engine collision detection. Local simulated tests passed; test terrain placement and cargo in-game on validate before merging to main.
