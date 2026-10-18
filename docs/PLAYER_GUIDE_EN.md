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
3. Move between 30 and 50 meters away, staying in the same playfield.
4. Send `qh:load:SLOT`, or select the ship from a working clickable list.
5. Confirm and wait for completion. Inspect the ship and cargo afterward.

The marker is a visual aid. Its display/removal is beta functionality.
STORED means available. QUARANTINE means ask a server administrator to inspect
the logs before attempting recovery. Never delete archive files to free a slot.
Use an empty test ship for 0.2.7.2 placement validation. Previously missing
container items were reported to reappear, but the timing/cause is not established.

The package defaults to English. Use `qh:lang:fr` (or `qh:lang fr`) for
French, `qh:lang:en` for English, and `qh:lang` to check the effective choice.
Your choice is saved per SteamID in the current save and survives reconnection
and restart. `qh:lang:auto` restores the save default. Changing the game
language does not automatically change the mod language.
