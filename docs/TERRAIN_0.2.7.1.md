# QH 0.2.7.1 — candidat validate / validate candidate

## État

Essais avec un vaisseau vide uniquement. La perte des objets dans les conteneurs signalée avec 0.2.7 n'est pas corrigée par ce paquet. Le joueur confirme que l'oxygène, le carburant et la pentaxid des réservoirs sont conservés dans son essai. Cela ne valide pas les conteneurs ni tous les scénarios. Ne pas fusionner sur main avant résolution de l'incident.

## Changements

- Pour sortir un vaisseau, le joueur doit être entre **30 et 50 mètres inclus du point qh:mark**, dans le même secteur. Il s'agit de la distance spatiale au marqueur, pas d'une hauteur de sortie. La condition est revérifiée après confirmation.
- La distance minimale des joueurs à la position de sortie corrigée passe à 30 m. Les autres contrôles de proximité restent actifs.
- Marge au sol par défaut : **0,5 m**, au lieu de 5 m. Valeurs acceptées : 0,25 à 50 m.
- Pour les nouveaux dépôts, le calcul de coque utilise les bords à un demi-bloc des centres extrêmes, au lieu d'ajouter un bloc entier. Les dimensions des archives existantes ne sont pas réécrites.
- L'ID du vaisseau change toujours lors de sa restitution. Aucun mécanisme de récupération des objets n'a été ajouté.

## Installation et réglage

Arrêter le serveur, ses playfields et ModHost. Sauvegarder les dossiers QH, puis fusionner le dossier Content du ZIP avec la racine du serveur. Installer les deux DLL ensemble, version **0.2.7.1**, ainsi que les catalogues et le YAML State. Ce paquet UPDATE ONLY nécessite une installation QH existante.

Le ZIP ne remplace aucune configuration, liste de DLL ou archive. Dans `Saves/Games/<partie active>/Mods/QuantumHangarQc/Configuration.xml`, ajuster seulement ce champ sous Settings :

```xml
<SpawnClearanceMeters>0.5</SpawnClearanceMeters>
```

Utiliser un point décimal dans le XML. Si le champ est absent, la nouvelle valeur par défaut s'applique. Si une valeur explicite existe déjà (par exemple 5 ou 2), elle reste prioritaire : la changer pour tester 0,5 m. Conserver les autres paramètres. Redémarrer le Host.

## Essai ciblé

Déposer un vaisseau de test vide avec les deux modules 0.2.7.1. Poser qh:mark au sol dans une zone ouverte, s'éloigner de 40 m et sélectionner le vaisseau dans qh:list. Observer son bas de coque sur sol plat puis sur une pente. Vérifier les refus en dessous de 30 m et au-delà de 50 m. Les menus et langues mémorisées restent disponibles.

Une petite descente ou un basculement demeure possible avec la physique d'un vaisseau hors tension : ce correctif ne le maintient pas en lévitation. Le marqueur en hauteur, le point de terrain le plus haut sous l'emprise et les anciennes dimensions conservatrices peuvent encore augmenter la hauteur. La grille de terrain n'est pas un contrôle complet des collisions. Les limites de 0.2.7 concernant les anciens dépôts sans dimensions et la récupération dans l'espace restent applicables ; ne pas utiliser un vaisseau chargé pour ces essais.

## Diagnostic des conteneurs

Les routines de copie de fichiers des DLL comparées de 0.2.6 à 0.2.7 sont identiques ; le code source 0.2.5 utilise la même logique. Cela ne prouve pas que le moteur réimporte tous les objets. Les vérifications après apparition ne comparent pas les piles d'objets.

Conserver l'archive du vaisseau touché : `Archives/Retired/<ArchiveId>/` (Manifest.xml, Export.dat et Shared), ainsi que `QuantumHangarQc.log`, sous le dossier QH de la sauvegarde. Un état QUARANTINE peut laisser l'archive sous `Archives/Players/<SteamId>/<ArchiveId>/`. Copier les fichiers sans modifier les originaux, et les transmettre en privé. Ne pas les publier dans ce dépôt. Leur inspection est nécessaire pour déterminer où la perte se produit et si une récupération est possible.

## Validation locale

Les résultats détaillés sont dans `docs/qa/RESULTS_0.2.7.1.json`. Compilation contre .NET Framework 4.7.2 et les références Mif/ModApi réelles. Les tests utilisent des API simulées et ne valident ni la physique ni le contenu réel des conteneurs en jeu. Les deux DLL n'ont pas été exécutées sur un serveur Empyrion ici.

## English

**Empty test ships only; container item loss is unresolved. Do not merge to main yet.** The player reported tank oxygen, fuel and pentaxid preserved in their test.

Install both 0.2.7.1 QH DLLs together with their catalogs and State YAML, with the server, playfields and ModHost stopped. Merge Content into the server root after backing up QH. Existing configuration and archives are not replaced.

Retrieval requires 30–50 m from qh:mark, including both endpoints. Ground clearance defaults to 0.5 m (allowed 0.25–50). Set `<SpawnClearanceMeters>0.5</SpawnClearanceMeters>` in the active save's QH Configuration.xml when an explicit older value exists. New hull captures use half-block edges; old archives retain their saved geometry. Test an empty ship at 40 m on flat ground and a slope. Physics can still settle or tip an unpowered ship. No stable ship ID or cargo restoration fix is included.

Keep the affected archive, including Manifest.xml, Export.dat and Shared, plus the QH log for private investigation. Local simulated tests cannot establish real cargo preservation or recovery.
