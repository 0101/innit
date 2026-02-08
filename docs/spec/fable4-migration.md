# Fable 4 Migration Completion

## Goals
- Upgrade to .NET 9 (SDK + test project)
- Replace webpack 4 with Vite + `dotnet fable` (standard approach)
- Update CI/CD to .NET 9, Node 20+, Vite build
- Restore web worker for solver (eliminates main-thread UI freezes)

## Expected Behavior
- `dotnet test test` passes on .NET 9
- `dotnet fable src && npx vite build` produces working production build in `deploy/`
- Dev server starts via `dotnet fable watch src --run npx vite` without `NODE_OPTIONS` hack
- Solver runs in web worker — no UI freezes during auto-solve
- GitHub Actions builds and deploys successfully
- `/verify` Playwright suite passes 17/17

## Technical Approach
- **Build tooling**: Vite + `dotnet fable` (NOT `vite-plugin-fable` — alpha, no workers)
- **Shared project**: `shared/Shared.fsproj` containing pure logic (Types, Utils, Mechanics, Solver) — referenced by both `src/App.fsproj` and `worker/Worker.fsproj`. Console.fs stays in `src/` (browser dependency, not used by worker)
- **Worker**: Separate `worker/Worker.fsproj` compiled with `dotnet fable worker -o worker-out`
- **Worker bundling**: Vite native `new Worker(new URL(..., import.meta.url), { type: "module" })` -- must specify `type: "module"` since Fable output uses ES imports
- **Worker serialization**: `postMessage` uses structured clone which cannot transfer Fable `Set` (contains compare functions) or Fable `Record` class instances. Solution: convert `GameState.Pieces` from `Set` to plain array via `WireGameState` type before posting, reconstruct with `JSON.parse(JSON.stringify(...))` to strip class prototypes, and manually reconstruct Fable types from plain objects on the receiving side
- **Deploy action**: v4 with `GITHUB_TOKEN` (standard, no PAT required)

## Decisions
- Vite over webpack 5: Less config, native worker support, community direction
- `dotnet fable` over `vite-plugin-fable`: Stable Fable 4.28.0, worker support, maintained
- .NET 9 over .NET 10: .NET 10 still in preview, upgrade later when it reaches LTS
- Separate `.fsproj` for worker: Only viable approach for Fable web workers
- Shared project over duplication: Worker can't reference App.fsproj (Fable compiles all files, including browser-dependent ones that break in worker context)
- No serialization library: Both sides are Fable JS, but still need manual conversion at postMessage boundary due to Fable's Set/Record types not being structured-clonable
- Worker must use `{ type: "module" }`: Fable-compiled JS uses ES module imports; Vite defaults to classic workers which can't use `import`
