# Current state — 0.2.7.2 validate candidate — 2026-10-18

- Both components and State YAML: 0.2.7.2. Publish to validate; wait for real placement test before main.
- Distance messages use words between 30 and 50; English first message is "move between 30 and 50 meters away".
- Planet Y uses highest sampled terrain minus signed hull MinY plus configured clearance. Marker altitude is no longer a lower bound. Space unchanged.
- Planet replies require placement protocol 2 and matching ground/hull/clearance geometry. Update both DLLs.
- Existing clearance 2 remains effective; default stays 0.5. Existing hull bounds remain unchanged. Logs expose each height input.
- Player reports container items reappeared. Cause/timing unconfirmed; no cargo logic changed. Tank contents were also reported preserved.
- Ship ID still changes; original-ID reuse remains unimplemented and unvalidated.
- Clickable menus and saved language choices retained; automatic game-language detection not implemented.
- [Update and targeted test](docs/TERRAIN_0.2.7.2.md). Simulated results are recorded separately; no actual server run here.
