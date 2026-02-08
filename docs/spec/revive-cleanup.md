# Revive Branch Cleanup

## Goals
- Add Playwright E2E tests as a proper npm script so we can verify after each change
- Eliminate duplicated wire parsing code between Update.fs and Worker.fs
- Fix PageResize handler losing SolverTimeout on resize (it reinits the entire game)
- Implement proper subscription cleanup in disposables

## Expected Behavior
- `npm test` runs E2E tests against the built app
- Wire parsing logic exists in exactly one place (Wire module in shared/)
- Resizing the browser window reinits game but preserves SolverTimeout
- Subscription disposables actually clean up their event listeners

## Technical Approach

### E2E Tests
- Move `.agents/verify.js` into a proper `e2e/` directory as a Playwright test
- Add `test` npm script to package.json
- The E2E test should build + serve + test in one command

### Deduplicate Wire Parsing
- Both `Worker.fs` and `Update.fs` contain identical `parsePair`, `parsePiece`, and game-state parsing logic
- Extend the existing `Wire` module in `shared/Types.fs` with JS interop parsing functions
- Both Worker.fs and Update.fs import from Wire module
- The Wire module handles: encode GameState → JS object, decode JS object → GameState, encode SolutionType → JS object, decode JS object → SolutionType

### Fix PageResize
- `PageResize` creates a fresh state via `init false` and only copies width/height
- This loses SolverTimeout (and all other accumulated state)
- Reiniting on resize is fine (pieces may go out of viewport), but SolverTimeout should be preserved from existing state

### Clean Up Subscriptions
- Subscription disposables in App.fs are all no-ops
- Implement proper cleanup: unset window.onresize, window.shuffle, worker.onmessage
