# QH 0.2.8.2 — observation native du secteur

[ZIP complet](./QuantumHangarQc_0.2.8.2_COMPLET.zip) · [SHA-256](./QuantumHangarQc_0.2.8.2_COMPLET.zip.sha256) · [Installation FR/EN](../docs/COMPLETE_0.2.8.2.md)

Retire les appels QH au rafraîchissement global bloquant. Les deux modules 0.2.8.2 sont requis ; State directement dans Content/Mods. Candidat à tester sur serveur privé. Aucun nouveau délai entre opérations.

# QH 0.2.8.1 — correctif de quarantaine

[ZIP complet](./QuantumHangarQc_0.2.8.1_COMPLET.zip) · [SHA-256](./QuantumHangarQc_0.2.8.1_COMPLET.zip.sha256) · [Installation FR/EN](../docs/COMPLETE_0.2.8.1.md)

ModLoader existant requis. Remplacer les deux modules ensemble. Vérification native après sortie ; qh:list peut libérer une place bloquée si le vaisseau sorti est confirmé, avec copie de récupération conservée. Aucun nouveau délai entre opérations. Candidat sur validate, test en jeu requis.

# QH 0.2.8 — paquet complet

[Télécharger le ZIP complet](./QuantumHangarQc_0.2.8_COMPLET.zip) · [SHA-256](./QuantumHangarQc_0.2.8_COMPLET.zip.sha256) · [Installation FR/EN](../docs/COMPLETE_0.2.8.md)

Tous les fichiers QH 0.2.8, exemples et documentation. ModLoader existant requis. Mêmes DLL que UPDATE ONLY ; aucun changement de comportement supplémentaire. Candidat sur validate, essai en jeu requis.

# QH 0.2.8 — UPDATE ONLY sur validate

[Télécharger 0.2.8 UPDATE ONLY](./QuantumHangarQc_0.2.8_UPDATE_ONLY.zip) · [SHA-256](./QuantumHangarQc_0.2.8_UPDATE_ONLY.zip.sha256) · [Installation et tests](../docs/REBUILD_0.2.8.md)

Candidat reconstruit sur la base 0.2.7.2 avec confirmations natives et point `qh:mark` à 30 mètres devant soi. Remplacer les deux modules ensemble, serveur arrêté. Configuration, archives, langues mémorisées et chargement existants conservés. Tests locaux simulés ; validation en jeu requise avant main.

# Binaires QH — série 0.2.7

[Télécharger le ZIP complet QH 0.2.7.2](./QuantumHangarQc_0.2.7.2_COMPLET_MAIN.zip) · [SHA-256](./QuantumHangarQc_0.2.7.2_COMPLET_MAIN.zip.sha256)

Paquet du mod QH pour un serveur déjà équipé d'EmpyrionModHost/ModLoader : les deux DLL, huit langues par module, YAML State, exemples et instructions FR/EN. Arrêter serveur, playfields et Host, sauvegarder QH, puis fusionner le dossier Content du ZIP avec celui du serveur.

Sources de main : `30a99da32dc2998110f3b119e543a866116bc004`. Les sources de production sont identiques au commit compilé `084ea697cda7501f0c55b7f4478c605832546e48`. Les deux empreintes DLL correspondent à [l'audit](../docs/qa/BINARY_AUDIT_0.2.7.2.json).

Les configurations actives, archives, préférences, listes de chargement et autres mods ne sont pas inclus. Le paquet conserve les réglages existants. Les exemples sont rangés hors du dossier Content.

**Limites connues :** hauteur encore signalée trop élevée, journal de diagnostic attendu ; ID du vaisseau toujours renouvelé à la sortie. Il s'agit d'un reconditionnement de main, sans nouveau correctif de ces points. Installation Windows réelle non exécutée ici. Intégrité du ZIP, catalogues XML et SHA-256 vérifiés.

SHA-256 du ZIP : `7714682efcd473b03f67ba7a4a519a39a0cf481860bacd9d0d790780afafbb8a`.
