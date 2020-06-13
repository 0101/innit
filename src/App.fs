module App

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props

open Rendering
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
            let dims = int Browser.Dom.window.innerWidth * 1<Px>, int Browser.Dom.window.innerHeight * 1<Px>
            PageResize dims |> dispatch))


Program.mkProgram init update view
|> Program.withSubscription resize
|> Program.withReactSynchronous "elmish-app"
|> Program.run