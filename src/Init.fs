module Init

open System
open Rendering
open Mechanics
open Elmish


let horizontalTitle = Map [
    (0, -2), 'I'
    (0, -1), 'N'
    (0,  0), 'N'
    (0,  1), 'I'
    (0,  2), 'T'
]

let verticalTitle = Map [
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
    let title = if gridW < 7 && gridH > 5 then verticalTitle else horizontalTitle

    let makePiece x y =
        match title |> Map.tryFind (- (centerY - y), - (centerX - x)) with
        | Some c -> TitlePiece random x y c
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
        CurrentAnimations = []
        AnimationQueue = []
        AnimationTimer = None
        Items = RandomItems random (gridW, gridH) [
            { Hue = 000; Class = "mail"; Content = Link ContactEmail }
            { Hue = 024; Class = "sc"; Content = LinkNew ScLink }
            { Hue = 192; Class = "gh"; Content = LinkNew GhLink }
            // { Hue = 097; Class = "shuffle"; Content = Control Shuffle }
        ]
        LastUpdate = DateTime.Now
        IdleCheckInProgress = false
        Idle = false
        Worker = None
        WorkerTimeout = SolverInitialTimeout
    }


let init () =
    let screenW = int Browser.Dom.window.innerWidth * 1<Px>
    let screenH = int Browser.Dom.window.innerHeight * 1<Px>
    initialSetup (screenW, screenH),
    Cmd.Worker.create Workers.Solver.WorkerSolve SetWorker ChangeWorkerState
