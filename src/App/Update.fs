module Update

open Elmish
open Fable.Core

open Rendering
open Mechanics
open System
open Animations
open Init


[<Emit("""new Worker(new URL("../../worker-out/Worker.js", import.meta.url), { type: "module" })""")>]
let createSolverWorker () : obj = jsNative

[<Emit("$0.postMessage($1)")>]
let workerPostMessage (worker: obj) (data: obj) : unit = jsNative

[<Emit("$0.onmessage = $1")>]
let workerOnMessage (worker: obj) (handler: obj -> unit) : unit = jsNative

open Fable.Core.JsInterop

let solverWorker = createSolverWorker ()

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

let postSolverRequest gameState timeout =
    workerPostMessage solverWorker [| gameStateToJs gameState; box timeout |]

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

let parseWorkerResponse (event: obj) : SolutionType * Solution =
    let data : obj = event?data
    let tag : int = data?tag
    let rawSolution : obj array = data?solution
    let solution : Solution =
        rawSolution |> Array.map parsePair |> Array.toList
    let solutionType =
        match tag with
        | 1 -> Partial (parseGameStateFromJs data?state)
        | _ -> Complete
    solutionType, solution

let delayThenReturn msg (ms: int) =
    async {
        do! Async.Sleep ms
        return msg
    }


let update (msg : Msg) (state : State) =
    match msg with

    | CursorMove (x, y) ->
        let shiftX, shiftY = CenterShift state
        let coords = (int (px2grid (x - shiftX)), int (px2grid (y - shiftY))) |> ToCoords
        let path = GetRandomPath state.Rng state.EmptySquare coords
        let segments = SegmentPath path
        { state with AnimationQueue = segments
                     LastUpdate = DateTime.Now
                     Idle = false },
        Cmd.batch [
            Cmd.ofMsg StartTimer
            if state.IdleCheckInProgress then Cmd.none else Cmd.ofMsg IdleCheck
        ]

    | PageResize (x, y) ->
        { fst (init false) with ScreenWidth = x; ScreenHeight = y; SolverTimeout = state.SolverTimeout },
        match state.AnimationTimer with Some t -> Cmd.ofMsg (StopTimer t) | _ -> Cmd.none

    | StartTimer ->
        state,
        match state.AnimationTimer with
        | Some t -> Cmd.none
        | _ -> Cmd.ofEffect (fun dispatch ->
                let tick _ = dispatch Tick
                let interval = 1000 / TargetFPS
                let timer = Browser.Dom.window.setInterval(tick, interval)
                dispatch (StartedTimer timer))

    | StartedTimer t -> { state with AnimationTimer = Some t }, Cmd.none

    | StopTimer t ->
        Browser.Dom.window.clearInterval t

        match state.Phase with
        | Intro1 ->
            { state with AnimationTimer = None; Phase = Intro2 },
            Cmd.ofEffect (fun dispatch ->
                Browser.Dom.window.setTimeout((fun _ -> dispatch IntroFinished), 1000) |> ignore)
        | _ ->
            { state with AnimationTimer = None }, Cmd.none

    | IntroFinished -> { state with Phase = RegularOperation }, Cmd.none

    | Tick ->
        let currentAnimations = state.CurrentAnimations |> List.choose (AdvanceAnimation state)
        let currentAnimations, animationQueue, newEmptySquare, cmd =
            match currentAnimations, state.AnimationQueue with
            | [], move::rest when move |> IsValidMove state ->
                Animate state move, rest, List.last move, Cmd.none
            | [], _::_ ->
                Console.warn ("Received invalid animation, discarding")
                [], [], state.EmptySquare, Cmd.ofMsg IdleCheck
            | c, q -> c, q, state.EmptySquare, Cmd.none
        let state =
          { state with CurrentAnimations = currentAnimations
                       AnimationQueue = animationQueue
                       EmptySquare = newEmptySquare }
        state,
        Cmd.batch [
            match state.CurrentAnimations, state.AnimationTimer with
            | [], Some t -> Cmd.ofMsg (StopTimer t)
            | _::_, None -> Cmd.ofMsg (StartTimer)
            | _          -> Cmd.none
            cmd
        ]

    | IdleCheck ->
        { state with IdleCheckInProgress = true },
        let secondsSinceUpdate = (DateTime.Now - state.LastUpdate).TotalSeconds
        if secondsSinceUpdate >= IdleSeconds
        then Cmd.ofMsg Idle
        else Cmd.OfAsync.perform (delayThenReturn IdleCheck) (int (1000.0 * (IdleSeconds - secondsSinceUpdate))) id

    | Idle ->
        Console.info "* IDLE"
        { state with IdleCheckInProgress = false
                     Idle = true
                     SolverTimeout = SolverInitialTimeout
                     AnimationQueue = [] },
        let gameState = Solver.CreateGameState state
        if Solver.IsSolved gameState
        then Cmd.none
        else
            Console.info "Executing solver"
            Cmd.ofEffect (fun _ -> postSolverRequest gameState SolverInitialTimeout)

    | Solution (solutionType, solution) ->
        if not state.Idle then state, Cmd.none
        else
        let solverTimeout =
            match solutionType, solution with
            | Partial _, [] ->
                Console.info("Increasing solver timeout to", state.SolverTimeout + SolverTimeoutStep)
                state.SolverTimeout + SolverTimeoutStep
            | _ -> state.SolverTimeout

        { state with AnimationQueue = state.AnimationQueue @ Solver.SolutionToPaths solution
                     SolverTimeout = solverTimeout }, Cmd.batch [
            Cmd.ofMsg StartTimer
            match solutionType with
            | Complete ->
                Console.info "Received Complete solution"
                Cmd.none
            | Partial partiallySolvedState ->
                Console.info "Received Partial solution"
                if solution = [] && solverTimeout > SolverMaxTimeout
                then
                    Console.warn "Failed to solve, giving up"
                    Cmd.none
                else
                    Console.info "Executing solver on partially solved state"
                    Cmd.ofEffect (fun _ -> postSolverRequest partiallySolvedState solverTimeout)
         ]

    | Shuffle ->
        let grid = state.Grid
        let pieces = GetPieces state.Grid
        let locations = FullyRandomLocations state.Rng (GridWidth grid, GridHeight grid) |> Seq.take pieces.Count
        Seq.zip pieces locations
        |> Seq.iter (fun ((currentLoc, _), newLoc) -> Swap grid currentLoc newLoc)
        { state with Idle = false
                     AnimationQueue = []
                     LastUpdate = DateTime.Now },
        Cmd.batch [
            if state.IdleCheckInProgress then Cmd.none else Cmd.ofMsg IdleCheck
        ]
