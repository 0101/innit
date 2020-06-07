module App

open System

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props

open Feliz.UseWorker

open Types
open Mechanics
open Rendering
open Animations
open Init
open Update





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


Program.mkProgram init update view
|> Program.withSubscription resize
|> Program.withReactSynchronous "elmish-app"
//|> Program.withConsoleTrace
|> Program.run