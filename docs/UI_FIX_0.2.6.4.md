# QH 0.2.6.4 — restauration des listes cliquables

La comparaison des ZIP 0.2.5, 0.2.6, 0.2.6.1, 0.2.6.2 et 0.2.6.3 confirme
une regression dans le controle du SteamID natif du playfield. Verification des
sources et du branchement IL des DLL State reellement livrees :
- 0.2.5 : joueur present, SteamID natif absent => ouverture autorisee.
- 0.2.6 a 0.2.6.3 : meme situation => refus error.session avant ShowDialogBox.
- SteamID connu different ou joueur absent => refus dans toutes les versions.

Le Host transforme les erreurs de la passerelle en error.ui. qh:list passe alors
au texte de secours non cliquable, qh:lang affiche la notification d'erreur.
Le retour utilisateur final confirme que les deux listes ne fonctionnaient pas ;
les descriptions anterieures supposant qh:list fonctionnel etaient incorrectes.
La presence effective d'un SteamID natif vide sur son serveur reste a confirmer.

0.2.6.4 restaure la compatibilite 0.2.5 pour le SteamID natif manquant. Le joueur
doit etre present ; tout SteamID natif fourni et different reste refuse. Le Host
continue d'authentifier Steam/client avant la demande et avant sauvegarde de la
langue ou action sur un vaisseau. Les protections de propriete, confirmation,
archives, session, jeton et expiration sont conservees.

qh:list : lignes des vaisseaux stockes cliquables, places libres et quarantaine
non cliquables. qh:lang : huit langues et defaut serveur cliquables, protocole de
selection commun a qh:list, choix persistant. Aucun menu sequentiel Suivant.
Log nouveau : QH_UI IDENTITY_HOST lorsque le playfield omet le SteamID.

Validation : 175 assertions menu/passerelle (SteamID natif vide, connu, different,
joueur absent, clic vaisseau, quarantaine, choix de langue, reconnexion, changement
d'identite, persistance et protocole historique). 528 assertions Host.
Les 1252 assertions de localisation de 0.2.6.3 restent applicables : aucun
catalogue ni code de traduction modifie pour 0.2.6.4.
DLL compilees avec references Microsoft .NET Framework 4.7.2.
Tests locaux avec API simulee et vrais fichiers ; affichage/clics en jeu NON TESTES.

Installation : arreter serveur, playfields et ModHost ; sauvegarder les dossiers
QH ; fusionner Content dans la racine du serveur. Remplacer les DEUX DLL, YAML
State et dossiers Languages. Aucun fichier de configuration ni donnee de partie
n'est fourni ou efface. Redemarrer et essayer qh:list puis qh:lang.
