# Installation / Update — FR / EN

## Français

1. Installer EmpyrionModHost depuis son projet officiel. Sur un serveur déjà
   équipé, conserver sa version, ses dépendances et ses autres modules.
2. Compiler QH avec `Build.ps1`, ou utiliser une release QH vérifiée.
3. Arrêter EmpyrionDedicated, les playfields et EmpyrionModHost.
4. Sauvegarder les fichiers QH existants avant remplacement.
5. Fusionner les dossiers `dist/Content/Mods/ModLoader` et
   `dist/Content/Mods/QuantumHangarQcState` dans `Content/Mods/`.
6. Première installation seulement : copier `config/Configuration.example.xml`
   vers `Content/Mods/ModLoader/MODs/QuantumHangarQc/Configuration.xml`.
   Remplacer `YOUR_SAVE_GAME_FOLDER` par le nom exact sous `Saves/Games`.
   Pour une mise à jour, conserver le `Configuration.xml` existant.
7. Vérifier les deux listes ci-dessous et ajouter seulement la ligne manquante.
   Ne pas remplacer une liste complète ni y ajouter QuantumHangarQcState.
8. Redémarrer, rejoindre le serveur, tester `qh:help` puis `qh:list`.

| Fichier | Ligne requise |
| --- | --- |
| `Content/Mods/ModLoader/DllNames.txt` | `Client\EmpyrionModClient.dll` |
| `Content/Mods/ModLoader/Host/DllNames.txt` | `..\MODs\QuantumHangarQc\QuantumHangarQc.dll` |

QuantumHangarQcState est un module natif PfServer, placé directement sous
`Content/Mods/QuantumHangarQcState/`. Il ne va pas dans la liste du Host.

Les archives restent dans `Saves/Games/<save>/Mods/QuantumHangarQc`.
Aucun fichier de sauvegarde, aucun DllNames et aucune configuration active
ne sont fournis dans la sortie de compilation. Il n'y a pas de dépendance
fonctionnelle à VB, Recycle ou CB lorsque SaveGameName est configuré.

En cas d'échec : relever la première erreur QH du journal DedicatedServer après
connexion. Un ancien QuantumHangarQc.log ne prouve pas le démarrage courant.

## English

Install EmpyrionModHost separately. Preserve existing loader files and modules.
Build QH or use a verified release. Stop the dedicated server, playfields and
EmpyrionModHost. Back up the current QH binaries, then merge `dist/Content/Mods`
into the server's `Content/Mods`.

On first installation only, copy the example XML to the host QH module folder,
rename it `Configuration.xml` and enter the exact save folder name. On updates,
preserve the existing configuration. Append the required lines in the table
only if absent; never replace complete loader lists. The state module is loaded
natively by PfServer and must not be added to Host/DllNames.txt.

Restart, join, then test `qh:help` and `qh:list`. Verify private ship storage,
retrieval and cargo on a test save. Existing saved ships are not part of the
repository or installation output.
