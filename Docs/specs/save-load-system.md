# Save/Load System

## Goal
Player can resume an in-progress run (health, procedural planet layout, per-planet "beaten" flag) from the Main Menu or Pause Menu instead of always starting fresh. Saving is automatic — clearing a planet's enemy encounter writes the save.

## Reuse vs Create
- **Reuse**: `Health` (player HP), `PlanetSpawner`/`PlanetData` (planet spawning), `EnemySpawnZone` (already tracks its own spawn coroutine — extended to also track kills), existing disabled `Save Game`/`Load Game` buttons in `PauseMenu`/`MainMenu` (this task wires them), `JsonUtility` (no new dependency).
- **Create**: JSON save schema + file I/O, a scene-load handoff for "load into this run", a `PlanetState` marker so spawned planets are individually save/restorable, wave-clear detection on `EnemySpawnZone`.

## Deliverables
- `Scripts/Core/SaveSystem/SaveData.cs` (`Manatite.Core.SaveSystem`) — plain `[Serializable]` POCOs: `SaveData` (int version, `PlayerSaveData player`, `List<PlanetSaveData> planets`), `PlayerSaveData` (health, maxHealth), `PlanetSaveData` (planetId, typeId, position, rotation, scale, zoneRadius, beaten). Only fields for state that exists today — no money/inventory placeholders.
- `Scripts/Core/SaveSystem/SaveSystem.cs` — static class: `Save(SaveData)`, `TryLoad(out SaveData)`, `HasSave()`, `DeleteSave()`. `JsonUtility` → single file at `Application.persistentDataPath/save.json` (one slot, always overwritten).
- `Scripts/Core/SaveSystem/PendingLoad.cs` — static class holding a nullable `SaveData` set right before `SceneManager.LoadScene`, consumed once by `RunSaveController.Awake` then cleared.
- `Scripts/Core/SaveSystem/RunSaveController.cs` — one instance living in `Outer Space.unity`. On `Awake`, if `PendingLoad.Data != null`: applies player health and tells `PlanetSpawner` to spawn from the saved list instead of randomizing. Public `Save()`: gathers player `Health` + every `PlanetState` in-scene into a `SaveData` and writes it via `SaveSystem.Save`.
- `Scripts/Environment/PlanetState.cs` — component `PlanetSpawner` attaches to every spawned planet: `PlanetId`, `TypeId`, `Beaten` (now a real flag, set true by the wave-clear hook below).
- Modify `EnemySpawnZone.cs` — at encounter start, snapshot the total enemy count from `spawnData.Entries`; each spawned enemy's `Health.OnDied` decrements a remaining-alive counter (self-unsubscribing, so a pooled instance reused by another zone/encounter later can't leak a stale count). Hits zero → fires a new `event Action OnEncounterCleared`. Leash-fleeing enemies don't count as a kill, so a wave the player flees from never "clears" — matches "clearing the wave" meaning killing it.
- Modify `PlanetSpawner.cs` — add a `SpawnFromSave(List<PlanetSaveData>)` path (exact position/scale/type/zone per entry, no `Random` calls) used when `PendingLoad.Data != null`; both paths attach `PlanetState` and subscribe `zone.OnEncounterCleared` to mark that planet's `PlanetState.Beaten = true` and call `RunSaveController.Save()` (autosave).
- Modify `PauseMenu.cs` — enable Save Game (manual save, calls `RunSaveController.Save()` — kept alongside autosave, e.g. for saving right before quitting); enable Load Game when `SaveSystem.HasSave()` (sets `PendingLoad.Data`, reloads `Outer Space`).
- Modify `MainMenu.cs` — Load Game button enabled only when `SaveSystem.HasSave()`; same load path as above.

## Decisions & defaults
- **Single save slot** (`save.json`), overwritten on every save.
- **Autosave trigger**: an `EnemySpawnZone`'s encounter is "cleared" when every enemy it spawned has been killed (not fled/leashed away). That's the only autosave trigger — no periodic/timer autosave.
- Manual Save Game button kept enabled too, since it's the same one-line call as autosave and was already a stubbed placeholder — say if you'd rather it stay disabled and rely on autosave only.
- **Only state that currently exists is saved**: player health and the planet layout (+ its now-real beaten flag). No money/economy, chips/inventory, or NPC-purchase systems are added — those don't exist in the game yet, so there's nothing to save for them.
- Planet identity across save/load is a **stable per-run `planetId`** (spawn index), not RNG-seed replay.

## Out of scope
Any economy/money, chip/modifier inventory, NPC/shop system, multiple save slots, manual save from the Main Menu (no run is loaded there), save-file corruption recovery UI, cloud saves. (`SaveData` is a plain versioned class — adding fields for the above later is additive and won't need a redesign.)

## Acceptance checks
1. New Game → planets spawn randomly as today (unaffected).
2. Fly into a planet's encounter zone and kill every spawned enemy → `save.json` is written/updated automatically (check file timestamp/contents) with that planet's `beaten: true`.
3. Quit to Main Menu → Load Game (now enabled) reloads `Outer Space` with the *same* planet positions/types/scales/beaten-flags and the *same* player health as last saved.
4. Fleeing an encounter (leashing enemies away without killing them) does **not** trigger a save.
5. Load Game is disabled/hidden on both menus until a save file exists; Save Game in the pause menu is enabled and works on demand.
