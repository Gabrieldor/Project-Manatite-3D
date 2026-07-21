---
name: spec
description: Turn a rough or vague feature request into a tight implementation spec before coding. Use for any large, ambiguous, or multi-part gameplay/feature request in this project.
---

# Spec-first feature workflow

Convert the user's raw prompt into a concrete, minimal spec, then implement it. The goal is to spend a few hundred tokens up front to avoid thousands wasted on wrong-direction implementation.

## Steps

1. **Restate the goal** in 1–2 sentences, in game-design terms (what the player experiences), not implementation terms.
2. **Check context** (cheaply): skim `Docs/GDD.md` and `Docs/BACKLOG.md`, and Glob `Assets/_Project/Scripts/` for existing systems this touches. List what will be *reused* vs *created*.
3. **Write the spec** (max ~30 lines) with these sections:
   - **Deliverables** — exact files: scripts (with namespace/folder), ScriptableObject classes + assets, prefabs, scene changes.
   - **Decisions & defaults** — every ambiguity in the prompt, resolved with a sensible arcade-space-shooter default, stated as "Assumed X (say if you want Y)". Do NOT turn these into questions.
   - **Out of scope** — what this deliberately does not include.
   - **Acceptance checks** — 2–4 observable behaviors that prove it works (used later by `/verify`).
4. **Blocking questions only**: if (and only if) a decision would cause significant rework if guessed wrong (e.g., "is this game on-rails or free-flight?"), ask via AskUserQuestion with a recommended option. Everything else: assume and note.
5. **Save the spec** to `Docs/specs/<kebab-feature-name>.md`, then implement it in the same turn (unless the user asked only for a plan).
6. When the feature lands, append a one-line entry to `Docs/BACKLOG.md` under "Done" and update `Docs/GDD.md` if a design decision was made.

## Anti-patterns

- Don't produce a spec longer than the code would be.
- Don't ask questions that have an obvious genre default (fire rate, projectile speed, HP numbers — put them in a ScriptableObject where the user can tune them).
- Don't re-spec tiny requests; if the whole change is < ~30 lines of code, just do it.
