# Correctif QH 0.2.7.3 — validate

[Télécharger UPDATE_ONLY](./QuantumHangarQc_0.2.7.3_CONFIRMATION_UPDATE_ONLY.zip) · [SHA-256](./QuantumHangarQc_0.2.7.3_CONFIRMATION_UPDATE_ONLY.zip.sha256) · [Installation FR/EN](../docs/CONFIRMATION_0.2.7.3.md)

Pour une installation QH existante : deux DLL 0.2.7.3, catalogues et YAML State. Confirmation du rangement et de la sortie via le pont natif ; une requête coûteuse retirée de l'aperçu habituel. Les contrôles avant retrait sont conservés. Arrêter serveur, playfields et Host, puis fusionner Content. Les fichiers de configuration actifs et les archives ne sont pas inclus.

Tests locaux simulés réussis ; affichage dans le jeu et démarrage Windows à valider. Hauteur et ID inchangés.

# Binaires QH — série 0.2.7

[Télécharger le ZIP complet QH 0.2.7.2](./QuantumHangarQc_0.2.7.2_COMPLET_MAIN.zip) · [SHA-256](./QuantumHangarQc_0.2.7.2_COMPLET_MAIN.zip.sha256)

Paquet du mod QH pour un serveur déjà équipé d'EmpyrionModHost/ModLoader : les deux DLL, huit langues par module, YAML State, exemples et instructions FR/EN. Arrêter serveur, playfields et Host, sauvegarder QH, puis fusionner le dossier Content du ZIP avec celui du serveur.

Sources de main : `30a99da32dc2998110f3b119e543a866116bc004`. Les sources de production sont identiques au commit compilé `084ea697cda7501f0c55b7f4478c605832546e48`. Les deux empreintes DLL correspondent à [l'audit](../docs/qa/BINARY_AUDIT_0.2.7.2.json).

Les configurations actives, archives, préférences, listes de chargement et autres mods ne sont pas inclus. Le paquet conserve les réglages existants. Les exemples sont rangés hors du dossier Content.

**Limites connues :** hauteur encore signalée trop élevée, journal de diagnostic attendu ; ID du vaisseau toujours renouvelé à la sortie. Il s'agit d'un reconditionnement de main, sans nouveau correctif de ces points. Installation Windows réelle non exécutée ici. Intégrité du ZIP, catalogues XML et SHA-256 vérifiés.

SHA-256 du ZIP : `7714682efcd473b03f67ba7a4a519a39a0cf481860bacd9d0d790780afafbb8a`.
