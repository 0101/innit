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
            000, a [ Href ContactEmail; Class "mail" ] [ ]
            024, a [ Href ScLink; Target "_blank"; Class "sc" ] [ ]
            192, a [ Href GhLink; Target "_blank"; Class "gh" ] [ ]
        ]
        LastUpdate = DateTime.Now
    }, Cmd.none


let init () =
    let screenW = int Browser.Dom.window.innerWidth
    let screenH = int Browser.Dom.window.innerHeight
    initialSetup (screenW, screenH)


let update (msg : Msg) (state : State) =

    let state = if msg = Tock then state else { state with LastUpdate = DateTime.Now }

    let state =
        match msg with

        | CursorMove (x, y) ->
            let shiftX, shiftY = CenterShift state
            let coords = int (px2grid (x - shiftX)) * 1<Sq>, int (px2grid (y - shiftY)) * 1<Sq>
            let path = GetRandomPath state.Rng state.EmptyField coords
            let segments = SegmentPath path
            { state with AnimationQueue = segments }

        | PageResize (x, y) -> { fst (init()) with ScreenWidth = x; ScreenHeight = y }

        | Tick ->
            let currentAnimations = state.CurrentAnimations |> List.choose (AdvanceAnimation state)
            let currentAnimations, animationQueue, newEmptyField =
                match currentAnimations, state.AnimationQueue with
                | [], x::rest -> Animate state x, rest, List.last x
                | c, q -> c, q, state.EmptyField
            { state with CurrentAnimations = currentAnimations
                         AnimationQueue = animationQueue
                         EmptyField = newEmptyField }

        | StartedTimer t -> { state with AnimationTimer = Some t }

        | StopTimer t ->
            Browser.Dom.window.clearInterval t
            { state with AnimationTimer = None }

        | Tock ->
            if (DateTime.Now - state.LastUpdate).TotalSeconds > IdleSeconds then
                Browser.Dom.console.info(sprintf "Solution: %A" (Solver.SolveState state))
                { state with AnimationQueue = Solver.SolveState state }
            else state



    let cmd =
        match state.AnimationTimer, state.CurrentAnimations, state.AnimationQueue with
        | None, [], [] -> Cmd.none
        | None, _, _  -> Cmd.ofSub (fun dispatch ->
            let timer = Browser.Dom.window.setInterval((fun _ -> dispatch Tick), 1000 / 40)
            dispatch (StartedTimer timer))
        | Some t, [], [] -> Cmd.ofMsg (StopTimer t)
        | _ -> Cmd.none

    state, cmd


let view (state : State) dispatch =
    let move (e: Browser.Types.MouseEvent) = CursorMove (int e.pageX * 1<Px>, int e.pageY * 1<Px>) |> dispatch

    div [] [
        div [] (RenderItems state)
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


let slowTimer _ =
    Cmd.ofSub (fun dispatch ->
        Browser.Dom.window.setInterval((fun _ -> dispatch Tock), 3000) |> ignore)


Program.mkProgram init update view
|> Program.withSubscription resize
|> Program.withSubscription slowTimer
|> Program.withReactSynchronous "elmish-app"
//|> Program.withConsoleTrace
|> Program.run