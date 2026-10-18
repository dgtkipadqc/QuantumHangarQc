# QH 0.2.6.2 — menu de langues cliquable

Dans le chat, taper `qh:lang`, puis cliquer sur la langue voulue, comme dans
`qh:list`. Les huit langues sont affichees dans leur propre langue. Un choix
« server default » (traduit dans la langue actuelle) remet le defaut de la partie.
Fermer la fenetre ne change rien. Le choix est sauvegarde par SteamID pour cette
partie et reste prioritaire apres reconnexion et redemarrage.

Les deux composants Host ET State doivent etre remplaces ensemble. L'anglais
reste le defaut du modele; les configurations de sauvegarde existantes et les
preferences ne sont pas ecrasees. Les commandes `qh:lang:fr` restent disponibles.
Pendant une operation de vaisseau en cours, aucun menu ne remplace sa confirmation.

Validation locale : compilation des deux DLL, 528 assertions Host, 1236 langues,
26 assertions de menu utilisant UiBridge reel et une API native simulee.
Couverture : ouverture, huit langues, clic, sauvegarde, annulation, autre joueur,
ancien jeton, connexion changee, choix inconnu, double callback et retour au defaut.
Le clic visuel dans Empyrion et le demarrage Windows restent NON TESTES ici.

English: type qh:lang and click a language, using the same clickable UI as qh:list.
Install both updated DLLs together. Saved player choices persist across reconnects
and restarts. Closing does not change the preference. Actual game UI testing pending.
