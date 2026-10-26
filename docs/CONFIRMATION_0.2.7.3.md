# 0.2.7.3 — confirmation native / native confirmation

## Français

Les tentatives examinées s'arrêtent avant l'export : la requête `Request_ShowDialog_SinglePlayer` expire après 60 secondes. Le rafraîchissement des structures prend aussi environ 15–16 secondes. Le journal ne permet pas de déterminer pourquoi le client ne répond pas à l'ancienne boîte de dialogue.

Le rangement et la sortie utilisent maintenant `QuantumHangarQcState.ShowDialogBox`, par le même pont natif que `qh:list` et `qh:lang`. Deux boutons présentent l'action et l'annulation. Échap annule ; Entrée et les liens ne confirment pas. Aucun clic automatique ni seconde tentative d'export/retrait n'est ajouté.

Pour un vaisseau connu dans le même secteur, l'aperçu utilise la liste disponible et vérifie directement sa position et son état natif. Un rafraîchissement reste nécessaire si le vaisseau manque dans cette liste ou si son secteur diffère. Les rafraîchissements après confirmation, avant retrait et pour vérifier l'absence restent présents : le correctif supprime une requête coûteuse habituelle avant l'affichage, pas tous les délais du serveur.

### Installation sur un serveur QH existant

1. Arrêter le serveur, tous les playfields et le Host ; sauvegarder la sauvegarde du jeu et les archives QH.
2. Extraire le ZIP UPDATE_ONLY et fusionner son dossier `Content` avec celui du serveur. Remplacer **les deux DLL QH**, les catalogues de langues et le YAML State fournis.
3. Conserver les configurations actives, archives, préférences et listes de chargement. Le ZIP ne contient aucun de ces fichiers.
4. Redémarrer et vérifier `START 0.2.7.3` côté Host et `QH_STATE START 0.2.7.3` côté playfield.
5. Tester `qh:store:ID` : vérifier le nom/ID, annuler une première fois, puis relancer et confirmer. Vérifier `qh:list`, puis la sortie avec un repère et une distance entre 30 et 50 mètres.

Si le menu ne s'ouvre pas, le diagnostic est maintenant `error.confirmation`, sans export ni retrait. Vérifier que le State 0.2.7.3 est chargé dans le playfield ; une ancienne DLL State ne prend pas en charge les nouvelles confirmations. Les journaux ajoutent `CONFIRM_REQUEST`, `QH_UI CONFIRM` et `CONFIRM_RESULT`.

Tests locaux : 729 assertions Host, 1332 localisation, 175 menus et 99 confirmations natives, avec API simulée. Les deux DLL sont compilées contre les véritables références de jeu et Microsoft .NET Framework 4.7.2. Aucun démarrage Windows ni essai sur serveur réel n'a été effectué ici. La hauteur et la conservation de l'ID ne sont pas corrigées par cette version ; le code de copie des coffres et réservoirs est conservé.

## English

The inspected attempts stop before export: `Request_ShowDialog_SinglePlayer` times out after 60 seconds. Structure refresh also takes about 15–16 seconds. These logs do not establish why the client does not answer the legacy dialog.

Storage and retrieval now use the native `ShowDialogBox` bridge already used by the ship/language menus. Explicit confirm/cancel buttons replace the legacy request for these two operations. Escape cancels; Enter and links cannot confirm. Tokens, player/session checks, freshness checks and archive verification remain required.

The known-ship preview skips one expensive refresh while checking live position and native ship state. Missing ships or a mismatched cached playfield trigger a refresh. Refreshes after confirmation, before removal and for absence verification remain mandatory.

Stop the dedicated server, playfields and Host; back up the save and QH archives. Merge the ZIP's `Content` folder into the server and replace **both QH DLLs**, language catalogs and State YAML together. Preserve existing configuration, archives, preferences and loader lists. Restart and check version 0.2.7.3 in both components' logs. Test cancellation, storage, clickable menus and retrieval.

An unavailable/old bridge now reports `error.confirmation` before export or removal. Local simulated regressions pass; real-game dialog rendering and Windows/server startup remain untested. This update does not change terrain placement, ship-ID allocation or cargo-copy logic.
