# Recherche : langues Empyrion et affichage QH

Date : 2026-10-07. Recherche documentaire et inspection statique ; aucun essai
Empyrion en cours d'exécution. Le runtime QH reste en 0.2.5.

## 1. Langues officiellement annoncées

La page Steam publiée pour Empyrion annonce huit langues d'interface :

| Langue | Code proposé pour les catalogues QH |
| --- | --- |
| English | en |
| Deutsch / allemand | de |
| Français | fr |
| Русский / russe | ru |
| Italiano / italien | it |
| Português do Brasil | pt-BR |
| 简体中文 / chinois simplifié | zh-Hans |
| Ελληνικά / grec | el |

Ces codes sont une convention proposée pour QH, pas des identifiants API
Empyrion vérifiés. La liste commerciale des langues supportées ne garantit
pas les noms exacts des colonnes CSV ni la complétude des traductions d'un scénario.
Le header du Localization.csv installé reste nécessaire pour valider les colonnes.

Source primaire : https://store.steampowered.com/app/383120/Empyrion__Galactic_Survival/

## 2. Le chat peut demander une traduction au jeu

Le README du projet EmpyrionWebAccess décrit un message construit à partir
d'une clé de Localization.csv et de deux paramètres maximum. L'inspection de
son ChatController.cs précise comment cela fonctionne : le préfixe `|` est
interprété par EWA, puis le mod renseigne `Eleon.MessageData.IsTextLocaKey`,
`Text`, `Arg1` et `Arg2` avant Request_SendChatMessage.

IMPORTANT : `|` est un raccourci du parseur EWA ; ce n'est pas une preuve que
coller ce préfixe dans n'importe quel message QH activera la traduction.
Le ModApi.dll inspecté possède effectivement ces champs dans MessageData.
Le mécanisme permet de demander une résolution de clé au jeu, plutôt que de
connaître la langue du joueur côté serveur. Son fonctionnement pour nos propres
clés dépend de leur présence dans les données de localisation chargées chez le
client. Distribution des clés du scénario et actualisation du cache : à tester.

Sources primaires :
- https://github.com/GitHub-TC/EmpyrionWebAccess (section chat)
- https://github.com/GitHub-TC/EmpyrionWebAccess/blob/master/EmpyrionModWebHost/Controllers/ChatController.cs
- https://github.com/GitHub-TC/EmpyrionScripting/tree/master/dependencies (ModApi.dll)

## 3. Les fenêtres QH sont différentes

Dans ModApi.dll, DialogConfig expose TitleText, BodyText, ButtonTexts et des
options de dialogue. Il n'a pas de champ IsTextLocaKey. L'ancien DialogBoxData
contient MsgText et les libellés des deux boutons, sans ce drapeau non plus.

L'inspection IL du Assembly-CSharp.dll fourni précédemment pour build 5150
montre que le rendu de DialogConfig affecte directement TitleText et BodyText
à TMP_Text.set_text, et fait de même pour les libellés des boutons.
Cette observation ne prouve pas tous les chemins internes du moteur, mais ne
valide aucune résolution automatique de clés dans les fenêtres utilisées par QH.
Ajouter uniquement Localization.csv ne suffit donc pas à déclarer qh:help,
les confirmations ou la liste cliquable multilingues.

## 4. Langue par joueur : toujours pas de propriété vérifiée

L'audit local des interfaces PlayerInfo et IPlayer ne trouve aucun champ de
langue. Le développeur d'EmpyrionScripting signalait déjà cette limite en 2021 ;
cette source historique complète l'inspection, elle ne remplace pas une preuve
sur chaque future version du jeu.

Source primaire historique :
https://empyriononline.com/threads/mod-empyrion-scripting-mod.49290/page-12/

## 5. Implémentation proposée pour QH

1. **Notifications chat :** tester SendChatMessage avec une clé QH unique,
   IsTextLocaKey=true et des textes FR/EN présents dans la localisation chargée.
   Deux joueurs de langues différentes doivent recevoir deux traductions.
2. **Ne pas remplacer aveuglément Notify :** QH utilise actuellement
   Request_InGameMessage_SinglePlayer. La preuve concernant le chat ne certifie
   pas les notifications écran ou les fenêtres de confirmation.
3. **Dialogues :** préparer des catalogues UTF-8 et faire traduire chaque clé
   avant ShowDialogBox / Request_ShowDialog_SinglePlayer. Utiliser une préférence
   par joueur (`qh:lang`) tant qu'une vraie langue client n'est pas disponible.
4. **Automatique complet :** étudier une source client de la langue sélectionnée
   et un transport authentifié. N'utiliser ni la langue Windows ni celle du
   processus serveur. Ne pas rendre un composant client obligatoire avant essais.
5. **Catalogue :** FR/EN d'abord pour les tests, puis les six autres langues
   annoncées. Chaque langue reste « à relire » jusqu'à contrôle humain et visuel.
6. **Compatibilité :** conserver les clés QH préfixées, fusionner les CSV sans
   remplacer la localisation du scénario, conserver les noms des vaisseaux,
   tester les caractères non latins, le changement de langue et les reconnexions.

Aucun CSV de scénario n'a été modifié et aucun nouveau code multilingue n'est
présent dans la DLL livrée en référence. Les lignes ci-dessus sont un plan
fondé sur les mécanismes trouvés, pas une validation en jeu.

## English summary

Steam lists eight interface languages (en, de, fr, ru, it, pt-BR, simplified
Chinese and Greek). QH catalog codes remain a project convention; verify actual
CSV column names against the installed game. EWA uses MessageData.IsTextLocaKey
for localizable chat messages. Its pipe prefix is parsed by EWA, not a universal
QH syntax. The inspected QH dialog rendering assigns raw strings to TMP text
fields, and the inspected server player interfaces expose no game-language
property. Test client-resolved chat keys first; use per-player catalog selection
for dialogs until a verified client-language signal is available. No multilingual
runtime or in-game test is delivered by this research update.
