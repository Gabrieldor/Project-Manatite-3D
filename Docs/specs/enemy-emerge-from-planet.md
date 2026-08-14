# Enemy emerge from planet

## Goal
Encounter spawns should feel like an ambush from behind the planet: enemies burst
into view from the planet's far side (opposite the player) and rush outward
before settling into their configured pursuit speed, instead of popping in
scattered around the player.

## Deliverables
- `Assets/_Project/Scripts/Data/EnemyData.cs` — add `emergeSpeed` (float, m/s)
  and `emergeBurstDistance` (float, m) tunables + properties.
- `Assets/_Project/Scripts/Enemies/EnemyController.cs` — add an emerge state:
  straight-line radial burst away from the planet center at `EmergeSpeed`,
  ends once the enemy has traveled `EmergeBurstDistance` beyond its own spawn
  point, then hands off to existing pursuit (steer + `MoveSpeed`) unchanged.
- `Assets/_Project/Scripts/Enemies/EnemySpawnZone.cs` — `SpawnOne` computes
  spawn position on the planet's far side (opposite the player, measured once
  at trigger time) instead of a random sphere around the player; passes
  planet center to `EnemyController.Initialize`.

## Decisions & defaults
- Assumed "other side of the planet" = opposite the player→planet-center
  direction at the moment the encounter triggers (not continuously updated
  per-enemy).
- Assumed spawn points should cluster with some spread rather than stack
  exactly — sampled in a ~35° cone around the back-direction, using the
  existing `MinSpawnRadius`/`MaxSpawnRadius` band for offset magnitude (this
  is already 500-1000 units for the shipped `EnemySpawnData` assets, so the
  emerge exit threshold has to be measured from each enemy's own spawn point,
  not from the bare planet surface — a surface-relative clearance is already
  satisfied at spawn and gives a zero-length "burst").
- Assumed emerge motion is a straight radial line away from planet center
  (no steering) to guarantee no mesh clipping, facing direction of travel.
- Assumed emerge burst speed default 35 (vs. default `MoveSpeed` 12) and
  burst distance default 150m — both tunable per enemy type like every other
  stat.
- The spawn *trigger* volume (`SphereCollider` radius, `OnTriggerEnter`) is
  unchanged — this only changes where enemies appear and their first moments
  of motion.

## Out of scope
- Burst VFX/trail.
- Wave timing changes.
- Any enemy type not driven by `EnemyController`.

## Acceptance checks
1. Triggering an encounter spawns all enemies clustered behind the planet,
   not scattered near the player.
2. Enemies visibly rush outward fast immediately after spawn, then noticeably
   slow to their configured `MoveSpeed` once clear of the planet.
3. Normal pursuit/combat resumes correctly after the burst ends.
4. No enemy clips through the planet mesh during emerge.
