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
    { Position = parsePair p?Position
      Targets = targets |> Array.map parsePair }

let parseGameStateFromJs (raw: obj) : GameState =
    let rawPieces : obj array = raw?Pieces
    { GridW = raw?GridW
      GridH = raw?GridH
      EmptySpace = parsePair raw?EmptySpace
      Pieces = rawPieces |> Array.map parsePiece |> Set.ofArray }

let gameStateToJs (gs: GameState) : obj =
    createObj [
        "GridW" ==> gs.GridW
        "GridH" ==> gs.GridH
        "EmptySpace" ==> [| fst gs.EmptySpace; snd gs.EmptySpace |]
        "Pieces" ==> (gs.Pieces |> Set.toArray |> Array.map (fun p ->
            createObj [
                "Position" ==> [| fst p.Position; snd p.Position |]
                "Targets" ==> (p.Targets |> Array.map (fun t -> [| fst t; snd t |]))
            ]))
    ]

onMessage (fun event ->
    let data : obj = event?data
    let gameState = parseGameStateFromJs data?(0)
    let timeout : float = data?(1)
    let solutionType, solution = Solver.Solve(gameState, timeout)
    let tag, state =
        match solutionType with
        | Complete -> 0, null
        | Partial gs -> 1, gameStateToJs gs
    postMessage (createObj [
        "tag" ==> tag
        "state" ==> state
        "solution" ==> (solution |> List.toArray |> Array.map (fun (x, y) -> [| x; y |]))
    ]))
