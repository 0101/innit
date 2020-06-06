module Solver


open Mechanics
open System





let CreateGameState (state : State) : GameState =
    let ex, ey = state.EmptyField
    {
        //TODO: refactor to use getPieces
        GridW = GridWidth state.Grid
        GridH = GridHeight state.Grid
        EmptySpace = int ex, int ey
        Pieces = set [
            for x, row in state.Grid |> Array.mapi (fun x r -> x, r) do
                for y, field in row |> Array.mapi (fun y f -> y, f) do
                    match field with
                    | Occupied piece when piece.Type = Title -> {
                            Position = x, y
                            Targets = set [ for (tx, ty) in piece.TargetPositions -> int tx, int ty ]
                        }
                    | _ -> () ]
    }


let IsSolved state = state.Pieces |> Set.forall (fun p -> p.Targets |> Set.contains p.Position)


let GetValidMoves state = seq {
    let ex, ey = state.EmptySpace
    if ex > 0 then yield ex - 1, ey
    if ey > 0 then yield ex, ey - 1
    if ex < state.GridW - 1 then yield ex + 1, ey
    if ey < state.GridH - 1 then yield ex, ey + 1
}


let ApplyMove move state =
    let pieceMoved, piecesStaying = state.Pieces |> Set.partition (fun p -> p.Position = move)
    { state with EmptySpace = move
                 Pieces = set [
                    for p in pieceMoved do { p with Position = state.EmptySpace }
                    yield! piecesStaying
                 ] }


let Distance (x1, y1) (x2, y2) = abs (x1 - x2) + abs (y1 - y2)


let DistanceToTarget piece =
    piece.Targets |> Set.map (Distance piece.Position) |> Set.minElement


let DistanceToEmpty state =
    state.Pieces
    |> Seq.filter (fun p -> p.Targets |> Set.contains p.Position |> not)
    |> Seq.sumBy (fun p -> p.Position |> Distance state.EmptySpace)
    //|> (fun d -> 1.1 ** float d)
    //|> int
    |> (fun d -> (d - 2) * 10 |> max 0)


let Score state =
    state.Pieces
    |> Seq.sumBy DistanceToTarget
    |> (*) 100
    |> (+) (DistanceToEmpty state)
    //|> (+) (Random().Next(0, 1000))


let Solve (gameState : GameState) : Async<SolutionType * Position list> = async {
    Browser.Dom.console.info "Start Solve"

    let start = DateTime.Now

    let rec solve iteration queue seen (bestScore, best) = async {
        if iteration % 100 = 0 then do! Async.Sleep 0 // TODO: test sleep 0
        match queue with
        | [] -> return Complete, []
        | (gs, moveHistory)::rest ->
            if Set.contains gs seen then
                return! solve (iteration + 1) rest seen (bestScore, best)
            else
            let newSeen = Set.add gs seen
            let score = Score gs
            let bestScore, best = if score <= bestScore then score, (gs, moveHistory) else bestScore, best
            if (DateTime.Now - start).TotalSeconds > 1.5 then
                Browser.Dom.console.info "Failed to put the board in order!"
                let partialState, partialSolution = best
                return Partial partialState, partialSolution |> List.rev
            else
            if IsSolved gs then return Complete, moveHistory |> List.rev
            else
                let newMoves =
                    GetValidMoves gs
                    |> Seq.map (fun move -> ApplyMove move gs, move::moveHistory)
                    |> Seq.filter (fun (gs', _) -> Set.contains gs' seen |> not)
                let newQueue = Seq.append newMoves rest |> Seq.sortBy (fst >> Score) |> Seq.toList
                return! solve (iteration + 1) newQueue newSeen (bestScore, best)
    }

    let! solution = solve 0 [gameState, []] Set.empty (Score gameState, (gameState, []))
    return solution |> mapSnd (function
                               | [] -> []
                               | xs -> gameState.EmptySpace::xs)
}

let SolutionToPaths =
    List.map (fun (x, y) -> x * 1<Sq>, y * 1<Sq>)
        >> (fun p ->
        try
            // TODO: this should be cleaned up
            SegmentPath p
        with ex ->
            Browser.Dom.console.error(sprintf "Failed path: %A" p)
            [])

let SolveState (state : State) : Async<SolutionType * Path list> = async {
    let! solution = state |> CreateGameState |> Solve
    return solution |> mapSnd SolutionToPaths
}


