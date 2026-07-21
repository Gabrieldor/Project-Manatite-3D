---
name: verify
description: Verify Unity changes actually work - compile check, console errors, and play-mode exercise via Unity MCP. Run after any C# or scene change before reporting done.
---

# Verify Unity changes

Never report a change as complete on the strength of "the code looks right". Verification levels, in order — do the highest one available:

## Level 1 — Compile + console (always required)

1. If Unity MCP tools are available (Editor running): trigger a script refresh/compile if the MCP exposes it, then read the Editor console. Report any errors/warnings **verbatim** and fix errors before proceeding. Clear resolved errors from consideration, re-check after fixing.
2. If MCP is **not** available: do a static self-check (namespaces, usings, asmdef references for any new folder), then explicitly tell the user: *"I could not compile-verify — please check the Unity console and paste any errors."* Never imply it compiled.

## Level 2 — Play-mode exercise (for gameplay/scene changes)

Only when MCP is available and the change affects runtime behavior:

1. Enter Play Mode via MCP.
2. Exercise the acceptance checks from the feature's spec (`Docs/specs/*.md`) where possible: query GameObject state, read console output, check for runtime exceptions (NullReference on wiring is the #1 failure).
3. Exit Play Mode. **Never leave the Editor in Play Mode.**
4. Report what was observed, not what was intended.

## Level 3 — Scene wiring audit (for prefab/scene work)

After creating or modifying GameObjects/prefabs via MCP, follow the `unity-hierarchy-editor` skill's verification loop: re-query the created objects and confirm every serialized reference is non-null. An unwired `[SerializeField]` is a silent runtime bomb.

## Reporting

End with a short verification statement: what level ran, what passed, what could not be checked. If anything failed, lead with that.
