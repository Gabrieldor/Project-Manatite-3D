# Planet Cleared Ring

Once a planet's enemy encounter is fully cleared, a thin blue ring appears around it — a
permanent, at-a-glance signal that the player has already beaten this planet.

## Deliverables

- `Assets/_Project/Scripts/Environment/PlanetClearedRing.cs` (`Manatite.Environment`) —
  builds a flat annulus mesh once (shared across all planet instances), renders it via
  `MeshRenderer`, starts hidden, exposes `Show()`.
- `Assets/_Project/Materials/VFX/PlanetClearedRing.mat` — reuses the project's existing
  `Universal Render Pipeline/Particles/Unlit` shader (already used by `HitMarker.mat` /
  `LightningArc.mat`), transparent + double-sided + no ZWrite, tinted blue.
- `Planet.prefab`: new inactive child `ClearedRing` under `Visual`, with
  `MeshFilter` + `MeshRenderer` + `PlanetClearedRing`.
- `PlanetSpawner.cs`: calls `ring.Show()` when `zone.OnEncounterCleared` fires, and
  immediately on load if the saved planet's `beaten` flag is already `true`.

## Decisions & defaults

- Assumed a flat circular ring (Saturn-style), inner/outer radius ~0.6/0.72 in Visual's
  local unit-sphere space, sized in `Visual`'s local space so it auto-scales with each
  planet's randomized size (no per-instance math needed).
- Assumed blue = `(0.25, 0.6, 1, 0.85)`, unlit, double-sided (no billboarding/facing
  logic — planets don't rotate at runtime, so a static double-sided disc reads fine from
  any angle).
- Assumed no animation (no fade-in/spin/pulse) — appears instantly, matches the project's
  "simplest performant option" precedent.
- Assumed plain `[SerializeField]` tunables on the component (radius/segments/color)
  rather than a new ScriptableObject — this is a single cosmetic signal, not a
  per-planet-type gameplay stat.

## Out of scope

- Per-planet-type ring colors/styles.
- Animation or particle effects.
- Any change to `EnemySpawnZone` clearing logic (already permanent, see prior change).

## Acceptance checks

1. Freshly spawned planet shows no ring.
2. Clearing every wave at a planet makes a blue ring appear around it immediately, sized
   to the planet.
3. Loading a save where a planet was already beaten shows its ring on spawn, no re-fight
   needed.
4. No new console errors/warnings.
