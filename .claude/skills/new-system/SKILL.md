---
name: new-system
description: Scaffold a new gameplay system (weapons, waves, health, movement, scoring, etc.) following this project's ScriptableObject-driven architecture. Use when adding any new mechanic or system from scratch.
---

# Scaffold a gameplay system

Every gameplay system in this project follows the same shape. When creating one, produce all three parts — a system missing its config asset or its wiring notes is incomplete.

## The pattern

1. **Config** — a `ScriptableObject` in `Assets/_Project/Scripts/Data/`, holding every tunable number, with `[CreateAssetMenu(menuName = "Manatite/<Category>/<Name>")]`. Sensible defaults in field initializers so a freshly created asset already plays.
2. **Behaviour** — MonoBehaviour(s) in the matching `Scripts/<Domain>/` folder that consume the config via a `[SerializeField] private <Config> config;` reference. No tunables hardcoded. Cache all component lookups in `Awake`.
3. **Integration** — whichever applies:
   - Anything spawned repeatedly (projectiles, enemies, pickups, VFX) registers with the pooling system in `Scripts/Core/` (create the pooler first if it doesn't exist yet — it's the foundation).
   - Cross-system communication via C# events on the owning component (`public event Action<int> OnScoreChanged;`), not direct references from gameplay to UI.

## Steps

1. Check `Scripts/Core/` and sibling domains for utilities to reuse (pooler, timers, health) — extend, don't duplicate.
2. Write config class + behaviour class(es). New top-level domain folder ⇒ confirm it's referenced by `Manatite.Runtime.asmdef` (folders under the asmdef are automatic; only editor code needs the separate `Manatite.Editor.asmdef`).
3. Create the default `.asset` config instance and prefab wiring **via Unity MCP** when available (follow `unity-hierarchy-editor` verification). If MCP is unavailable, deliver the scripts plus a short numbered "in-Editor setup" checklist for the user (create asset via menu, add component, drag references) — do not hand-write `.asset`/`.prefab` YAML.
4. Run `/verify`.

## Example skeleton

```csharp
// Scripts/Data/WeaponConfig.cs
using UnityEngine;

namespace Manatite.Data
{
    [CreateAssetMenu(menuName = "Manatite/Weapons/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        [SerializeField] private float fireRate = 8f;
        [SerializeField] private float projectileSpeed = 60f;
        [SerializeField] private int damage = 10;

        public float FireRate => fireRate;
        public float ProjectileSpeed => projectileSpeed;
        public int Damage => damage;
    }
}
```
