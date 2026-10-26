# Quantum Hangar Qc 0.2.8 — reprise ciblée de 0.2.7.2

## Contenu

Base de production : `173d2e9426718ceef626e67a3b86ce71e983538b` (main 0.2.7.2), mêmes sources de production que `084ea697cda7501f0c55b7f4478c605832546e48`. Les deux DLL du paquet 0.2.7.2 de référence correspondent exactement aux empreintes de l'audit publié.

La 0.2.8 reprend les boutons natifs de confirmation de la 0.2.7.3 pour ranger et sortir un vaisseau. Le choix est explicite ; Annuler, Échap, un délai dépassé, un mauvais joueur ou une réponse périmée ne valident aucune opération. Les confirmations utilisent le même composant natif que les listes cliquables.

L'ordre des requêtes de structures reste celui de la 0.2.7.2, y compris l'actualisation avant la confirmation. L'optimisation de prévisualisation par cache de la 0.2.7.3 n'est pas reprise. La copie, l'export, la restauration, la vérification des empreintes, la quarantaine, le calcul de hauteur et les formats de sauvegarde restent ceux de la base. La distance minimale reste 30 mètres ; les contrôles de sortie admettent une tolérance numérique de 1 cm pour les arrondis des coordonnées. Le rangement conserve ses seuils exacts. Un diagnostic SOURCE_CHANGED précise désormais le fichier ajouté, absent ou dont le contenu diffère ; il ne contourne aucun refus.

## Point de sortie devant le joueur

`qh:mark` place maintenant le point à **30 mètres devant le personnage**, selon l’orientation horizontale transmise par PlayerInfo. Regarder vers le haut ou le bas ne change pas l’altitude du marqueur. Le point reste fixe si le joueur se tourne ensuite. Le vaisseau conserve son orientation enregistrée ; ce changement déplace le point, sans réorienter le vaisseau.

Regarder une zone libre, faire `qh:mark`, rester sur place puis utiliser `qh:list` et cliquer sur le vaisseau. `qh:load:SLOT` reste disponible. Aucun recul n’est nécessaire pour atteindre les 30 mètres initiaux. Si le joueur se déplace ensuite, la plage de 30 à 50 mètres autour du point reste contrôlée. Le contrôle d’obstacles et la mesure du terrain avant apparition restent actifs.

Les huit catalogues expliquent cette nouvelle utilisation. Un cap ou des coordonnées invalides refusent la création du point. Les tests couvrent les quatre directions, les diagonales, la rotation après création, la position immobile, le redémarrage et les obstacles. L’orientation exacte transmise en jeu reste à valider sur le serveur privé.

Les journaux MARK_SET, MARK_CHECK et DESTINATION_CHECK donnent aussi les coordonnées, distances et l’âge du point pour diagnostiquer un refus de sortie. Les règles restent identiques : point créé dans la session et le secteur actuels, valide cinq minutes, joueur entre 30 et 50 mètres du point. Un redémarrage efface les points temporaires ; refaire `qh:mark`. Le refus de sortie signalé reste à identifier avec son message exact.

`qh:list`, `qh:lang`, l'anglais par défaut, les langues mémorisées et les réglages par sauvegarde sont conservés. Aucun réglage supplémentaire n'est nécessaire. Version affichée : 0.2.8 ; version CLR des deux DLL : 0.2.8.0.

## Installation UPDATE ONLY

1. Arrêter le serveur, ses playfields et ModHost.
2. Sauvegarder les deux dossiers du mod et les données QH de la sauvegarde active.
3. Fusionner le dossier `Content` du ZIP dans la racine du serveur en remplaçant les fichiers fournis, puis redémarrer l'ensemble.
4. Vérifier `START 0.2.8` pour Host et `QH_STATE START 0.2.8` pour State dans les journaux.

Les deux DLL, leurs catalogues et le YAML doivent être remplacés ensemble. ModLoader doit déjà être installé et QH activé. Le paquet ne contient aucune configuration active, archive, préférence, liste de chargement ou DLL d'un autre mod.

Retour en 0.2.7.2 : arrêter l'ensemble puis réappliquer les deux modules et catalogues du paquet 0.2.7.2 conservé. Aucun changement de format n'est introduit. Ne pas supprimer ou modifier les archives de récupération.

## Validation et limites

Compilation .NET Framework 4.7.2 avec références réelles Mif/ModApi. Les tests automatisés utilisent des API simulées et de vrais fichiers locaux ; les résultats et empreintes sont dans `docs/qa/RESULTS_0.2.8.json`.

Premier essai sur le serveur privé : vaisseau de test vide, annuler un rangement, confirmer un rangement, sélectionner le slot avec `qh:list`, puis regarder une zone libre, lancer `qh:mark` et rester sur place pour sortir le vaisseau avec `qh:list`. Vérifier que le point est bien à 30 mètres devant soi. Vérifier aussi `qh:lang` et la langue après reconnexion. Une fois le cycle validé, tester quelques objets sans valeur dans un conteneur et les réservoirs.

Le démarrage Windows, le rendu des menus et le cycle réel Empyrion ne sont pas exécutés dans l'environnement de compilation. La 0.2.8 est un candidat de validation, pas une version certifiée en jeu. Elle ne prétend pas résoudre la cause des pertes de heartbeat ou des écritures Shared tardives. Le contrôle de source modifiée existait déjà en 0.2.7.2 et peut encore refuser un rangement ; son diagnostic est maintenant précis. L'ID du vaisseau change toujours à la sortie. La hauteur et la physique restent celles de 0.2.7.2.

## English

0.2.8 is rebuilt from the 0.2.7.2 source baseline and selectively adds the native storage/retrieval confirmation buttons from 0.2.7.3. The original structure-refresh sequence is retained. Storage, recovery, terrain calculations and saved-data formats are unchanged; source-change diagnostics now identify the affected file. `qh:mark` places the point 30 meters ahead using the horizontal player heading. Stay still and select your ship with `qh:list`. The point remains fixed if you turn afterwards. The 30–50 meter retrieval range has a 1 cm float-rounding tolerance; storage thresholds are unchanged.

Stop the dedicated server, playfields and ModHost; back up QH; merge the ZIP's Content folder into the server root. Update both DLLs, catalogs and State YAML together. Existing configuration, archives, preferences and loader settings are preserved. Both startup logs must report 0.2.8.

Automated tests use simulated APIs and local files. Windows/game execution is not certified. Validate an empty ship through cancel, store, clickable list and retrieve on the private test server before merging validate into main. This release does not claim to fix heartbeat loss, late engine writes, unchanged ship IDs or all terrain-height issues.
