module Worker

open Fable.Core
open Fable.Core.JsInterop

[<Emit("self.onmessage = $0")>]
let onMessage (handler: obj -> unit) : unit = jsNative

[<Emit("self.postMessage($0)")>]
let postMessage (msg: obj) : unit = jsNative

onMessage (fun event ->
    let data : obj = event?data
    let wireGameState : obj = data?(0)
    let timeout : float = data?(1)
    let gameState = Wire.parseGameStateFromJs wireGameState
    let solutionType, solution = Solver.Solve(gameState, timeout)
    postMessage (Wire.encodeResultToJs solutionType solution))
