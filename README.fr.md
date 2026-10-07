# Quantum Hangar Qc — BETA 0.2.5

![Quantum Hangar Qc](assets/QH_Banniere_BETA.jpg)

[English](README.md) · [Installation](docs/INSTALLATION_FR_EN.md) · [Localisation](docs/LOCALIZATION_FR_EN.md)

Votre flotte vous accompagne : stockez jusqu'à **10 vaisseaux** dans votre
hangar personnel, marquez un point de sortie et récupérez un vaisseau pour
votre prochaine aventure.

## État de ce paquet

Préparation du dépôt à partir des sources corrigées **0.2.5**. Les dialogues
sont encore principalement en français, avec quelques textes bilingues.
La détection automatique de langue et `qh:lang` sont **à développer**.
Ce paquet ne constitue pas une nouvelle version multilingue à installer.

## Utilisation

| Commande | Fonction |
| --- | --- |
| `qh:help` | Aide et conditions |
| `qh:list` | Voir le hangar et sélectionner un vaisseau disponible |
| `qh:store:ID` | Déposer dans la première place libre |
| `qh:store:ID:PLACE` | Choisir la place |
| `qh:info:ID` | Diagnostic position, distance et propriété |
| `qh:mark` | Point de sortie valable cinq minutes |
| `qh:load:PLACE` | Sortir un vaisseau |

Sans `/` devant les commandes. Pour déposer : vaisseau privé appartenant au
personnage, alimentation OFF, cockpit quitté, personne à bord, aucun vaisseau
arrimé, distance maximale de 250 m par rapport à la position API du vaisseau.
Pour sortir : `qh:mark` dans une zone libre, puis s'éloigner de **50 à 1000 m**
dans le même secteur et lancer `qh:load:PLACE`. Confirmer dans le dialogue.

Les données du hangar sont liées à l'identité Steam. Le mod archive les fichiers
du vaisseau pour le restituer ; vérifier les coffres dans votre scénario pendant
la bêta. Un état QUARANTINE nécessite une vérification par l'administration.

## Administrateurs et développeurs

[Installer](docs/INSTALLATION_FR_EN.md) · [Vérifications](docs/VALIDATION.md)

Le dépôt contient les sources QH, les tests et les visuels. Le chargeur
EmpyrionModHost et les DLL du jeu sont des prérequis externes.
La compilation se lance depuis le dossier du serveur, avec le dépôt extrait
dans un sous-dossier `QuantumHangarQc` :

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\QuantumHangarQc\Build.ps1 -ServerRoot (Get-Location).Path
```

La sortie se trouve dans `QuantumHangarQc/dist/Content/Mods`. La compilation
n'installe rien. Conserver les listes de DLL et configurations existantes.

## Soutenir le projet

**[❤️ Soutenir Frank ( Dgt Kipad QC ) — don facultatif](https://paypal.me/FrankOuellet)**

Projet Frank ( Dgt Kipad QC ) / Le Spot Studio, indépendant d'Eleon. L'utilisation sur votre propre serveur, y compris multijoueur, est autorisée.
Frank ( Dgt Kipad QC ) conserve la distribution des versions et mises à jour ; toute
redistribution demande son autorisation écrite préalable. Voir `LICENSE.md`.
