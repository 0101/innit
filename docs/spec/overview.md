# INNIT — Project Overview

## What It Is

A sliding-tile puzzle that spells "INNIT". A grid of random character tiles fills the screen, with five special title pieces (I, N, N, I, T) scattered among them. An A* solver running in a Web Worker automatically rearranges the title pieces into position after 2 seconds of inactivity. Users can click anywhere to slide tiles around.

## Architecture

F# compiled to JavaScript via Fable 4, using Elmish (functional Model-View-Update over React). The solver runs in a Web Worker to keep the UI responsive.

```
src/
  Shared/     Pure domain logic — referenced by both App and Worker
  App/        Elmish application (UI, state management, animations)
  Worker/     Web Worker entry point (receives puzzle state, returns solution)

test/         xUnit + FsCheck property-based tests (.NET compiler)
e2e/          Playwright browser tests (Fable compiler + real browser)
```

Two compilers see this code: .NET (for `dotnet test`) and Fable (for the browser build). They can disagree, so both must pass.

## Project Structure

### Shared/ (pure logic, no browser dependencies)

| File | Purpose |
|------|---------|
| Types.fs | Domain types: grid coordinates (`Sq` measure), pieces, game state, solver types, UI state, messages |
| Mechanics.fs | Grid operations: random path generation, path segmentation, piece swapping, position randomization |
| Solver.fs | A* best-first search with timeout; returns Complete or Partial with best state for chaining |
| Utils.fs | `mapSnd`, `coordsToPosition` |

### App/ (browser UI)

| File | Purpose |
|------|---------|
| Config.fs | Constants: grid size, animation speed, solver timeouts, character sets |
| Console.fs | `info`/`warn` — logging in DEBUG, no-ops in RELEASE |
| Init.fs | Creates initial grid, places title pieces, ensures puzzle isn't already solved |
| Update.fs | Elmish message handler — the core state machine (click, animate, idle, solve, shuffle) |
| Rendering.fs | React view: renders grid pieces and social-link overlays |
| Animations.fs | Frame-by-frame piece interpolation (0.4 sq/frame at 40 FPS) |
| App.fs | Elmish program setup, subscription wiring |

### Worker/

| File | Purpose |
|------|---------|
| Worker.fs | `onMessage` handler: deserializes game state, runs solver, posts result back |

## Key Concepts

### Phase System

The app progresses through three phases:

1. **Intro1** — Title pieces are scrambled, solver animates them into position (no user interaction)
2. **Intro2** — 1-second "light-up" CSS animation reveals social-link overlays on highlighted pieces
3. **RegularOperation** — User can click to move tiles; solver auto-runs after 2s idle

### Solver Pipeline

1. User stops interacting for 2 seconds
2. App sends `GameState` directly to worker via `postMessage`
3. Worker parses the structured-clone'd object, runs A* search with timeout (starts at 0.5s)
4. If **Complete**: solution moves queued for animation
5. If **Partial**: best state returned, timeout increased (up to 2.5s), solver re-invoked with partial state

### Piece Substitution (Duplicate Letters)

I, N appear twice each. Rather than assigning each piece a unique target, duplicate letters **share the same target set**. Any permutation of identical letters is valid. The solver treats them as interchangeable.

### Animation System

- Paths are segmented into axis-aligned segments (horizontal or vertical runs)
- One segment processed per animation cycle
- Each `Tick` (25ms) advances pieces 0.4 squares toward their target
- Grid array is mutated in-place during animation (justified: Elmish is single-threaded)

### Worker Communication

Web Workers use structured clone for `postMessage`. F# records compile to plain JS objects and arrays survive structured clone, so `GameState` is sent directly without a serialization layer.

**Key design choice**: `GamePiece.Targets` uses `Position array` (not list) because F# lists don't survive structured clone. `[<CustomEquality; CustomComparison>]` on `GamePiece` ensures arrays compare by contents, preserving Set deduplication in the solver.

`SolutionType` DU doesn't survive structured clone (prototype chains lost), so the worker encodes responses as plain JS objects with `tag`/`state`/`solution` fields.

### Elmish Subscription Gotcha

Elmish 4.4.0's `Program.withSubscription` **replaces** the subscription function (does not compose). Multiple calls silently discard earlier subscriptions. All subscriptions (resize, solver response, console API) must be batched in a single `Sub.batch` call.

## Configuration (Config.fs)

| Constant | Value | What It Controls |
|----------|-------|------------------|
| SquareSize | 100px | Grid cell size |
| AnimationStep | 0.4 sq | Movement per frame |
| TargetFPS | 40 | Animation refresh rate |
| IdleSeconds | 2.0s | Delay before auto-solve |
| SolverInitialTimeout | 0.5s | First solver attempt |
| SolverMaxTimeout | 2.5s | Maximum solver timeout |
| SolverTimeoutStep | 0.5s | Timeout increase per partial result |

## What the Tests Verify

### Unit Tests (dotnet test)

**Property-based tests (FsCheck)** — generate random inputs, verify invariants hold:

- **Pathfinding**: random paths have correct length, start/end, continuity; self-paths are empty
- **Path segmentation**: segments recombine to original path
- **Location randomization**: no overlaps, respects margins and spacing constraints
- **Solver correctness**: single-piece solves, already-solved returns empty, no backtracking in solutions
- **Chained solving**: repeated partial solves eventually complete
- **Shared targets**: duplicate-letter piece substitution works

### E2E Tests (npm run test:e2e)

**Playwright browser tests** — build the app, serve it, drive a real browser:

1. **Initial Load** — page returns 200, title is "INNIT", Elmish mounts, grid renders pieces with correct positioning
2. **Intro Sequence** — no highlights/overlays during Intro1; after ~10s solver animates title pieces; items appear after intro
3. **User Interaction** — mouse click triggers piece animation
4. **Shuffle + Solve** — `window.shuffle` exposed; after shuffle, solver arranges letters correctly within 20s
5. **Web Worker** — worker process is created
6. **Health** — no JS console errors
7. **Mobile** — app renders on 375x667 viewport; mobile grid is smaller than desktop
