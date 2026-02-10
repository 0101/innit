# Eliminate Wire Serialization Layer

## Goals
- Remove `Wire.fs` module entirely
- Remove all manual `parse*FromJs` / `encode*ToJs` functions
- Remove `WireGamePiece` and `WireGameState` types
- Simplify Worker ↔ App communication to pass F# types directly via `postMessage`
- Maintain solver correctness (Set deduplication must still work)

## Expected Behavior
- App sends `GameState` directly to worker via `postMessage` (no manual conversion)
- Worker receives structured-clone'd object, parses it, solves, sends result back
- Solver `seen` Set still deduplicates identical `GameState` values correctly
- All existing tests pass, E2E tests pass, app works identically

## Technical Approach
Switch `GamePiece.Targets` from `Position list` back to `Position array` (arrays survive `postMessage` structured clone). Add `[<CustomEquality; CustomComparison>]` to `GamePiece` so arrays compare by contents (not reference), fixing Set deduplication. This was verified to work in both .NET and Fable 4.28.0 JS output.

With arrays throughout, the Wire conversion layer becomes unnecessary — `GameState` can be sent directly over `postMessage` since records compile to plain JS objects, tuples to arrays, and arrays survive structured clone.

The `SolutionType` DU (`Complete | Partial of GameState`) doesn't survive structured clone (prototype chains lost). Worker response encoding: `createObj ["tag" ==> 0/1; "state" ==> (partialState box or null); "solution" ==> (solution |> List.toArray)]`. Parsing side reads `data?tag`, `data?state`, `data?solution` fields. Solution stays as `Position list` in F# — only converted to array at the postMessage boundary.

## Decisions
- **`Position array` + custom equality** over keeping `Position list` + Wire conversion: `Targets` is the sole reason Wire.fs exists — F# lists don't survive structured clone, everything else does. Switching to array eliminates ~73 lines and an entire module. Add a comment on the field explaining the array choice.
- **Keep `Targets` as array in `GamePiece` type**: The CLAUDE.md warning about arrays breaking Set dedup is addressed by custom equality.
- **Update CLAUDE.md**: Remove the outdated warning about arrays vs lists since custom equality solves it.
- **Direct `postMessage`** over `JSON.parse(JSON.stringify(...))`: Structured clone already deep-copies; the JSON round-trip is redundant.
- **Keep `Solution` as `Position list`**: Only `Targets` needs to be array (for structured clone survival). Solution is created in solver and consumed in app — converting at the single postMessage boundary is simpler than changing the type everywhere.
