# Quantum Hangar Qc 0.2.8.2 — candidat validate / validate candidate

## Correctif du rafraîchissement bloquant

Cette version retire tous les envois de Request_GlobalStructure_Update par QH. Les journaux analysés montrent un rafraîchissement de structures de 16–17 secondes, immédiatement suivi de l'arrêt d'un playfield pour absence de heartbeat. Ce lien temporel motive le correctif ; la cause interne du moteur et la stabilité réelle du serveur restent à confirmer en jeu.

QH State lit maintenant les structures du secteur chargé sur le thread Application.Update. QH conserve la liste globale en cache pour les autres secteurs, mais remplace le secteur courant par une observation native complète et récente. Les contrôles de présence après retrait/sortie et les obstacles utilisent cette observation. Les contrôles ciblés du propriétaire, alimentation, cockpit, passagers et arrimage restent actifs, comme les copies d'archives et leurs empreintes.

Une réponse absente, ancienne, incomplète ou liée à une autre session bloque l'opération. Aucun retour vers la requête globale bloquante. Le protocole exige les deux modules 0.2.8.2. Une seule observation de secteur est traitée par mise à jour du capteur ; plafond de 2 048 structures et 16 384 entités parcourues, avec refus après dépassement d'un budget observé de 50 ms. Ce budget ne peut pas interrompre un appel du moteur déjà en cours. Aucun nouveau délai entre rangements ou sorties.

Un défaut de concurrence révélé par les tests de confirmation est aussi corrigé : un jeton de dialogue déjà répondu ne peut plus rouvrir le dialogue pendant le nettoyage de ses fichiers temporaires.

Les correctifs 0.2.8.1 restent présents : contrôle natif après apparition et reconciliation prudente d'une quarantaine par qh:list. Les copies déplacées dans Archives/Retired sont conservées. Les objets des conteneurs ne sont pas comptés automatiquement ; CargoVerified reste faux.

## Installation

Prérequis : EmpyrionModHost / ModLoader déjà installé. Le chargeur et ses dépendances ne sont pas inclus.

1. Arrêter le serveur, tous les playfields et ModHost. Sauvegarder les modules QH et les données QH de la partie.
2. Extraire le ZIP et fusionner son dossier Content avec Content à la racine du serveur.
3. Vérifier les emplacements :
   - Content/Mods/ModLoader/MODs/QuantumHangarQc/QuantumHangarQc.dll
   - Content/Mods/QuantumHangarQcState/QuantumHangarQcState.dll
   - Le YAML State et Languages restent avec la DLL State. State ne va pas dans ModLoader/MODs.
4. Si une ancienne copie de QuantumHangarQcState existe dans ModLoader/MODs, la déplacer hors de Content/Mods, serveur arrêté. Ne garder qu'une installation active de State au bon endroit.
5. Redémarrer tout et vérifier START 0.2.8.2 côté Host et QH_STATE START 0.2.8.2 côté playfield.

Première installation QH seulement : ajouter la ligne Exemples/DllNames_QH_line.txt à Content/Mods/ModLoader/Host/DllNames.txt si absente. Ne pas remplacer la liste des autres mods, et ne pas y ajouter State.

Les configurations actives, archives, préférences de langue et autres mods ne sont pas inclus ni remplacés. Les exemples sont séparés. Aucun nom de sauvegarde à sélectionner. Valeurs par défaut conservées : 10 vaisseaux, anglais, marge au sol 0,5 m ; paramètres ajustables par partie.

## Essai privé avant main

- Commencer avec un petit vaisseau vide, moteur/alimentation coupés et cockpit quitté ; qh:store:ID.
- Vérifier SECTOR_NATIVE et QH_SECTOR complete=True. Aucun envoi QH command=Request_GlobalStructure_Update ne doit apparaître.
- Annuler une confirmation pour vérifier que le vaisseau reste présent, puis confirmer un rangement.
- qh:mark place le point 30 mètres devant soi. Rester sur place, qh:list puis sélectionner et confirmer la sortie.
- Vérifier la connexion, le vaisseau, ses équipements, puis les conteneurs et réservoirs sur un essai avec contenu connu.
- Si une ancienne place en quarantaine correspond à un vaisseau déjà sorti dans ce secteur, qh:list peut la libérer après vérification en conservant toute l'archive dans Retired.

Le journal QH est dans Saves/Games/<partie>/Mods/QuantumHangarQc/QuantumHangarQc.log. Une erreur sector unavailable signifie que le capteur n'a pas répondu : vérifier les deux versions, l'emplacement State et le redémarrage des playfields. Aucune suppression ni réapparition automatique sur erreur.

Les vérifications automatisées simulent les API et les octets des fichiers. Compilation des DLL de production contre les vraies références .NET 4.7.2 / ModApi. Détail dans Docs/RESULTS_0.2.8.2.json. Test Windows/serveur réel non effectué ici ; candidat à qualifier avant main. Le changement supprime l'appel suspect mais ne certifie pas la disparition de toutes les causes de déconnexion. L'ID du vaisseau change encore à la sortie.

Retour arrière : serveur arrêté, remettre les deux modules sauvegardés ensemble sans effacer les données QH. Les versions 0.2.8 et 0.2.8.1 contiennent encore l'appel suspect ; ne pas reprendre les rangements publics avec ces versions pendant le diagnostic. Les échanges SectorState sont temporaires ; aucun format de manifeste d'archive n'a changé.

## English

Complete QH 0.2.8.2 validate candidate. Requires an existing working EmpyrionModHost/ModLoader. This release removes every QH call to Request_GlobalStructure_Update, the expensive refresh observed just before a playfield heartbeat shutdown. It does not certify the engine root cause or real-server stability.

QH State captures the loaded sector directly on Application.Update. The Host replaces only that sector in its cached global list, keeping other sectors conservatively. Missing, stale, partial or mismatched snapshots stop the operation with no blocking-refresh fallback. Storage safety checks, archive hashes, cargo bytes, terrain, language persistence and the 0.2.8.1 quarantine recovery are retained. No new operation cooldown.

Stop the server, playfields and ModHost, back up QH, then merge Content into the server root. Install BOTH 0.2.8.2 modules: Host under Content/Mods/ModLoader/MODs/QuantumHangarQc, State directly under Content/Mods/QuantumHangarQcState with YAML and Languages. Move any incorrectly located State copy outside Content/Mods while stopped. Do not replace loader lists, active settings, archives or preferences.

Restart everything, check both 0.2.8.2 startup versions, then test an empty ship on a private server. Look for SECTOR_NATIVE / QH_SECTOR complete=True and no QH global-update sends. Verify store cancellation, confirmed storage, forward mark and retrieval before testing known cargo. Real engine cargo serialization and Windows/server execution remain unverified here. Preserve Archives/Retired recovery copies. See Docs/RESULTS_0.2.8.2.json.
