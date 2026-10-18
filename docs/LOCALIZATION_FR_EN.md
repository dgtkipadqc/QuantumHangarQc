# Localisation BETA 0.2.6 — FR / EN

## Fonctionnement livré

73 clés dans huit catalogues UTF-8 XML : en, fr, de, ru, it, pt-BR, zh-Hans, el.
Les huit traductions sont des versions initiales **à relire**. Couverture technique
complète contrôlée localement ; lisibilité/polices en jeu non testées.
Commandes sans `/` :

- `qh:lang` : langue effective, origine et commandes dans les noms natifs.
- `qh:lang:fr`, `qh:lang:en`, `qh:lang:de`, `qh:lang:ru`, `qh:lang:it`, `qh:lang:pt-BR`, `qh:lang:zh-Hans`, `qh:lang:el`.
- `qh:lang:auto` : retire réellement le choix enregistré. Aucun auto-détecteur n'est connecté : recours explicite au défaut serveur.

Casse ignorée ; `_` normalisé en `-`. Alias `pt` → pt-BR, `zh` et `zh-CN` → zh-Hans.
Ni pt-PT ni zh-TW ne sont convertis silencieusement. Une langue inconnue conserve
le choix précédent. Aucun code ne devient un chemin arbitraire.

Priorité implémentée : choix manuel > défaut serveur > anglais. L'étage « langue
Empyrion de session vérifiée » reste BLOQUÉ faute de source ; il n'est pas simulé
dans le runtime. Ne pas annoncer la priorité automatique comme testée.
`<DefaultLanguage>fr</DefaultLanguage>` est facultatif dans Configuration.xml.
Sans option : en. Une option existante reconnue est conservée. Ni Windows,
CultureInfo, Steam supposé, IP ou nom du joueur ne servent à détecter une langue.

Choix stockés par Steam fourni par Request_Player_Info, dans
`Saves/Games/<save>/Mods/QuantumHangarQc/Preferences/`. XML schéma 1, écriture
atomique ; un fichier corrompu est conservé avec suffixe `.corrupt.*` avant secours.
Pas de dépendance au fichier personnage ni à cb:reset. Aucun cache automatique
par entity ID. Changement manuel pendant une confirmation : notification, fenêtre
courante intacte, nouvelle langue pour les prochains messages/dialogues.

## Catalogues et bridge

Une source `src/Shared/Localization.cs` est compilée dans les deux composants.
Catalogues chargés une fois, contrôles UTF-8 strict, XML sans DTD, schéma, doublons,
valeurs vides, taille et paramètres. Une langue invalide → anglais embarqué ; clé
manquante → clé anglaise ; format irréparable → message minimal LOCALE_RENDER.
Aucun accès web, LLM ou I/O de traduction par tick. Les préférences ne sont lues
qu'au premier accès de leur identité, puis mises en cache.

Bridge liste schéma 2 : `Language` et `Protocol`. Ancienne requête sans champs :
anglais sûr. Protocoles inconnus, sessions incorrectes et identifiants d'archive
malformés sont refusés. Tokens, expiration, destinataire, anti-double-clic et
confirmation finale demeurent actifs. Les noms non fiables sont échappés dans
les textes TMP ; seul le code produit les liens de sélection. Les codes persistés
STORED/QUARANTINE ne sont pas traduits, seulement leur affichage.

L'inventaire `PLAYER_TEXT_INVENTORY.json` relie clés, sources, canaux, paramètres
et catalogues. Les diagnostics techniques internes et détails persistés ne sont
pas des phrases destinées au joueur.

## Limites

Automatique complet et chat natif non validés. Voir LANGUAGE_RESEARCH_FR_EN.md.
Pas de sélection de langue cliquable dans cette livraison ; toutes les langues
se choisissent par commande. CSV de scénario non modifié. Essais Windows/jeu requis.

## English

Eight local XML catalogs and a saved, authenticated per-Steam language override
are implemented. Use qh:lang:CODE or qh:lang:auto to clear it. The latter currently
uses the server default because no verified game-language adapter exists.
Defaults to English, with an optional DefaultLanguage setting. Shared validation,
embedded fallback, escaped dynamic values and localized Host/State dialogs do
not change ship transaction rules. All translations await human review and game
font/layout tests. No client plugin or Internet is required. Native chat helper is
an unvalidated laboratory prototype, not a production feature.
