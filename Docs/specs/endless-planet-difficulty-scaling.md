# Endless planet regeneration + difficulty scaling

## Goal
When the player clears every planet currently in the field, a fresh field of planets spawns around their current position, and the run gets permanently a little harder: more enemies per encounter, and enemies that are tougher, hit harder, shoot faster, and move faster. Difficulty persists in the save so reloading a run keeps its accumulated toughness.

## Reuse vs create
- **Reuse**: `PlanetSpawner` (placement/spawn logic), `PlanetSpawnData`/`PlanetData` (unchanged), `EnemySpawnZone`/`EnemySpawnData`/`EnemyData` (unchanged — no new SO fields), `SaveData`/`RunSaveController`/`SaveFile` (extended, not restructured).
- **Create**: `Manatite.Core.DifficultyScale` (static, in-memory current tier + multiplier helpers), a `difficultyTier` field on `SaveData`, and regeneration logic in `PlanetSpawner`.

## Deliverables
- `Assets/_Project/Scripts/Core/DifficultyScale.cs` — static holder for the current run's `Tier` (int) and derived multiplier `Mathf.Pow(1.1f, Tier)`. Reset to 0 on domain reload; set explicitly by `RunSaveController` on load.
- `Assets/_Project/Scripts/Core/SaveSystem/SaveData.cs` — add `public int difficultyTier;` to `SaveData`.
- `Assets/_Project/Scripts/Core/SaveSystem/RunSaveController.cs` — on load, set `DifficultyScale.Tier = PendingLoad.Data.difficultyTier`; on `Save()`, write `data.difficultyTier = DifficultyScale.Tier`.
- `Assets/_Project/Scripts/Environment/PlanetSpawner.cs` — refactor the batch-spawn loop in `Start()` into a reusable method taking a center override; after a planet's `OnEncounterCleared` fires, check if every currently-tracked `PlanetState` is `Beaten`; if so, destroy the current planet batch, increment `DifficultyScale.Tier`, spawn a new batch of `spawnData.PlanetCount` planets centered on the player's current position, then invoke `OnPlanetBeaten` (existing autosave hook) so the new layout + tier land in the save.
- `Assets/_Project/Scripts/Enemies/EnemySpawnZone.cs` — apply `Mathf.CeilToInt(entry.count * DifficultyScale.Multiplier)` instead of raw `entry.count` when spawning/tracking each roster entry.
- `Assets/_Project/Scripts/Enemies/EnemyController.cs` — `ApplyData()` sets max health via `Mathf.CeilToInt(data.MaxHealth * DifficultyScale.Multiplier)`; `FixedUpdate` uses `data.MoveSpeed * DifficultyScale.Multiplier` (no rounding, float stat).
- `Assets/_Project/Scripts/Enemies/EnemyWeapon.cs` — cooldown uses `data.FireRate * DifficultyScale.Multiplier` (no rounding); `Fire()` uses `Mathf.CeilToInt(data.Damage * DifficultyScale.Multiplier)`.

## Decisions & defaults
- Assumed **compounding, recomputed-from-base each read**: multiplier = `1.1^Tier`, applied fresh to the base `EnemyData`/`EnemySpawnData` value every time (not chained roundings), so there's no accumulating rounding drift across cycles.
- Assumed **round up only where the field is an `int`**: enemy spawn count, max health, damage → `Mathf.CeilToInt`. Fire rate and move speed are floats and scale by the exact multiplier with no rounding (confirmed with you — literal ceiling on floats would spike small values like a 1.0 fire rate to 2.0 on the very first cycle).
- Assumed **regeneration replaces the field** rather than adding to it: the fully-cleared batch (all loot/shops already resolved) is destroyed and a same-size new batch spawns centered on the player's current position, reusing the existing `PlanetSpawnData` (same type roster, same `PlanetCount`, same placement rules) — just recentered instead of using the fixed `FieldCenter`. Planet IDs restart at 0 per batch since the save only ever serializes whatever `PlanetState`s currently exist in the scene.
- `DifficultyScale` lives in `Core/` as a static class (no MonoBehaviour, no scene wiring) since every consumer already reads `EnemyData`/`EnemySpawnData` fields directly at point-of-use (not cached once at spawn) — multiplying at that same point of use means zero changes to `EnemyData`/`EnemySpawnData` themselves, and nothing mutates the shared ScriptableObject assets.
- Old saves without a `difficultyTier` field deserialize it as `0` (JsonUtility default) — no migration needed.

## Out of scope
- No UI indicator for current difficulty tier (not requested).
- No cap on tier growth or on scaling (stats/counts grow indefinitely as designed).
- No changes to loot tables, shop chance, or planet visuals per tier.
- No changes to `EnemyData`/`EnemySpawnData`/Balance Configurator — scaling is applied at runtime read, not baked into assets or exposed as an editable field.

## Acceptance checks
1. Clearing the last unbeaten planet in the field destroys all current planet objects and spawns a new full batch centered on the player, and the Balance Configurator / console shows no errors.
2. After one regeneration cycle, a freshly spawned enemy has ~10% more max health and deals ~10% more damage than the same `EnemyData` asset's base values (verify via `read_console`/inspecting a spawned instance in Play mode).
3. An `EnemySpawnData` entry with `count = 3` spawns 4 enemies (`ceil(3 × 1.1)`) after one cycle.
4. Saving mid-run and reloading restores the same `difficultyTier`, and newly spawned enemies after reload still reflect that tier's scaling (not reset to tier 0).
