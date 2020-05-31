module Solver


open Mechanics


type Position = int * int

type GamePiece = {
    Position: Position
    Targets: Set<Position>
}

type GameState = {
    GridW: int
    GridH: int
    EmptySpace: Position
    Pieces: GamePiece list
}


let CreateGameState (state : State) : GameState =
    let ex, ey = state.EmptyField
    {
        GridW = state.Grid.Length
        GridH = state.Grid.[0].Length
        EmptySpace = int ex, int ey
        Pieces = seq {
            for x, row in state.Grid |> Array.mapi (fun x r -> x, r) do
                for y, field in row |> Array.mapi (fun y f -> y, f) do
                    match field with
                    | Occupied piece when piece.Type = Title ->
                        let tx, ty = piece.StartingPosition
                        yield {
                            Position = x, y
                            Targets = set [ int tx, int ty ] // TODO Multiple targets
                        }
                    | _ -> () } |> Seq.toList
    }


let IsSolved state = state.Pieces |> Seq.forall (fun p -> p.Targets |> Set.contains p.Position)


let GetValidMoves state = seq {
    let ex, ey = state.EmptySpace
    if ex > 0 then yield ex - 1, ey
    if ey > 0 then yield ex, ey - 1
    if ex < state.GridW - 1 then yield ex + 1, ey
    if ey < state.GridH - 1 then yield ex, ey + 1
}


let ApplyMove state move =
    let pieceMoved, piecesStaying = state.Pieces |> List.partition (fun p -> p.Position = move)
    { state with EmptySpace = move
                 Pieces = [
                    for p in pieceMoved do { p with Position = state.EmptySpace }
                    yield! piecesStaying
                 ] }


let Distance (x1, y1) (x2, y2) = abs (x1 - x2) + abs (y1 - y2)


let DistanceToTarget piece =
    piece.Targets |> Set.map (Distance piece.Position) |> Set.minElement


let DistanceToEmpty state =
    state.Pieces
    |> List.sumBy (fun p -> p.Position |> Distance state.EmptySpace)


let Score state =
    state.Pieces
    |> List.sumBy DistanceToTarget
    |> (*) 1000
    |> (+) (DistanceToEmpty state)


let Solve (gameState : GameState) : Position list =

    let rec solve queue seen =
        match queue with
        | [] -> []
        | (gs, moveHistory)::rest ->
            let newSeen = Set.add gs seen
            if IsSolved gs then moveHistory |> List.rev
            else
                let newMoves =
                    GetValidMoves gs
                    |> Seq.map (fun move -> ApplyMove gs move, move::moveHistory )
                    |> Seq.filter (fun (gs, _) -> Set.contains gs seen |> not)
                let newQueue = Seq.append newMoves rest |> Seq.sortBy (fst >> Score) |> Seq.toList
                solve newQueue newSeen

    solve [gameState, []] Set.empty
    |> function
    | [] -> []
    | xs -> gameState.EmptySpace::xs



let SolveState (state : State) : Path list =
    state
    |> CreateGameState
    |> Solve
    |> List.map (fun (x, y) -> x * 1<Sq>, y * 1<Sq>)
    |> SegmentPath