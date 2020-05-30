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


let init () =
    let makeOdd x = if x % 2 = 0 then x + 1 else x
    let screenW = int Browser.Dom.window.innerWidth
    let screenH = int Browser.Dom.window.innerHeight
    let gridW = max 5 (makeOdd (screenW / SquareSize + 1))
    let gridH = max 3 (makeOdd (screenH / SquareSize + 1))
    let centerX = gridW / 2
    let centerY = gridH / 2
    let random = Random()

    let makeField x y =
        let t = TitleField random x y
        if   y = centerY && x = centerX - 2 then t 'I'
        elif y = centerY && x = centerX - 1 then t 'N'
        elif y = centerY && x = centerX     then t 'N'
        elif y = centerY && x = centerX + 1 then t 'I'
        elif y = centerY && x = centerX + 2 then t 'T'
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
        Timer = None
    }, Cmd.none


let update (msg : Msg) (state : State) =

    let cmd =
        match state.Timer, state.CurrentAnimations, state.AnimationQueue with
        | None, [], [] -> Cmd.none
        | None, _, _  -> Cmd.ofSub (fun dispatch ->
            let timer = Browser.Dom.window.setInterval((fun _ -> dispatch Tick), 1000 / 40)
            dispatch (StartedTimer timer))
        | Some t, [], [] -> Cmd.ofMsg (StopTimer t)
        | _ -> Cmd.none

    match msg with

    | CursorMove (x, y) ->
        let shiftX, shiftY = CenterShift state
        let coords = int (px2grid (x - shiftX)) * 1<Sq>, int (px2grid (y - shiftY)) * 1<Sq>
        if coords <> state.EmptyField then
            let path = GetRandomPath state.Rng state.EmptyField coords
            let segments = SegmentPath path
            { state with AnimationQueue = segments }, cmd
        else state, cmd

    | PageResize (x, y) -> { fst (init()) with ScreenWidth = x; ScreenHeight = y }, cmd

    | Tick ->
        let currentAnimations = state.CurrentAnimations |> List.choose (AdvanceAnimation state)
        let currentAnimations, animationQueue, newEmptyField =
            match currentAnimations, state.AnimationQueue with
            | [], x::rest -> Animate state x, rest, List.last x
            | c, q -> c, q, state.EmptyField
        { state with CurrentAnimations = currentAnimations
                     AnimationQueue = animationQueue
                     EmptyField = newEmptyField }, cmd

    | StartedTimer t -> { state with Timer = Some t }, Cmd.none

    | StopTimer t ->
        Browser.Dom.window.clearInterval t
        { state with Timer = None }, cmd


let view (state : State) dispatch =
    div [ OnMouseMove (fun x -> CursorMove (int x.pageX * 1<Px>, int x.pageY * 1<Px>) |> dispatch )
          Class "Screen" ]
        (RenderGrid state)


let resize _ =
    Cmd.ofSub (fun dispatch ->
        Browser.Dom.window.onresize <- (fun _ ->
            let dims = int Browser.Dom.window.innerWidth, int Browser.Dom.window.innerHeight
            PageResize dims |> dispatch))


Program.mkProgram init update view
|> Program.withSubscription resize
|> Program.withReactSynchronous "elmish-app"
// |> Program.withConsoleTrace
|> Program.run