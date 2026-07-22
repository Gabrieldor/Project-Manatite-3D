# Enemy spawning system

## Goal
Flying near a planet spawns enemies around the ship that pursue it, with the
roster/count/timing fully data-driven.

## Deliverables
- `Scripts/Core/GameLayers.cs` — tag constants (`Player`, `Enemy`).
- `Scripts/Core/ObjectPooler.cs` — generic per-prefab pool (Get/Release), lazy
  self-creating singleton.
- `Scripts/Data/EnemyMovementData.cs` (SO) — moveSpeed, turnRate, stopDistance,
  leashDistance.
- `Scripts/Data/EnemySpawnData.cs` (SO) — enemy roster (`entries[]` of
  prefab+count), `SpawnMode {AllAtOnce, Waves}`, waveSize, timeBetweenWaves,
  min/max spawn radius.
- `Scripts/Enemies/EnemyController.cs` — Rigidbody pursuit toward a target
  Transform; releases to pool past `leashDistance`.
- `Scripts/Enemies/EnemySpawnZone.cs` — trigger volume; spawns per
  `EnemySpawnData` via `ObjectPooler` when the player enters; cooldown re-arm.
- `Prefabs/Enemies/BasicEnemy.prefab` — capsule primitive, Rigidbody,
  `EnemyController`, `Lockable`, tag `Enemy`.
- `Data/EnemyMovement_Basic.asset`, `Data/EnemySpawn_Default.asset`.
- Scene: ship (`NavePrincipal`) tagged `Player`; one `EnemySpawnZone` child
  added under `Planets` per `Planet_*`, trigger radius = 1.5× the planet's own
  world radius, all referencing `EnemySpawn_Default`.

## Decisions & defaults
- Detection = sphere trigger per planet, not per-frame distance polling.
- Trigger check uses `Collider.attachedRigidbody` (not the entering
  collider's own tag) — the ship's physical colliders live on untagged child
  mesh parts (`Model/Cube`, `Model/Cube (1)`), only the root is tagged
  `Player`.
- MVP enemy AI = straight-line pursuit only, no attacks/avoidance.
- Wave mode = time-delayed batches; no wave-clear gating (no death tracking
  yet).
- Zone re-arm = 30s cooldown after an encounter finishes spawning.
- All planets share one `EnemySpawn_Default` asset (5× BasicEnemy, AllAtOnce).

## Out of scope
Enemy attacks, player/enemy health & death, avoidance/flocking, wave-clear
gating, spawn VFX/audio, HUD indicators.

## Acceptance checks (verified in play mode via `execute_code`)
1. Teleporting the ship inside a planet's trigger spawns 5 `BasicEnemy`
   instances around it. ✅ (Planet_01: 5 spawned at configured radius)
2. Enemies pursue and stop at `stopDistance` (~15m). ✅ (closed from ~60-120m
   to ~15m from the ship)
3. Flipping `EnemySpawnData.spawnMode` to `Waves` changes behavior with no
   code edits — confirmed 2 → 4 → 5 over two 8s wave intervals. ✅
4. R3 target lock can acquire spawned enemies — `Lockable.All` count went
   from 7 (planets) to 12 (+5 enemies). ✅
5. No console errors/warnings across all runs. ✅
