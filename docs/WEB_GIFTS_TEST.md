# Cadeaux Quantum Universe — HOST 0.2.8.3 / State 0.2.8.2

Le portail QU 0.9.2.16 propose un cadeau d’une archive stockée à un autre compte associé localement par le propriétaire. Le mod reste l’autorité pour le quota et la propriété. Aucun appel RCON, export, destruction ou apparition n’est exécuté par le raccord cadeaux.

## Activation TEST

Depuis QU, installer la mise à jour QH sauvegardée puis raccorder les cadeaux pendant l’arrêt confirmé du serveur TEST, ModHost et EAH. Le portail écrit `QuantumWebGift.xml` dans `Saves/Games/<partie>/Mods/QuantumHangarQc`, lié au chemin exact de cette partie et contenant un secret aléatoire. Ne jamais publier ce fichier. Redémarrer manuellement le TEST et attendre le signal signé récent.

Le module State 0.2.8.2 est inchangé. Les archives existantes et leur format sont conservés. Sans raccord TEST explicite, aucun cadeau n’est traité. Les associations de comptes/Steam ID sont locales et approuvées par le propriétaire ; ce mécanisme ne prétend pas authentifier Steam.

## Contrat QU-QH-GIFT-1

Enveloppe XML `GiftEnvelope` : Payload = base64 UTF-8, Signature = HMAC-SHA256 hex. La clé HMAC est la chaîne UTF-8 du secret hex de 64 caractères (pas ses octets décodés). Champs séparés par LF, sans LF final.

Préfixe : `QU-QH-GIFT-1`, `TEST`, identifiant d’installation hex32.

- Demande : préfixe, `GIFT`, demande hex32, Steam donneur, Steam receveur, archive hex32, SHA256 du Manifest.xml, expiration Unix ms. Durée maximale 125 s côté mod.
- Signal : préfixe, `STATUS`, Unix ms, capacité native, `READY` ou `BLOCKED`.
- Reçu : préfixe, `RESULT`, demande, SHA256 du payload reçu, `COMMITTED` ou `REJECTED`, motif, Unix ms, Steam receveur, place.

Répertoires sous `QuantumWebGift` : requests, responses, journal. Le web attend un reçu signé correspondant à la même demande avant d’annoncer le transfert. Une expiration sans reçu reste incertaine ; elle n’autorise pas une nouvelle demande.

## Conservation et reprise

Les opérations QH natives et les cadeaux partagent les verrous donneur/receveur. Le mod revérifie archive unique, état STORED/REMOVING, SpawnedId nul, empreinte, fichiers et quota. Il écrit un journal durable puis déplace le dossier par Directory.Move. Seuls SteamId, CarrierId, Slot et PersonalSlot changent ; le manifeste original reste au journal. Il ne clone pas d’archive utilisable.

Après coupure : avant déplacement, rejet et original conservé ; après déplacement prouvé, achèvement du manifeste receveur et reçu. Modification externe ou situation ambiguë : blocage. La reprise du journal précède l’activation de QH, même si le fichier de raccord a été retiré. Ne pas supprimer les journaux pour contourner un résultat incertain.

## Qualification locale reproductible

`tools/verify-web-gifts.py --help` décrit les chemins nécessaires : SDK .NET8/Roslyn, références Microsoft net472, vrai Mif du jeu et protobuf-net du ModHost. Les dépendances du jeu ne sont pas distribuées par ce dépôt.

Résultats : compilation HOST PASS ; régression HOST 724 assertions ; cadeaux 14 scénarios dont quatre coupures de processus réelles ; restitution 162 assertions incluant refus du donneur et restitution au receveur. Les API du jeu sont simulées dans ces tests. Les octets des archives de test sont conservés ; leur contenu ne constitue pas une preuve de sérialisation des coffres par Empyrion.

Le portail ajoute les tests Node et le parcours Node → C# → reçu, ainsi que les tests de navigateur. Windows, serveur Empyrion réel et coffres réels : NOT_TESTED. La sortie/récupération déclenchée directement depuis le web et les transferts régionaux restent hors de ce raccord.
