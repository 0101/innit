# Migration Status: Fable 2 → Fable 4

## What was done

The project was on Fable 2 (npm-based `fable-compiler`) which is incompatible with the installed .NET 9 SDK. A migration to Fable 4 was performed.

### Completed

1. **global.json** — Updated to target .NET 9.0 SDK
2. **Fable 4 dotnet tool** — Installed as local tool (`dotnet tool install fable` → 4.28.0, see `.config/dotnet-tools.json`)
3. **NuGet packages** — Updated in `src/App.fsproj`:
   - `Fable.Core` 3.1.5 → 4.5.0
   - `Fable.Elmish` 3.0.6 → 4.4.0
   - `Fable.Elmish.React` 3.0.1 → 4.0.0
   - `Fable.Browser.Dom` 1.1.0 → 2.20.0
   - Added `Fable.React` 9.4.0
   - Removed `Feliz.UseWorker` (incompatible with Fable.Core 4.x, unmaintained)
4. **React** — Upgraded from 16 to 18 (required by `Fable.Elmish.React` 4.0.0 which imports `react-dom/client`)
5. **npm packages** — Removed `fable-compiler`, `fable-loader`. Added `@babel/core`, `@babel/preset-env`, `babel-loader@8` (needed because `fable-library-js` uses modern JS syntax like `?.` that webpack 4 can't parse natively)
6. **webpack.config.js** — Simplified to single config (removed second web worker entry), entry point changed from `.fsproj` to `./src/App.fs.js`, added babel-loader rule for `fable_modules`, dev server bound to `0.0.0.0:8080`
7. **package.json scripts** — Updated to `dotnet fable watch src --run npx webpack-dev-server`
8. **F# source changes**:
   - Removed all `Feliz.UseWorker` imports and usage
   - Replaced `Worker<>` state field with simple `SolverTimeout` field
   - Replaced `Cmd.Worker.exec/create/restart` with `Cmd.OfAsync.perform` (solver runs on main thread for now)
   - Replaced `Cmd.ofSub` → `Cmd.ofEffect` (renamed in Elmish 4)
   - Fixed `Program.withSubscription` to return `Sub<Msg>` (Elmish 4 API change)
   - Renamed module from `Workers.Solver` to `Solver`
9. **devcontainer.json** — Added `forwardPorts: [8080]` for automatic port forwarding

### Current state

- **Fable compilation**: Succeeds (`dotnet fable src`) with one deprecation warning about `Cmd.OfAsync.result`
- **Webpack compilation**: Succeeds — "Compiled successfully"
- **App**: Verified working in browser at `http://localhost:8080/`

## What remains

1. **Test project** — `test/test.fsproj` still targets `netcoreapp3.1` and references old packages. Needs updating to `net9.0` and compatible package versions
2. **Solver runs on main thread** — The web worker was removed for simplicity. The solver has built-in timeouts (0.5-2.5s) so it shouldn't block the UI too badly, but for larger grids it could cause jank. Could be restored with a native web worker if needed
3. **Fix `Cmd.OfAsync.result` deprecation warning** — Replace with `Cmd.OfAsync.perform` or `Cmd.OfAsync.either` in `Update.fs` line 89
4. **Consider upgrading webpack 4 → 5** — Would remove the need for `NODE_OPTIONS=--openssl-legacy-provider` and the babel-loader workaround for fable-library-js

## How to run

```bash
# First time: restore tools and packages
dotnet tool restore
dotnet restore src/App.fsproj
npm install

# Start dev server (compile F# then serve)
NODE_OPTIONS=--openssl-legacy-provider npm start

# Or manually: compile then serve separately
dotnet fable src
NODE_OPTIONS=--openssl-legacy-provider npx webpack-dev-server
```

App will be at http://localhost:8080/
