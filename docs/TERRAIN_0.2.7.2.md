# QH 0.2.7.2 — validate

## Ce qui change

Le premier message anglais écrit désormais **"move between 30 and 50 meters away"**. Les messages d'aide, de dépôt et de marqueur ainsi que le refus de distance emploient la même formulation. Les autres catalogues utilisent aussi des mots pour séparer 30 et 50. Le rendu de la police en jeu reste à confirmer.

Sur une planète, la formule devient :

`altitude de sortie = point de terrain mesuré le plus haut - décalage signé du bas de coque + marge configurée`

L'ancienne formule maintenait l'altitude du marqueur comme plancher et ignorait un décalage de coque positif. Ces deux cas ajoutaient une hauteur injustifiée. Le marqueur fournit maintenant X/Z sur une planète ; le terrain détermine Y. Dans l'espace, les trois coordonnées du marqueur restent inchangées.

Le réglage `<SpawnClearanceMeters>2</SpawnClearanceMeters>` est conservé et signifie **2 mètres entre le bas de l'enveloppe de coque enregistrée et le point de terrain mesuré le plus haut**. Ce n'est pas la hauteur de l'origine du vaisseau. Le défaut reste 0,5 m et les limites 0,25–50 m. Aucune nouvelle modification de configuration n'est nécessaire pour tester la valeur 2 existante.

## Mise à jour

Arrêter le serveur, les playfields et ModHost. Sauvegarder les dossiers QH. Fusionner le dossier Content du ZIP UPDATE ONLY dans la racine du serveur. Remplacer les **deux DLL 0.2.7.2**, leurs catalogues et le YAML State, puis redémarrer l'ensemble. Le paquet ne remplace ni configuration, ni liste de chargement, ni archive.

Le nouveau Host refuse les réponses de terrain d'un ancien State sur planète. Ce refus préserve l'archive et évite de reproduire l'ancien calcul. Les anciennes archives sans dimensions de coque restent non restituables sur planète ; ne pas modifier leurs manifestes.

## Essai ciblé et limites

Avec un vaisseau de test vide, laisser la marge configurée à 2 m, créer qh:mark, s'éloigner entre 30 et 50 m et sélectionner le vaisseau. Vérifier le placement sur terrain plat puis en pente. Le vaisseau hors tension peut encore descendre ou basculer avec la physique. La grille de terrain n'est pas une détection complète des collisions : elle couvre l'enveloppe entière, donc un relief haut sous un coin peut élever le vaisseau par rapport au marqueur.

Les archives déjà enregistrées gardent leurs dimensions : les anciennes marges de géométrie peuvent encore ajouter de la hauteur. Si le placement reste excessif, transmettre **QuantumHangarQc.log** et le **Manifest.xml** de l'archive concernée en privé. La ligne TERRAIN_CHECK indique maintenant `groundY`, `hullMinY`, `clearance`, `placementVersion`, le marqueur et la sortie. Elle permet aussi de vérifier que le réglage 2 est effectivement chargé.

## Objets et identifiant

Le joueur signale maintenant que les objets des conteneurs sont revenus. L'oxygène, le carburant et la pentaxid des réservoirs étaient déjà confirmés conservés dans son essai. Le moment précis de réapparition et la cause ne sont pas établis ; aucun correctif de copie ou de restauration de cargo n'est inclus ici.

**L'identifiant du vaisseau change toujours.** Le code demande un nouvel ID au moteur avant chaque restitution. La réutilisation de l'ancien ID n'est pas implémentée ni validée : l'absence dans la liste des structures ne démontre pas à elle seule que toutes les données internes du moteur sont libérées. Ce candidat ne doit pas être présenté comme corrigeant cette demande.

## Vérifications

Compilation .NET Framework 4.7.2 avec références réelles Mif/ModApi. Tests simulés : coordonnées sous le marqueur, bas de coque positif/négatif, marge 2 m, ancien protocole refusé, géométrie incohérente refusée, pente, espace, menus et catalogues. Résultats détaillés dans docs/qa/RESULTS_0.2.7.2.json. Pas d'essai sur un serveur Empyrion depuis cet environnement ; attendre le retour de placement avant fusion sur main.

## English

The first English message now says **"move between 30 and 50 meters away"**. Planet placement uses sampled highest ground minus the signed lower hull offset plus configured clearance. Marker altitude no longer raises the ship. Space placement is unchanged.

Keep your existing clearance of 2 to test a 2-meter margin above sampled terrain at the saved lower hull boundary. Install both 0.2.7.2 DLLs, catalogs and State YAML together with the server, playfields and Host stopped. Old State planet replies are rejected. Existing configuration and archives are preserved.

Test an empty ship on flat ground and a slope. Archived conservative bounds, slopes and engine physics can still affect visible height. If it remains excessive, provide QuantumHangarQc.log and the affected Manifest.xml privately. User reports cargo has reappeared; this release does not change cargo logic. Ship ID still changes. Local tests use simulated APIs; game placement is not certified.
