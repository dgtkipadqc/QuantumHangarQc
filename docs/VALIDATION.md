# Validation status

## This repository preparation

Source and runtime byte comparisons, archive integrity, generic configuration,
shared source consistency, absence of game DLLs and known private identifiers
are checked by the packaging process. See REPOSITORY_CHECKS.json.

The new Build.ps1 has not been executed on Windows in this preparation.
No automatic-language feature or translated runtime has been tested or delivered.
No dedicated server startup, GUI rendering, cargo integrity or GPS removal test
was performed in this session.

## Historical 0.2.5 evidence (not rerun here)

The recovered build reports 528 host assertions, 34 native observer assertions,
and 18 selection bridge assertions on simulated APIs. The previous reference
audit checked 103 host and 63 state Microsoft type references and resolved
Queue<T> to System.dll. These are historical local checks, not live-game proof.

Before a public release, build on the documented Windows setup, test ordinary
players and administrators, restart persistence, private ownership, occupied and
powered ships, cancellation, cargo restore, quarantine handling, clickable list
fallback, GPS behavior and coexistence with the server's existing mods.
