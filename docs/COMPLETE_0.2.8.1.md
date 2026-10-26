# Quantum Hangar Qc 0.2.8.1 — paquet complet QH / complete QH package

Candidat sur validate, basé sur le paquet 0.2.8. Installer les deux modules ensemble. EmpyrionModHost / ModLoader déjà installé et fonctionnel requis : le chargeur et ses dépendances ne sont pas inclus.

## Correctif de quarantaine

Le rangement conserve les compteurs natifs du vaisseau. La sortie comparait ces valeurs aux compteurs de la liste globale dès la première apparition. Cette comparaison pouvait refuser une structure déjà présente. La 0.2.8.1 compare maintenant les compteurs natifs des deux côtés et exige deux observations consécutives d'un vaisseau prêt, du bon type, appartenant au joueur courant, avec les mêmes nombres de blocs et d'appareils. Six observations au maximum, espacées de 500 ms ; une réponse absente ou invalide bloque la validation. Aucun délai entre deux rangements ou deux sorties n'est ajouté. Les journaux RESTORE_CHECK indiquent les valeurs attendues et reçues.

Pour une ancienne sortie en quarantaine, qh:list vérifie au plus une archive candidate du secteur courant : ID de sortie exact présent, original absent, identité/session cohérente, compteurs natifs conformes, empreintes d'archive intactes et aucune autre transaction ambiguë. En cas de réussite, l'archive entière est déplacée dans Archives/Retired et la place est libérée. Les fichiers de récupération et le manifeste original restent conservés. Le vaisseau présent n'est ni recréé, ni supprimé, ni réécrit. Une différence persistante conserve la quarantaine.

Ces contrôles ne comptent pas les objets des conteneurs. CargoVerified reste faux. La présence des objets doit toujours être vérifiée en jeu. Le décalage exact ayant déclenché l'ancien refus ne peut être identifié à partir d'un journal sans compteurs détaillés.

## Installation / test du vaisseau déjà sorti

1. Arrêter le serveur, les playfields et ModHost ; sauvegarder QH et ses données de partie.
2. Extraire le ZIP et fusionner Content avec le dossier Content du serveur, en remplaçant les fichiers QH fournis.
3. Redémarrer et vérifier START 0.2.8.1 et QH_STATE START 0.2.8.1 dans les journaux.
4. Rejoindre le secteur du vaisseau déjà sorti, puis lancer qh:list. Si les vérifications correspondent, sa place en quarantaine est libérée. Le vaisseau reste dans le monde.
5. Si la place reste bloquée, conserver l'archive et relever RESTORE_CHECK / RESTORED_QUARANTINE_RETAINED dans le journal QH.

Modules : Content/Mods/ModLoader/MODs/QuantumHangarQc et Content/Mods/QuantumHangarQcState.
Première installation seulement : ajouter la ligne Exemples/DllNames_QH_line.txt à Content/Mods/ModLoader/Host/DllNames.txt si absente ; ne pas remplacer la liste complète.

La configuration existante, les archives, les préférences de langue et les autres mods sont conservés. Aucun fichier privé de serveur n'est inclus. Les exemples ne remplacent aucun réglage actif. Par défaut : 10 places, anglais et marge au sol de 0,5 m. MaxShipsPerPlayer peut être réglé de 1 à 15 par partie, serveur arrêté. qh:lang garde le choix du joueur. qh:mark reste à 30 mètres devant soi ; rester sur place puis sélectionner avec qh:list. L'ID change encore à la sortie.

Validation automatisée : Docs/RESULTS_0.2.8.1.json. Les tests simulent les API et vérifient les octets des archives ; ils ne certifient pas la sérialisation des objets par Empyrion. Essai Windows/serveur réel requis avant fusion vers main.
Retour arrière : serveur arrêté, remettre ensemble les deux modules 0.2.8 sauvegardés, sans effacer ou réactiver les archives Retired. Aucun nouveau format de sauvegarde.

## English

Complete QH 0.2.8.1 candidate for validate, based on 0.2.8. An existing working EmpyrionModHost/ModLoader is required; loader dependencies are not bundled.

Restore validation now compares native counters with the native counters recorded at storage, instead of global-list counts. Two consecutive ready, matching snapshots are required, with at most six observations separated by 500 ms. Missing or invalid state replies fail closed. No cooldown between stores or loads is added.

For an already spawned quarantined ship, qh:list checks at most one candidate in the current sector. It requires the exact live spawned ID, original absence, current owner/session, matching type/counts, intact archive hashes and unambiguous archive lineage. On success the entire unchanged recovery archive moves to Archives/Retired and the slot is released. It never respawns, destroys or rewrites the live ship. Failed checks preserve quarantine. Container contents are not inspected; CargoVerified stays false.

Stop the server, playfields and ModHost, back up QH, then merge Content into the server root. Install both modules together and check the 0.2.8.1 startup version. Join the already spawned ship's sector and run qh:list. Existing settings, archives, preferences and other mods are preserved. Examples are separate from active configuration.

Automated checks simulate API responses and filesystem bytes; actual Windows/server validation is pending. See Docs/RESULTS_0.2.8.1.json. To roll back, stop everything and restore both saved 0.2.8 modules, preserving all archives including Retired.
