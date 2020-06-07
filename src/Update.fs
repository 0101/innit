module Update

open Feliz.UseWorker
open Elmish

open Rendering
open Mechanics
open System
open Animations
open Init


let update (msg : Msg) (state : State) =
    match msg with

    | SetWorker w -> { state with Worker = Some w }, Cmd.none
    | ChangeWorkerState workerState ->
        Console.info("Worker status:", workerState)
        state,
        match workerState with
        | WorkerStatus.TimeoutExpired
        | WorkerStatus.Killed ->
            Console.info "Restarting worker"
            Cmd.batch [
                Cmd.Worker.restart state.Worker
                Cmd.ofMsg IdleCheck ]
        | _ -> Cmd.none

    | CursorMove (x, y) ->
        let shiftX, shiftY = CenterShift state
        let coords = ToCoords (int (px2grid (x - shiftX)), int (px2grid (y - shiftY)))
        let path = GetRandomPath state.Rng state.EmptyField coords
        let segments = SegmentPath path
        { state with AnimationQueue = segments
                     LastUpdate = DateTime.Now
                     Idle = false },
        Cmd.batch [
            Cmd.ofMsg StartTimer
            if state.IdleCheckInProgress then Cmd.none else Cmd.ofMsg IdleCheck
        ]

    | PageResize (x, y) ->
        { fst (init()) with ScreenWidth = x; ScreenHeight = y; Worker = state.Worker },
        match state.AnimationTimer with Some t -> Cmd.ofMsg (StopTimer t) | _ -> Cmd.none

    | Tick ->
        let currentAnimations = state.CurrentAnimations |> List.choose (AdvanceAnimation state)
        let currentAnimations, animationQueue, newEmptyField, cmd =
            match currentAnimations, state.AnimationQueue with
            | [], x::rest ->
                if x |> IsValidMove state then
                    Animate state x, rest, List.last x, Cmd.none
                else
                    Console.warn ("Received invalid animation, discarding")
                    [], [], state.EmptyField, Cmd.ofMsg IdleCheck
            | c, q -> c, q, state.EmptyField, Cmd.none
        let state =
          { state with CurrentAnimations = currentAnimations
                       AnimationQueue = animationQueue
                       EmptyField = newEmptyField }
        state,
        Cmd.batch [
            match state.CurrentAnimations, state.AnimationTimer with
            | [], Some t -> Cmd.ofMsg (StopTimer t)
            | _::_, None -> Cmd.ofMsg (StartTimer)
            | _          -> Cmd.none
            cmd
        ]

    | StartTimer ->
        state, match state.AnimationTimer with
               | Some t -> Cmd.none
               | _ ->
                    Cmd.ofSub (fun dispatch ->
                        let timer = Browser.Dom.window.setInterval((fun _ -> dispatch Tick), 1000 / 40)
                        dispatch (StartedTimer timer))

    | StartedTimer t -> { state with AnimationTimer = Some t }, Cmd.none

    | StopTimer t ->
        Browser.Dom.window.clearInterval t
        { state with AnimationTimer = None }, Cmd.none

    | IdleCheck ->
        { state with IdleCheckInProgress = true },
        let secondsSinceUpdate = (DateTime.Now - state.LastUpdate).TotalSeconds
        if secondsSinceUpdate >= IdleSeconds
        then Cmd.ofMsg Idle
        else Cmd.OfAsync.result (async {
            do! Async.Sleep (int (1000.0 * (IdleSeconds - secondsSinceUpdate)))
            return IdleCheck
        })

    | Idle ->
        Console.info "* IDLE"
        { state with IdleCheckInProgress = false
                     Idle = true
                     WorkerTimeout = SolverInitialTimeout
                     AnimationQueue = [] },
        let gameState = Workers.Solver.CreateGameState state
        if Workers.Solver.IsSolved gameState
        then Cmd.none
        else
            Console.info "Executing worker"
            Cmd.Worker.exec state.Worker (gameState, SolverInitialTimeout) Solution

    | Solution (solutionType, solution) ->
        if not state.Idle then state, Cmd.none
        else
        let solverTimeout =
            match solutionType, solution with
            | Partial _, [] ->
                Console.info("Increasing solver timeout to", state.WorkerTimeout + SolverTimeoutStep)
                state.WorkerTimeout + SolverTimeoutStep
            | _ -> state.WorkerTimeout

        { state with AnimationQueue = state.AnimationQueue @ Workers.Solver.SolutionToPaths solution
                     WorkerTimeout = solverTimeout }, Cmd.batch [
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
                    Console.info "Executing worker on partially solved state"
                    Cmd.Worker.exec state.Worker (partiallySolvedState, solverTimeout) Solution
         ]

    | Shuffle ->
        let grid = state.Grid
        let pieces = GetPieces state.Grid
        let locations = FullyRandomLocations state.Rng (GridWidth grid, GridHeight grid) |> Seq.take pieces.Count
        Seq.zip pieces locations
        |> Seq.iter (fun ((currentLoc, _), newLoc) -> Swap grid currentLoc newLoc)
        state, Cmd.none