module App

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props

open Rendering
open Init
open Update


let view (state : State) dispatch =
    let move (e: Browser.Types.MouseEvent) =
        if state.Phase = RegularOperation then
            CursorMove (int e.pageX * 1<Px>, int e.pageY * 1<Px>) |> dispatch

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
    [ ["resize"], fun dispatch ->
        Browser.Dom.window.onresize <- (fun _ ->
            let dims = int Browser.Dom.window.innerWidth * 1<Px>, int Browser.Dom.window.innerHeight * 1<Px>
            PageResize dims |> dispatch)
        { new System.IDisposable with member _.Dispose() = () } ]


let solverSubscription _ =
    [ ["solver-worker"], fun dispatch ->
        workerOnMessage solverWorker (fun event ->
            let solutionType, solution = parseWorkerResponse event
            Solution (solutionType, solution) |> dispatch)
        { new System.IDisposable with member _.Dispose() = () } ]


open Fable.Core

[<Emit("window.shuffle = $0")>]
let setWindowShuffle (f: unit -> unit) : unit = jsNative

let consoleApi _ =
    [ ["console-api"], fun dispatch ->
        setWindowShuffle (fun () -> dispatch Shuffle)
        { new System.IDisposable with member _.Dispose() = () } ]


let subscriptions model =
    Sub.batch [
        resize model
        solverSubscription model
        consoleApi model
    ]

Program.mkProgram (fun () -> init true) update view
|> Program.withSubscription subscriptions
|> Program.withReactSynchronous "elmish-app"
|> Program.run