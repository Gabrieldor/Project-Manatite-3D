# Balance Configurator

## Goal
Give the designer one Editor window where every tunable gameplay number (ship movement,
weapon, health, enemy stats, spawn timing, target lock) can be scaled from 0.1x to 10x of
its current, already-balanced value, instead of hunting through individual .asset Inspectors.

## Context
- All tunables already live in ScriptableObjects under `Assets/_Project/Data/`, defined by
  6 classes in `Scripts/Data/`: `ShipMovementData`, `WeaponData`, `HealthData`, `EnemyData`,
  `EnemySpawnData`, `TargetLockData`.
- No `Scripts/Editor/` folder or `Manatite.Editor.asmdef` exists yet (first editor tool).
- Reused: existing Data classes/assets as-is (no field renames).
- Created: new editor-only asmdef + 2 new scripts + 1 new baseline snapshot asset.

## Deliverables
- `Assets/_Project/Scripts/Editor/Manatite.Editor.asmdef` — editor-only asmdef referencing `Manatite.Runtime`.
- `Assets/_Project/Scripts/Editor/BalanceBaseline.cs` (`Manatite.Editor`) — ScriptableObject
  storing one baseline float per (data asset, field name) pair — the "standard" 1.0x anchor.
- `Assets/_Project/Data/Balance/BalanceBaseline.asset` — the baseline instance. Populated lazily: the
  first time the tool sees a field it hasn't seen before, it snapshots that field's *current*
  value as its standard/1.0x point. Nothing on disk changes until the window is opened.
- `Assets/_Project/Scripts/Editor/BalanceConfiguratorWindow.cs` (`Manatite.Editor`) — `EditorWindow`
  under `Window > Manatite > Balance Configurator`. Three tabs (`GUILayout.Toolbar`):
  **General** (`ShipMovementData`, `WeaponData`, `HealthData`, `TargetLockData` — player-only
  tunables), **Enemies** (`EnemyData`, `EnemySpawnData` — every enemy stat block and every spawn
  roster/table, split out 2026-07-31 so enemy tuning isn't mixed in with player/ship data), and
  **Planets** (`PlanetSpawnData`, `PlanetData`, added 2026-07-31 for procedural planet tuning —
  spawn count/separation/attempts and per-type scale/zone-radius min-max). Each tab finds every
  asset of its types under `Assets/_Project/Data`, groups them in foldouts by asset name, and
  renders one slider per tunable numeric field: range 0.1x–10x, live label showing the resulting
  value, writes back via `SerializedObject`/`SerializedProperty` (Undo-safe, marks the asset
  dirty). Per-field and per-asset "Reset to standard" buttons.

## Decisions & defaults
- **"Standard" = today's values** (the ones already tuned in the 7 .asset files — 100/150/150
  thrust, 10 HP, stopDistance 28/45/95, etc.), captured as the baseline the first time the tool
  touches each field. Not the C# field-initializer defaults in the class files (those are stale
  placeholders from before playtesting/tuning happened).
- Slider scope: only top-level `[SerializeField] float`/`int` fields on the 6 classes. Enums
  (`fireMode`, `spawnMode`), bools (`unlockOnTargetDeath`), `GameObject` prefab refs, and
  `EnemySpawnData.entries` (roster array) are left as plain Inspector fields, no slider.
- Int fields floor at 1 after scaling (so e.g. damage/health can't be scaled to 0).
- One shared baseline asset (not per-field-per-window-session), so it survives Editor restarts
  and is git-tracked like any other Data asset.

## Out of scope
- Runtime/Play-mode UI (Editor-only per your choice).
- Slider control over individual `EnemySpawnEntry` counts inside the roster array.
- Changing the C# field-initializer defaults in the Data classes.

## Acceptance checks
1. `Window > Manatite > Balance Configurator` lists all 7 data assets grouped by type, each
   numeric field showing a slider at 1.0x reflecting today's value.
2. Dragging a slider to 2.0x doubles that field's live value on the asset immediately (visible
   in its normal Inspector too).
3. "Reset to standard" on a field snaps it back exactly to the captured baseline.
4. Values and baselines survive closing the window / restarting the Editor.
