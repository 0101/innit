module Workers.Solver

open Mechanics
open System

open Feliz.UseWorker


let CreateGameState (state : State) : GameState =
    let ex, ey = state.EmptySquare
    { GridW = GridWidth state.Grid
      GridH = GridHeight state.Grid
      EmptySpace = int ex, int ey
      Pieces = GetPieces state.Grid |> Set.map (fun (position, (tx, ty)) -> {
          Position = position
          Target = int tx, int ty
      }) }


let IsSolved state = state.Pieces |> Set.forall (fun p -> p.Target = p.Position)


let GetValidMoves state = seq {
    let ex, ey = state.EmptySpace
    if ex > 0 then ex - 1, ey
    if ey > 0 then ex, ey - 1
    if ex < state.GridW - 1 then ex + 1, ey
    if ey < state.GridH - 1 then ex, ey + 1
}

let ApplyMove move state =
    let pieceMoved, piecesStaying = state.Pieces |> Set.partition (fun p -> p.Position = move)
    { state with EmptySpace = move
                 Pieces = set [
                    for p in pieceMoved do { p with Position = state.EmptySpace }
                    yield! piecesStaying
                 ] }


let Distance (x1, y1) (x2, y2) = abs (x1 - x2) + abs (y1 - y2)


let DistanceToTarget piece = Distance piece.Position piece.Target


let DistanceToEmpty state =
    state.Pieces
    |> Seq.filter (fun p -> p.Target <> p.Position)
    |> Seq.sumBy (fun p -> p.Position |> Distance state.EmptySpace)
    |> fun d -> (d - 2) * 10 |> max 0


let Score state =
    state.Pieces
    |> Seq.sumBy DistanceToTarget
    |> (*) 100
    |> (+) (DistanceToEmpty state)


let Solve (gameState: GameState, timeout: float) : SolutionType * Position list =

    let start = DateTime.Now

    let rec solve queue seen (bestScore, best) =
        match queue with
        | [] -> Complete, []
        | (gs, moveHistory)::rest ->
            if Set.contains gs seen then
                solve rest seen (bestScore, best)
            else
            let newSeen = Set.add gs seen
            let score = Score gs
            let bestScore, best = if score < bestScore then score, (gs, moveHistory) else bestScore, best
            if (DateTime.Now - start).TotalSeconds > timeout then
                let partialState, partialSolution = best
                Partial partialState, partialSolution |> List.rev
            else
            if IsSolved gs then Complete, moveHistory |> List.rev
            else
                let newMoves =
                    GetValidMoves gs
                    |> Seq.map (fun move -> ApplyMove move gs, move::moveHistory)
                    |> Seq.filter (fun (gs', _) -> Set.contains gs' seen |> not)
                let newQueue = Seq.append newMoves rest |> Seq.sortBy (fst >> Score) |> Seq.toList
                solve newQueue newSeen (bestScore, best)

    let solution = solve [gameState, []] Set.empty (Score gameState, (gameState, []))
    solution |> mapSnd (function
                        | [] -> []
                        | xs -> gameState.EmptySpace::xs)


let SolutionToPaths s = s |> List.map ToCoords |> SegmentPath


let WorkerSolve = WorkerFunc.Create("Solver", "WorkerSolve", Solve)
