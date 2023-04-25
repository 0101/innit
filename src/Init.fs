module Init

open System
open Rendering
open Mechanics
open Elmish


let horizontalTitle = [
    (0, -2), 'I'
    (0, -1), 'N'
    (0,  0), 'N'
    (0,  1), 'I'
    (0,  2), 'T'
]

let verticalTitle = [
    (-2, 0), 'I'
    (-1, 0), 'N'
    ( 0, 0), 'N'
    ( 1, 0), 'I'
    ( 2, 0), 'T'
]


let initialSetup (screenW, screenH) =
    let makeOdd x = if x % 2 = 0 then x + 1 else x
    let gridW = max 5 (makeOdd (screenW / SquareSize + 1))
    let gridH = max 5 (makeOdd (screenH / SquareSize + 1))
    let centerX = gridW / 2
    let centerY = gridH / 2
    let random = Random()
    let titleSpec = if gridW < 7 && gridH > 5 then verticalTitle else horizontalTitle
    let titleAbs = [ for ((y, x), c) in titleSpec -> ((centerX + x), (centerY + y)), c ]
    let title = Map titleAbs
    let titleTargets = titleAbs |> List.groupBy snd |> List.map (mapSnd (List.map fst)) |> Map

    let makePiece x y =
        match title |> Map.tryFind (x, y) with
        | Some c -> TitlePiece random x y c (titleTargets.[c])
        | None -> RandomPiece random x y

    {
        Rng = random
        ScreenWidth = screenW
        ScreenHeight = screenH
        EmptySquare = InitialEmptySquare |> ToCoords
        Grid = Array.init gridW (fun x ->
               Array.init gridH (fun y ->
                match x, y with
                | z when z = InitialEmptySquare -> Empty
                | _ -> Occupied (makePiece x y)))
        Title = title |> Map.toSeq |> Seq.map fst |> Seq.toList
        CurrentAnimations = []
        AnimationQueue = []
        AnimationTimer = None
        Items = RandomItems random (gridW, gridH) [
            { Hue = 000; Class = "mail"; Content = Link ContactEmail }
            { Hue = 024; Class = "sc"; Content = LinkNew ScLink }
            { Hue = 192; Class = "gh"; Content = LinkNew GhLink }
            { Hue = 097; Class = "shuffle"; Content = Control Shuffle }
        ]
        LastUpdate = DateTime.Now
        IdleCheckInProgress = false
        Idle = false
        Worker = None
        WorkerTimeout = SolverInitialTimeout
    }

let initialRandomization (state: State) =
    state.Title
    |> Seq.zip (state.Title |> Seq.map FullSurroundings |> Seq.map (fun x -> x |> Seq.sortBy (fun _ -> state.Rng.Next()) |> Seq.head ))
    |> Seq.sortBy (fun _ -> state.Rng.Next())
    |> Seq.take 3
    |> Seq.iter (fun (x, y) -> Swap state.Grid x y)
    state

let init () =
    let screenW = int Browser.Dom.window.innerWidth * 1<Px>
    let screenH = int Browser.Dom.window.innerHeight * 1<Px>
    initialSetup (screenW, screenH) |> initialRandomization,
    Cmd.batch [
        Cmd.Worker.create Workers.Solver.WorkerSolve SetWorker ChangeWorkerState
        Cmd.ofMsg Idle
    ]
