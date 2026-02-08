module Worker

open Fable.Core
open Fable.Core.JsInterop

[<Emit("self.onmessage = $0")>]
let onMessage (handler: obj -> unit) : unit = jsNative

[<Emit("self.postMessage($0)")>]
let postMessage (msg: obj) : unit = jsNative

let inline readInt (o: obj) (idx: int) : int = o?(idx)

let parsePair (o: obj) : int * int = readInt o 0, readInt o 1

let parsePiece (p: obj) : GamePiece =
    let targets : obj array = p?Targets
    let targetPositions : Position array = targets |> Array.map parsePair
    { Position = parsePair p?Position
      Targets = targetPositions |> List.ofArray }

let parseGameState (raw: obj) : GameState =
    let rawPieces : obj array = raw?WirePieces
    { GridW = raw?WireGridW
      GridH = raw?WireGridH
      EmptySpace = parsePair raw?WireEmptySpace
      Pieces = rawPieces |> Array.map parsePiece |> Set.ofArray }

let encodeResult (solutionType: SolutionType) (solution: Solution) : obj =
    let tag, wireState =
        match solutionType with
        | Complete -> 0, null
        | Partial gs -> 1, Wire.gameStateToWire gs |> box
    createObj [
        "tag" ==> tag
        "wireState" ==> wireState
        "solution" ==> (solution |> List.toArray |> Array.map (fun (x, y) -> [| x; y |]))
    ]

onMessage (fun event ->
    let data : obj = event?data
    let wireGameState : obj = data?(0)
    let timeout : float = data?(1)
    let gameState = parseGameState wireGameState
    let solutionType, solution = Solver.Solve(gameState, timeout)
    postMessage (encodeResult solutionType solution))
