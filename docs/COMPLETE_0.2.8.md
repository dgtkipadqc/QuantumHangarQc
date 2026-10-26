# Quantum Hangar Qc 0.2.8 — paquet complet QH

Ce paquet réunit tous les fichiers du mod QH 0.2.8 : les deux DLL, les huit langues de chaque module, le YAML State, les exemples de configuration et de déclaration du mod, la licence, les notes et les empreintes SHA-256. Les DLL sont exactement celles du candidat 0.2.8 publié sur validate, commit 25c00750639d26fc8eb14056b5cbfdb15a446027.

Prérequis : serveur Empyrion avec EmpyrionModHost / ModLoader déjà installé et fonctionnel. Le chargeur et ses dépendances ne sont pas inclus dans ce paquet QH.

## Installation sur ton serveur

1. Arrêter Empyrion, les playfields et ModHost. Sauvegarder les dossiers QH et les données QH de la partie active.
2. Extraire le ZIP dans un dossier temporaire.
3. Fusionner son dossier Content avec Content à la racine du serveur. Remplacer les fichiers QH fournis ; conserver les autres fichiers.
4. Pour une première installation de QH seulement : ajouter la ligne de Exemples/DllNames_QH_line.txt à Content/Mods/ModLoader/Host/DllNames.txt si elle n'y figure pas déjà. Ne pas remplacer la liste complète.
5. Redémarrer l'ensemble et vérifier START 0.2.8 pour Host et QH_STATE START 0.2.8 dans les journaux de playfield.

Les deux modules doivent être installés ensemble :

- Content/Mods/ModLoader/MODs/QuantumHangarQc/
- Content/Mods/QuantumHangarQcState/

## Réglages par partie

QH détecte la sauvegarde active et crée sa configuration au premier démarrage fonctionnel dans Saves/Games/<partie>/Mods/QuantumHangarQc/Configuration.xml. Aucun nom de sauvegarde n'est à choisir dans ce paquet.

Valeurs par défaut : 10 vaisseaux, anglais, marge au sol de 0,5 m. Pour limiter à 5 vaisseaux, arrêter le serveur, régler MaxShipsPerPlayer sur 5 dans la configuration de la partie, puis redémarrer. Limites acceptées : 1 à 15 vaisseaux ; marge au sol de 0,25 à 50 m. Les archives existantes restent accessibles si le quota est réduit.

Exemples/Configuration.example.xml est une référence, pas un remplacement automatique de tes réglages. Le ZIP n'inclut aucune configuration active, liste de chargement active, archive ou préférence personnelle. Les langues choisies avec qh:lang restent mémorisées par joueur et par sauvegarde. L'anglais est le défaut d'une nouvelle installation ; changer la langue du jeu ne change pas automatiquement celle de QH.

## Utilisation de la 0.2.8

- qh:store:ID : ranger un vaisseau après confirmation cliquable.
- qh:mark : placer un point fixe à 30 mètres devant le personnage, à l'horizontale.
- Rester sur place, puis qh:list : sélectionner le vaisseau et confirmer sa sortie.
- qh:load:SLOT reste disponible. Le point expire après cinq minutes ou un redémarrage ; il est lié à la session et au secteur.
- qh:lang : sélectionner la langue dans la liste.

L'orientation sauvegardée du vaisseau est conservée. Les contrôles de terrain, d'obstacles et de distance restent actifs. Si tu te déplaces après qh:mark, reste entre 30 et 50 mètres du point.

## Validation et retour arrière

Reconditionnement des mêmes DLL 0.2.8, sans nouvelle modification du code. 2 523 vérifications automatisées simulées ont passé pour cette version. L'intégrité de ce ZIP, les empreintes et l'identité des DLL avec UPDATE ONLY sont vérifiées. L'installation et le fonctionnement réels sous Windows/Empyrion restent à valider sur le serveur privé avant fusion vers main.

Tester d'abord un rangement annulé puis un cycle rangement/sortie avec un vaisseau vide. L'ancienne erreur mark n'est pas identifiée sans son message exact ; la hauteur, le cargo réel et les pertes de heartbeat ne sont pas certifiés corrigés. L'ID du vaisseau change toujours à la restitution. Les copies de récupération sont conservées.

Pour revenir en 0.2.7.2 : arrêter serveur, playfields et Host, remettre ensemble les deux modules et leurs catalogues 0.2.7.2 conservés, puis redémarrer. Ne pas effacer les archives ou modifier leurs manifestes. Aucun nouveau format de sauvegarde n'est introduit en 0.2.8.

## English

Complete QH 0.2.8 runtime package. Requires an existing working EmpyrionModHost/ModLoader installation; loader dependencies are not bundled. The two DLLs are byte-identical to the 0.2.8 UPDATE ONLY candidate on validate.

Stop the server, playfields and ModHost, back up QH, then merge the supplied Content folder into the server root. Install both modules together. For a first QH installation, append the example DLL line to the existing Host list only if missing. Existing settings, archives, preferences and other mods are preserved. Per-save configuration is created automatically; default values are 10 ships, English and 0.5 m ground clearance.

Face a clear area and run qh:mark: the fixed point is placed 30 meters ahead on the horizontal heading. Stay still, use qh:list, select the ship and confirm. qh:lang opens the language selector and saves the choice. The marker expires after five minutes or a restart.

2,523 simulated assertions passed; actual Windows/game execution remains pending. Validate an empty test ship on the private server before merging into main. See Docs/REBUILD_0.2.8.md and Docs/RESULTS_0.2.8.json for scope and limitations.
