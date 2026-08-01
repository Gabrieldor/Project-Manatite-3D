# Procedural Planet Spawning

## Goal
Replace the 3 hand-placed planets (lava/ice/acid) with a procedural system: at scene start, 3 planets spawn at random positions/sizes anywhere in the playable field, each an independently-random type, each carrying its own randomized enemy encounter zone and spawn table.

## Current state (found in `Outer Space.unity`)
- `IcePlanet` / `LavaPlanet` / `AcidPlanet`: plain primitive-sphere meshes, hand-placed, `Lockable` only — no encounter trigger of their own.
- 7 separate `Planet_XX_SpawnZone` objects (01,02,10,11,12,13,14): ad hoc `EnemySpawnZone` triggers, not physically tied to any of the 3 visual planets.
- 4 separate `Planet_04/05/06/07`: inert mobility-test obstacles (GDD: source of the documented playfield-bounds box) — unrelated to the 3 visual planets, **not touched by this change**.
- `PlayfieldBounds` GameObject's `BoxCollider` is currently `size (1,1,1)` at scale 1 — doesn't actually match the box GDD documents (center `275,0,137.5`, size `9350×6600×10175`). Pre-existing bug, noted but out of scope here.

## Deliverables
- `Scripts/Data/PlanetData.cs` (`Manatite.Data`) — per-type SO: `material`, `minScale`/`maxScale`, `EnemySpawnData spawnTable` (reuses existing class), `minZoneRadiusMultiplier`/`maxZoneRadiusMultiplier` (applied to the spawned scale to size the encounter trigger).
- `Data/PlanetData_Lava.asset`, `PlanetData_Ice.asset`, `PlanetData_Acid.asset` (reuse `LavaPlanet.mat`/`IcePlanet.mat`/`AcidPlanet.mat`).
- `Data/EnemySpawn_Lava.asset`, `EnemySpawn_Ice.asset`, `EnemySpawn_Acid.asset` — each planet type gets its own spawn table (cloned from `EnemySpawn_Default`'s roster shape so they're independently tunable later).
- `Scripts/Data/PlanetSpawnData.cs` — SO: `PlanetData[] planetTypes`, `int planetCount` (default 3), field bounds (`center`/`halfExtents`, defaulted to the GDD-documented box), `minSeparation` margin multiplier, `maxPlacementAttempts`.
- `Data/PlanetSpawn_Default.asset`.
- `Scripts/Environment/PlanetSpawner.cs` (`Manatite.Environment`, new folder+asmdef-covered under `Manatite.Runtime`) — `MonoBehaviour`, runs once in `Start`: spawns `planetCount` planets, each an independently-random type (duplicates allowed), random position via rejection sampling inside the field bounds (retrying up to `maxPlacementAttempts` on overlap), random scale in `[minScale,maxScale]`, random trigger radius multiplier in `[min,max]`, wires `EnemySpawnZone.spawnData` to the type's table.
- `Prefabs/Environment/Planet.prefab` — root (`Lockable`) + child `Visual` (sphere mesh, `MeshRenderer`, material set at spawn time) + child `SpawnZone` (`SphereCollider` trigger + `EnemySpawnZone`, radius set at spawn time).
- Scene: delete `IcePlanet`, `LavaPlanet`, `AcidPlanet`, and the 7 `Planet_XX_SpawnZone` objects; add one `PlanetSpawner` GameObject wired to `PlanetSpawn_Default`.

## Decisions & defaults
- Assumed **one-time spawn of exactly 3 planets when the scene loads**, not a continuous/endless generator as the player travels (playable field is a fixed bounded box, not infinite) — say if you want ongoing spawning instead.
- Assumed "within clip plane far range" = **within the GDD-documented playfield box** (what `farClipPlane` was actually tuned to cover), not a raw 17000-radius sphere from the origin, which would place planets outside the intended play volume.
- Assumed the 3 slots pick type **independently at random** (uniform over lava/ice/acid) — duplicates allowed, matching "not necessarily one of each."
- Assumed **min/max apply to**: planet scale, encounter-trigger radius multiplier, and placement (rejection-sampled position with a minimum separation so scaled planets don't overlap). Spawn count itself stays fixed at 3 per your instruction, not randomized.
- New per-type spawn tables start as clones of the existing default roster (Basic/Medium/Large counts) — architecture is the deliverable; rebalancing per planet type is a follow-up.
- Existing mobility-test `Planet_04/05/06/07` obstacles and the `PlayfieldBounds` bug are left untouched (out of scope).

## Out of scope
- Fixing `PlayfieldBounds`'s undersized collider.
- Per-type unique enemy rosters/balance (tables start identical, just architecturally separate).
- Pooling/despawning planets at runtime (they spawn once, like today).
- Balance Configurator integration for the new SOs.

## Acceptance checks
- Entering Play Mode shows exactly 3 planets, sizes/positions varying, scattered through the field (not clustered at the old 3 fixed spots).
- Re-entering Play Mode multiple times produces different type combinations, including repeats of the same type.
- Flying into a spawned planet's trigger radius spawns enemies matching that planet type's table.
- No console errors on scene load or planet spawn.
