# Player Test Scene

## Goal
An isolated scene containing only the player ship, where every player-facing
action (movement, weapon fire, target lock, taking damage, dying, chip
equip/stat effects) works exactly as in the real game, for balance data
analysis — without save/load.

## Deliverables
- New scene: `Assets/_Project/Scenes/Sandbox/PlayerTest.unity`, created by
  **file-copying** `Outer Space.unity` (not hand-authored YAML) so every
  component reference/GUID on the player stays exactly as-is, then stripping
  non-player content via Unity MCP `manage_gameobject` deletes.
- Scene keeps these roots, unmodified: `Directional Light`, `NavePrincipal`
  (ShipController, Rigidbody, TargetLock, Weapon, Health, PlayerDeath,
  DamageScreenEffect, PlayerInventory, ShipAudio + child CameraMount/Main
  Camera/Model/FirePoint/AimPoint), `HUD`, `EventSystem`, `InventoryCanvas`,
  `InventoryController`, `AudioManager`, `MusicDirector`.
- Scene removes: `Planets` (+ PlanetSpawner + planet instances), `PlayfieldBounds`,
  `PauseMenuCanvas` (Save/Load buttons + Settings), `RunSaveController`, and
  the stray leftover `ParticleExplosion(Clone)` currently saved in the source
  scene.
- Not added to Build Settings (matches the `Sandbox/` convention: throwaway,
  not shipped).

## Decisions & defaults
- Assumed location `Assets/_Project/Scenes/Sandbox/PlayerTest.unity` (this is
  exactly what `Sandbox/` is for per CLAUDE.md — say if you want it at the
  top-level `Scenes/` instead).
- "Chips" = the existing passive stat-modifier system (equip via
  `InventoryCanvas`/`InventoryController`, applied through
  `PlayerInventory.GetModifiedFloat/Int` into `Weapon`/`ShipController`). There's
  no separate "activate a chip" input in the codebase today, so nothing extra
  is needed to make that "work" beyond keeping the inventory UI.
- `RunSaveController` is dropped entirely (it only ever does anything when
  `PendingLoad.Data` is set by a menu, so it's dead weight here) — this is the
  "excluding save and load" part.
- `PauseMenuCanvas` dropped wholesale rather than gutted, since its only
  content is Save/Load (excluded) and Settings (not requested).
- `ObjectPooler` needs no scene object — it lazily self-creates, so pooled
  projectiles/VFX work automatically.

## Out of scope
Enemies, planets/wave spawning, save/load, pause/settings menu.

## Acceptance checks
- Scene opens standalone; hierarchy matches the "keeps" list above exactly.
- Play mode: ship moves/rotates/fires, target-lock works, taking damage drops
  `Health` and shows the damage-crack overlay, dying triggers `PlayerDeath` —
  no console errors (specifically nothing about `PendingLoad`/`SaveFile`/
  `PlanetSpawner`, confirming the strip didn't leave a dangling reference).
- Pressing the Inventory key opens equip/inventory grids; dragging a chip
  into an equip slot visibly changes weapon stats (fire rate/damage) in play.
