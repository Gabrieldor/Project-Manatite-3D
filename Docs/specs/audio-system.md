# Audio System

Player hears distinct, mixable SFX and music for concrete moments (firing, taking/landing a
hit, ambient thrust) and full music-context switching (menu / idle exploration / battle),
with two independent volume sliders in a new Settings panel that persist between sessions.

## Reused
- `GameLayers.PlayerTag` to tell "hit taken" (target is player) from "hit dealt" (shooter is
  player) apart in the existing shared `ProjectileLauncher` fire/damage path.
- `EnemySpawnZone.OnEncounterCleared` (already exists) for battle→idle switching.
- Main Menu backpanel/button visual style, `TypeLightSans` font, `Button_Settings` (exists,
  currently disabled+unwired) for the new Settings panel.

## Deliverables
- **Scripts/Audio/**: `SfxCue.cs` (enum: Shot, HitTaken, HitDealt, ThrusterLoop), `MusicTrack.cs`
  (enum: None, Menu, Idle, Battle), `AudioManager.cs` (scene-placed singleton, DontDestroyOnLoad
  + duplicate guard; fixed-size pooled `AudioSource[]` for SFX one-shots — no per-shot
  Instantiate; one looping `AudioSource` for music with volume crossfade; static
  `PlaySfx` (2D) / `PlaySfx3D(cue, worldPosition)` (spatial, `spatialBlend=1`) /
  `PlayMusic`/`MusicVolume01`/`SfxVolume01`, persisted via `PlayerPrefs`),
  `MusicDirector.cs` (counts active encounters via `EnemySpawnZone` events, switches Idle⇄Battle).
- **Scripts/Data/**: `AudioCueData.cs` (serializable: clip variations + volume/pitch ranges),
  `AudioLibraryData.cs` (SO: `SfxCue`→`AudioCueData` and `MusicTrack`→clip tables, one shared
  asset `Data/AudioLibrary.asset`, clip slots left empty for you to fill in).
- **Scripts/Player/**: `ShipAudio.cs` — loops the `ThrusterLoop` cue, volume follows ship speed.
- **Scripts/UI/**: `SettingsMenu.cs` — Open/Close panel, two sliders call
  `AudioManager.MusicVolume01`/`SfxVolume01`.
- **Modified**: `ProjectileLauncher.Fire` (Shot sfx), `ProjectileLauncher.ApplyDamage`
  (HitTaken/HitDealt sfx), `EnemySpawnZone` (add `OnEncounterStarted` event), `MainMenu.cs`
  (play Menu track on Start).
- **Scene**: `AudioManager` GameObject in Main Menu + Outer Space (both point at
  `AudioLibrary.asset`); `MusicDirector` in Outer Space; `ShipAudio`+`AudioSource` on the ship;
  Settings panel built in Main Menu (backpanel style, Music/SFX sliders, Back button), wired to
  the existing `Button_Settings`.

## Decisions & defaults
- Assumed **no `AudioMixer` asset** — plain per-source volume scaling (`cueVolume × category
  slider`) is simpler to build via automation and is enough for two sliders (say if you want
  mixer snapshots/ducking later).
- **Enemy-sourced SFX are 3D/spatial** (`PlaySfx3D` at the source position, `spatialBlend=1`,
  default falloff); player-sourced SFX (own gun, own thrust) and hit-feedback cues
  (`HitTaken`/`HitDealt` — arcade hit-marker style, not ambient) stay 2D so they're always
  clearly audible regardless of distance/facing. Concretely: `ProjectileLauncher.Fire`'s `Shot`
  cue plays 3D when the shooter is an enemy, 2D when it's the player (say if you also want
  `HitDealt` positional at the target).
- Assumed **Settings panel lives in Main Menu only** for this pass, not Pause Menu (avoids
  duplicating panel-build work across two scenes today; easy follow-up).
- "Hitting someone" fires only when the **player** is the shooter; "getting hit" fires whenever
  the **player** is the target — covers enemy-shoots-player and player-shoots-enemy, the two
  cases that matter for feedback.
- No actual audio clips are assigned (per your no-AI-generated-media rule) — all `AudioCueData`
  clip arrays ship empty for you to drop clips into later; the system is fully wired regardless.

## Out of scope
- Real audio clips/mixing, snapshots/ducking, spatial attenuation, Pause Menu settings access,
  per-cue randomized clip pools beyond the array-of-clips mechanism itself.

## Acceptance checks
- Play Main Menu: Menu music starts; Settings panel opens from `Button_Settings`, sliders move
  and persist their value after closing/reopening the panel and after an editor session restart.
- Play Outer Space: Idle music plays; entering an `EnemySpawnZone` switches to Battle music;
  clearing the encounter switches back to Idle.
- Firing plays the Shot cue (silent until you assign a clip, but `AudioManager.PlaySfx` fires
  with no console errors); taking damage / landing a hit each fire their own distinct cue call.
