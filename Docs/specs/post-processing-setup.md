# Post-processing setup (Main Menu vs. Gameplay)

## Goal
Give the game a cohesive cinematic look — glow on projectiles/engines, richer color, subtle vignette — with a menu mood distinct from the in-flight mood, using URP's built-in Volume framework (no custom shaders).

## Context found
- URP post-processing is available (`PC_RPAsset` has `postProcessData` assigned, HDR on, MSAA on) but **currently fully unconfigured**: no `Global Volume` exists in either scene, and `Outer Space.unity`'s Main Camera has `m_RenderPostProcessing: 0` (off). Main Menu's camera doesn't even have a `UniversalAdditionalCameraData` yet.
- `PC_Renderer.asset` already has an **unrelated, pre-existing** full-screen renderer feature, `DamageCrackPass` (active) — a custom shader pass, not part of the Volume stack. Not touching it; new Volume effects layer on top independently.
- Two scenes only: `Main Menu.unity` (menu) and `Outer Space.unity` (gameplay). Each already has its own `AudioManager` per scene (established pattern) — a scene-local `Global Volume` follows the same pattern.

## Deliverables
- `Assets/_Project/Data/PostProcessing/PP_MainMenu.asset` — `VolumeProfile`
- `Assets/_Project/Data/PostProcessing/PP_Gameplay.asset` — `VolumeProfile`
- `Global Volume` GameObject added to `Main Menu.unity`, `Is Global = true`, profile = `PP_MainMenu`
- `Global Volume` GameObject added to `Outer Space.unity`, `Is Global = true`, profile = `PP_Gameplay`
- Both scenes' Main Camera: add/enable `UniversalAdditionalCameraData`, `Post Processing = On`

## Decisions & defaults
- **Volume framework, not a custom shader** — matches "simplest performant option" and reuses URP's built-in stack. `DamageCrackPass` stays as its own separate thing.
- **One Global Volume per scene**, not one persistent cross-scene volume — matches the existing per-scene `AudioManager` pattern, and scenes never coexist so there's no blending to manage.
- **Gameplay profile stays restrained** (6DoF free-flight + target-lock needs full readability): Bloom (moderate, for projectile/thruster/explosion glow), Vignette (subtle), Color Adjustments (slight contrast/saturation lift), ACES Tonemapping. **No Motion Blur, no Depth of Field** — assumed off to avoid nausea during fast 6DoF flight and to keep target-lock aiming crisp (say if you actually want either). Chromatic Aberration added but weight 0 by default — left as a hook for a future low-HP damage-feedback pulse, not wired to anything yet.
- **Menu profile can be moodier**: same Bloom/Tonemapping baseline, stronger Vignette, slight Film Grain — no gameplay-readability constraint here.
- **No post-process anti-aliasing** (SMAA/FXAA) — `PC_RPAsset` already has MSAA on; stacking post-AA on top is redundant cost for no visible gain.
- All effect values are just numbers on a `VolumeProfile` asset — tune later in the Editor, no code changes needed to adjust mood.

## Out of scope
- Local/blended volume zones within a scene (e.g. a "near a planet" mood shift)
- Motion blur, depth of field
- Wiring Chromatic Aberration to the low-HP damage-crack effect (future work, hook only)
- Per-platform tuning (PC only, per GDD)

## Acceptance checks
- Both scenes' saved `.unity` show `m_RenderPostProcessing: 1` on the Main Camera and a `Global Volume` referencing the correct profile (grep-verified, not just live state).
- Entering Play Mode from Main Menu shows visible bloom/vignette; loading into Outer Space visibly shifts to the gameplay mood (less grain/vignette).
- HUD/crosshair (Overlay canvas) unaffected — Overlay UI draws after the camera's post-processed image.
- `read_console` clean after the change.
