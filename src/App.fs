module App

open System

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props

open Types
open Mechanics
open Rendering
open Animations


let initialSetup (screenW, screenH) =
    let makeOdd x = if x % 2 = 0 then x + 1 else x
    let gridW = max 5 (makeOdd (screenW / SquareSize + 1))
    let gridH = max 3 (makeOdd (screenH / SquareSize + 1))
    let centerX = gridW / 2
    let centerY = gridH / 2
    let random = Random()

    let makeField x y =
        let t = TitleField random x y
        if   y = centerY && x = centerX - 2 then t [x,y] 'I'
        elif y = centerY && x = centerX - 1 then t [x,y] 'N'
        elif y = centerY && x = centerX     then t [x,y] 'N'
        elif y = centerY && x = centerX + 1 then t [x,y] 'I'
        elif y = centerY && x = centerX + 2 then t [x,y] 'T'
        else RandomField random x y

    {
        Rng = random
        ScreenWidth = screenW
        ScreenHeight = screenH
        EmptyField = fst InitialEmptyField * 1<Sq>, snd InitialEmptyField * 1<Sq>
        Grid = Array.init gridW (fun x ->
               Array.init gridH (fun y ->
                match x, y with
                | z when z = InitialEmptyField -> Empty
                | _ -> Occupied (makeField x y)
        ))
        CurrentAnimations = []
        AnimationQueue = []
        AnimationTimer = None
        Items = RandomItems random (gridW, gridH) [
            { Hue = 000; Class = "mail"; Content = Link ContactEmail }
            { Hue = 024; Class = "sc"; Content = Link ScLink }
            { Hue = 192; Class = "gh"; Content = Link GhLink }
            { Hue = 097; Class = "shuffle"; Content = Control Shuffle }
        ]
        LastUpdate = DateTime.Now
        IdleCheckInProgress = false
        Idle = false
    }, Cmd.none


let init () =
    let screenW = int Browser.Dom.window.innerWidth
    let screenH = int Browser.Dom.window.innerHeight
    initialSetup (screenW, screenH)




let update (msg : Msg) (state : State) =
    match msg with

    | CursorMove (x, y) ->
        let shiftX, shiftY = CenterShift state
        let coords = int (px2grid (x - shiftX)) * 1<Sq>, int (px2grid (y - shiftY)) * 1<Sq>
        let path = GetRandomPath state.Rng state.EmptyField coords
        let segments = SegmentPath path
        { state with AnimationQueue = segments
                     LastUpdate = DateTime.Now
                     Idle = false },
        Cmd.batch [
            match state.AnimationTimer with None -> Cmd.ofMsg StartTimer | _ -> Cmd.none
            if state.IdleCheckInProgress then Cmd.none else Cmd.ofMsg IdleCheck
        ]

    | PageResize (x, y) ->
        { fst (init()) with ScreenWidth = x; ScreenHeight = y },
        match state.AnimationTimer with Some t -> Cmd.ofMsg (StopTimer t) | _ -> Cmd.none

    | Tick ->
        let currentAnimations = state.CurrentAnimations |> List.choose (AdvanceAnimation state)
        let currentAnimations, animationQueue, newEmptyField =
            match currentAnimations, state.AnimationQueue with
            | [], x::rest -> Animate state x, rest, List.last x
            | c, q -> c, q, state.EmptyField
        let state =
          { state with CurrentAnimations = currentAnimations
                       AnimationQueue = animationQueue
                       EmptyField = newEmptyField }
        state,
        match state.CurrentAnimations, state.AnimationTimer with
        | [], Some t -> Cmd.ofMsg (StopTimer t)
        | _::_, None -> Cmd.ofMsg (StartTimer)
        | _          -> Cmd.none

    | StartTimer -> state, Cmd.ofSub (fun dispatch ->
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
        { state with IdleCheckInProgress = false; Idle = true },
        Cmd.OfAsync.result (Solver.SolveState state |> Async.map Solution)

    | Solution (solutionType, solution) ->
        if state.Idle then
            { state with AnimationQueue = state.AnimationQueue @ solution }, Cmd.batch [
                Cmd.ofMsg StartTimer
                match solutionType with
                | Complete ->
                    Browser.Dom.console.info "Received Complete solution"
                    Cmd.none
                | Partial gs ->
                    Browser.Dom.console.info "Received Partial solution"

                    Cmd.OfAsync.result (Solver.Solve gs |> Async.map (mapSnd Solver.SolutionToPaths >> Solution) )
            ]
        else
            state, Cmd.none

    | Shuffle ->
        let grid = state.Grid
        let pieces = GetPieces state.Grid
        let locations = FullyRandomLocations state.Rng (GridWidth grid, GridHeight grid) |> Seq.take pieces.Count
        Seq.zip pieces locations
        |> Seq.iter (fun ((currentLoc, _), newLoc) -> Swap grid currentLoc newLoc)
        state, Cmd.none


let view (state : State) dispatch =
    let move (e: Browser.Types.MouseEvent) = CursorMove (int e.pageX * 1<Px>, int e.pageY * 1<Px>) |> dispatch

    div [] [
        div [] (RenderItems state dispatch)
        div [
                OnMouseMove move
                OnClick move
                Class "Screen"
            ]
            (RenderGrid state)
    ]


let resize _ =
    Cmd.ofSub (fun dispatch ->
        Browser.Dom.window.onresize <- (fun _ ->
            let dims = int Browser.Dom.window.innerWidth, int Browser.Dom.window.innerHeight
            PageResize dims |> dispatch))


// let slowTimer _ =
//     Cmd.ofSub (fun dispatch ->
//         Browser.Dom.window.setInterval((fun _ -> dispatch Tock), 3000) |> ignore)


Program.mkProgram init update view
|> Program.withSubscription resize
//|> Program.withSubscription slowTimer
|> Program.withReactSynchronous "elmish-app"
//|> Program.withConsoleTrace
|> Program.run


