# SFX Intro + Loop Playback

## Goal
Some SFX (engine hum, alarm, ambient loop) need an attack/intro that plays once, then a
seamless repeating region — e.g. play 0–10s once, then loop 10–15s forever until stopped.
A designer should be able to set the loop-in/loop-out points visually with sliders in the
Inspector, without hand-editing seconds by trial and error.

## Reuse vs create
- Reuse: `AudioManager` (SFX voice pool pattern), `SfxCue` enum, `AudioLibraryData` lookup
  pattern (`TryGetX`), existing `AudioCueData.GetVolume/GetPitch` randomization convention.
- Create: a single-clip loop-capable data type, a library entry list for it, playback
  scheduling in `AudioManager`, and an Inspector drawer with the loop-region slider.

## Deliverables
1. `Scripts/Data/AudioCueData.cs` — add a `loop` checkbox and `loopStart`/`loopEnd` (seconds)
   directly to the existing struct, alongside its clip pool and volume/pitch ranges.
   `HasLoopRegion` true when `loop` is on and `loopEnd > loopStart`.
2. `Scripts/Audio/AudioManager.cs` — add:
   - `PlayLoopingSfx(SfxCue, Vector3 pos, bool spatial, float volumeScale = 1f, Transform followTarget = null) -> LoopingSfxHandle`
   - `StopLoopingSfx(LoopingSfxHandle)`
   - a small pool of loop voices (default 4, tunable like `sfxVoiceCount`), each backed by
     two ping-ponged `AudioSource`s so the loop segment can be rescheduled gaplessly via
     `AudioSettings.dspTime` + `PlayScheduled`/`SetScheduledEndTime` while the currently
     playing segment finishes.
3. `Scripts/Editor/AudioCueDataDrawer.cs` — `PropertyDrawer` for `AudioCueData` rendering the
   clip list/volume/pitch fields as usual, plus the `loop` checkbox, revealing a `MinMaxSlider`
   (loop-in/loop-out) bounded to clip 0's length with numeric seconds fields when checked.

## Decisions & defaults
- Loop points stored in **seconds**, not normalized 0–1 (say if you'd rather normalize).
- Loop fields live directly on `AudioCueData` (used by every entry in `sfxCues`), not a
  separate type/list — any cue can be one-shot, whole-clip loop, or intro+region loop.
  With multiple clips in the pool, loop-region bounds come from clip 0; keep loop-enabled
  cues to a single clip for predictable loop points.
- Pitch is randomized once per `PlayLoopingSfx` call and held fixed for that instance's
  life — required because the scheduling math depends on a fixed playback rate.
- `StopLoopingSfx` stops immediately, no fade-out (say if you want one).
- No waveform rendering behind the slider — just a bounded MinMaxSlider + seconds fields.
  A rendered waveform is a separate, bigger editor feature.
- Optional `followTarget` lets a loop track a moving emitter (e.g. enemy engine) each frame;
  omit for a static position, matching current one-shot 3D behavior.

## Out of scope
- `ShipAudio`'s thruster loop is not migrated to this system.
- Music-track intro+loop (this is SFX-only, per the request).
- Waveform preview, cross-fade on stop.

## Acceptance checks
- Loop points set via the slider serialize into the saved `.asset` (grep the YAML).
- A cue with a loop region plays intro once, then loops seamlessly (no audible click) —
  verified by ear in a test scene.
- A cue with no loop region (0,0) plays as a plain full-clip loop, unchanged from today.
- `StopLoopingSfx` stops audio immediately; a stale/double-stopped handle is a no-op.
