# QH 0.2.6.3 — clickable languages using the qh:list transport

User report: qh:list works, qh:lang:auto works, bare qh:lang returns error.ui.
The prior language implementation introduced protocol 3. The older qh:list
bridge accepts protocols 0/2 only and deterministically returns error.ui for 3.
This incompatibility is reproduced locally. The exact DLL loaded on the user's
server has not been inspected, so this remains a matching cause, not a confirmed
server diagnosis.

qh:lang now sends nine ordinary protocol-2 selection rows using the exact
qh:list token/slot/archive link format and callback. Eight languages plus server
default appear together. Each row maps to one language in the Host; no ship
store/list/load action is executed by that mapping. Updated State displays the
language title and native names. The original protocol-2 State can also return
these choices (with its older generic ship-list headings).

The briefly prepared sequential Next-button fallback has been REMOVED. There is
no sequential picker in this deliverable. Invalid selection tokens, identity,
client reconnect and stale callbacks remain rejected. Choices are saved through
the existing Preferences mechanism. Closing does not change the language.
Language failures now advise retrying qh:lang, never qh:load:SLOT.

Install BOTH DLLs, State YAML and BOTH Languages folders together. In particular,
the intermediate 0.2.6.2 State expects language protocol 3; do not mix that State
with the new Host. Stop server, playfields and Host before copying. Preserve all
configuration, loader lists, archive and Preferences files.

Local QA: 154 language-menu assertions, 1252 localization assertions and 528 Host
assertions. Tests include the original cff7ad0 qh:list bridge and request/reply
classes (namespace-only renaming) serialized through real files, all nine choices
on both bridges, protocol-3 rejection reproduction, save persistence, cancellation,
replay, wrong token/player, reconnect. Native UI API is simulated.
Production DLLs built against Microsoft .NET Framework 4.7.2 references.
Native game rendering / Windows server runtime remain NOT TESTED.
