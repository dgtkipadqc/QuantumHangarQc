# Current state — 2026-10-07

- Runtime baseline: corrected QH BETA 0.2.5, recovered from the previous package.
- No new runtime version or production C# change in this preparation.
- Repository preparation: English/French docs, owner-approved artwork, build
  entry point, sources/tests, issue templates, privacy checks and PayPal support links.
- Language API audit: no per-player language member in the inspected interfaces.
- Research update: native chat has IsTextLocaKey; QH dialogs inspected as raw text.
  See docs/LANGUAGE_RESEARCH_FR_EN.md. Eight Steam interface languages identified.
- Support wording simplified to Support Frank ( Dgt Kipad QC ) / Soutenir Frank ( Dgt Kipad QC ), optional donation.
- Multilingual runtime: NOT IMPLEMENTED; automatic game-language detection BLOCKED
  pending a verified client signal/transport. Do not claim it is available.
- Latest live server result for 0.2.5 was not provided in this thread.
- Owner terms confirmed: use on own multiplayer server permitted; distribution
  of versions/updates reserved to Frank ( Dgt Kipad QC ). Redistribution requires his written
  permission. See LICENSE.md.
- Public PayPal link provided by owner and added to both READMEs and funding:
  https://paypal.me/FrankOuellet
- Pending game input: Localization.csv header from the target game build.
- Release plan: BETA testing, followed by changing the repository from private to public.

Next authorized implementation should begin with the local message catalog and
verified player-language acquisition, then proceed through the acceptance tests
in docs/LOCALIZATION_FR_EN.md. Preserve the 0.2.5 reference until a successor is
actually verified. Do not overwrite other modules' loader lists.
