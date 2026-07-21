# Project Manatite 3D — 3D Space Shooter

Unity **6000.5.4f1**, URP 17.5, **new Input System** (`Assets/InputSystem_Actions.inputactions`), uGUI for UI. Root namespace: `Manatite`.

Design source of truth: `Docs/GDD.md`. Task list: `Docs/BACKLOG.md`. Keep both updated as features land.

## Golden rules

1. **All game content goes under `Assets/_Project/`** (create folders on first use — see Layout). Never add files loose in `Assets/` root. `Assets/TutorialInfo` is template junk; ignore it (deletable on request).
2. **Never read or edit**: `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `*.csproj`, `*.slnx` — Unity regenerates them and they burn tokens for nothing.
3. **Never hand-create `.meta` files** — Unity generates them. When moving/renaming an asset from the shell, move its `.meta` alongside it.
4. **Prefer Unity MCP tools** (`manage_gameobject`, `manage_scene`, etc., available when the Unity Editor is running) for scene/prefab/component work. Hand-edit `.unity`/`.prefab`/`.asset` YAML only as a last resort, with minimal diffs. If MCP tools are not available, say so and ask the user to open the Editor rather than guessing at YAML.
5. **Never claim "done" without verification** — run `/verify` after any code or scene change (compile check + console errors at minimum).
6. Don't `git commit`/`push` unless asked.

## Prompt handling (important)

- For any **vague, large, or multi-part** request, run `/spec` first: restate the request as a short concrete spec with sensible arcade-space-shooter defaults, surface only *blocking* questions, then implement. Do not ask about things a default covers.
- For small unambiguous asks ("rename X", "fix this error"), skip the spec and just do it.
- If a request conflicts with the GDD or existing architecture, flag the conflict before coding.

## Layout

```
Assets/_Project/
  Scripts/            Manatite.Runtime.asmdef
    Core/             game state, bootstrapping, pooling, utilities
    Player/  Enemies/  Weapons/  Combat/   (health, damage, projectiles)
    Data/             ScriptableObject definitions
    UI/
  Scripts/Editor/     Manatite.Editor.asmdef (editor-only)
  Prefabs/  Scenes/  Data/  Materials/  VFX/  Audio/  UI/
  Shaders/            .shadergraph / .shader source
  Textures/           texture maps
  Models/             source geometry, grouped by subject
    Ships/Player/  Ships/Enemies/  Props/  Environment/
  Scenes/Sandbox/     throwaway test scenes (not shipped)
```

`Assets/_Project/Data/` holds ScriptableObject *assets*; `Scripts/Data/` holds their class definitions.

Art asset rules: keep a model's `.mtl` beside its `.obj` (Unity reads it at import to generate materials); texture maps live in `Textures/`, not beside the model. OBJ carries no rig, animation, or hierarchy — use FBX/glTF for anything skinned or animated.

## Code conventions

- One public type per file; filename == type name. Namespaces mirror folders (`Manatite.Weapons`).
- Fields: `[SerializeField] private` + property/method access. No public mutable fields.
- **Data-driven**: every tunable number (ship stats, weapon damage/fire rate, wave definitions, enemy stats) lives in a ScriptableObject — never hardcoded in a MonoBehaviour.
- **Hot-path discipline**: no LINQ, no string concat, no `GetComponent`/`Find` calls inside `Update`/`FixedUpdate` — cache in `Awake`. Projectiles, enemies, VFX, and audio one-shots go through object pooling (`Core/`); never `Instantiate`/`Destroy` per shot.
- Input **only** through the Input System actions asset — never `UnityEngine.Input` legacy API.
- Movement/physics in `FixedUpdate` via Rigidbody; camera/visuals in `LateUpdate`.
- Decouple with C# events (e.g. `Health.OnDied`); UI listens, gameplay never references UI. Singletons: at most a small `GameManager`/`ObjectPooler` — nothing else.
- Layers & tags: define in one static class (`Core/GameLayers.cs`), no magic strings.

## Token efficiency

- Never read `.unity`/`.prefab`/`.asset` files wholesale — Grep for the specific GameObject name or GUID, or query via MCP.
- Search with Grep/Glob (they respect `.gitignore`, which excludes `Library/` etc.) — don't shell out to recursive `Get-ChildItem`.
- Reuse before writing: check `Scripts/Core/` for an existing pooling/timer/event utility before adding a new one.
- Delegate broad "find where X happens" sweeps to the built-in Explore agent instead of dumping many files into context.
