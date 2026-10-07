# Player guide

Start with `qh:help`. Your personal hangar holds ten ships.

## Store

1. Use a privately owned ship belonging to your current character.
2. Turn power off, leave the cockpit and have everyone leave the ship.
3. Undock attached ships and stop scripts/logistics modifying cargo.
4. Stay within 250 m of the ship's API position.
5. Send `qh:store:SHIP_ID`, confirm and wait for success.
6. Check `qh:list`.

## Retrieve

1. Choose clear space large enough for the entire ship, away from obstacles.
2. Send `qh:mark`; the return point is valid for five minutes.
3. Move 50–1000 m away, staying in the same playfield.
4. Send `qh:load:SLOT`, or select the ship from a working clickable list.
5. Confirm and wait for completion. Inspect the ship and cargo afterward.

The marker is a visual aid. Its display/removal is beta functionality.
STORED means available. QUARANTINE means ask a server administrator to inspect
the logs before attempting recovery. Never delete archive files to free a slot.
Use a test ship and test cargo for initial beta validation.

Player language selection is not implemented in baseline 0.2.5. Instructions
in this document do not imply that dialogs are already translated into English.
