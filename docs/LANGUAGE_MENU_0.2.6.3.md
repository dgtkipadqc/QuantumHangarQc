# QH 0.2.6.3 — language menu availability fallback

Observed report on 0.2.6.2: bare qh:lang returned a yellow error.ui notification;
qh:lang:auto worked. Exact native failure cause is not established without server logs.

The clickable list remains the preferred UI. If the State bridge reports dialog
unavailability (including legacy error.ui), or does not acknowledge opening within
six seconds, the Host now uses its existing two-button dialog transport.
This fallback displays all languages and highlights the current candidate. Click
the named language button to save, or Next to advance; the final negative button
closes. This is a sequential button picker, not clickable rows. Direct language
commands remain supported. No automatic language selection occurs on opening.

Both paths recheck Steam identity and client session before saving. Invalid reply
correlation, invalid request and explicit identity errors do not enable fallback.
Expired native requests are removed before the fallback is opened.

State logs now distinguish SHOW_REJECTED, SHOW_EXCEPTION and request rejection.
Host logs MENU_FALLBACK with its reason. State startup identifies 0.2.6.3.

Update both DLLs, State YAML and both Languages directories while server,
playfields and ModHost are stopped. Preserve all configs, loader lists, save data
and Preferences. The update archive contains no loader binaries or configs.

Local validation: 59 menu assertions; 1268 localization assertions; 528 Host
assertions. Native API simulated, actual production bridge and file exchange.
Production DLLs compile against .NET Framework 4.7.2 and native ModApi references.
Native Windows/game rendering is NOT tested. This is an availability mitigation;
it does not prove the cause of the original native dialog failure is repaired.
