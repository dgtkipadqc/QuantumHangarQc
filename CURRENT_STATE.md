# Current state — 2026-10-08

- Branch target: validate; runtime BETA 0.2.6. main remains the 0.2.5 reference pending Frank's agreement.
- Implemented: 73 semantic message keys, eight UTF-8 XML catalogs, shared Host/State resolver, embedded English fallback.
- Implemented: qh:lang and eight choices; authenticated Steam preference in the save's Mods/QuantumHangarQc/Preferences, independent of ship archives and character entity ID.
- Implemented: qh:lang:auto removes the manual override. There is NO verified automatic language source; server default then English applies.
- Implemented: localized help, diagnostics, confirmations, notification results/refusals, list rows and buttons. Technical exception details remain in logs; player errors have translated semantic explanations and stable codes.
- A language change during a ship dialog sends a notification and changes the next dialog; it does not replace or execute the pending transaction.
- Both components compiled using Microsoft .NET Framework 4.7.2 references and real external Empyrion APIs. Queue<T> resolves to System.dll.
- Local tests: 528 Host assertions, 34 State assertions, 21 selection assertions, 1226 localization assertions, 10006 fallback/cache assertions. These are simulated API/filesystem checks; not game certification.
- Prototype native chat helper compiles, but is laboratory-only and excluded from production DLLs/ZIP. Native chat behavior NOT_RUN.
- Windows startup, real-game fonts/cargo/rollback and automatic language detection remain NOT_RUN/BLOCKED. Localization.csv build/path/header not supplied.
- All eight language drafts require human review. Language commands are the selection interface; a clickable language picker is not delivered.
- Capacity remains 10; legacy archive compatibility, marker expiry/removal and retrieval geometry preserved.
- Public beta repository; no future private-to-public transition pending.
- Owner: Frank ( Dgt Kipad QC ). https://paypal.me/FrankOuellet unchanged; LICENSE.md unchanged.

See docs/VALIDATION.md, docs/LOCALIZATION_FR_EN.md and docs/qa for current evidence.
