module App

open System

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props


[<Measure>] type Sq
[<Measure>] type Px


type Coords = int<Sq> * int<Sq>
type Path = Coords list
type Axis = AX | AY


let rec GetShortestPaths ((ax, ay) : Coords) ((bx, by) : Coords) =
    let xDiff = bx - ax
    let yDiff = by - ay
    let xDistance = abs xDiff
    let xDirection = if xDistance <> 0<Sq> then xDiff / xDistance else 0
    let yDistance = abs yDiff
    let yDirection = if yDistance <> 0<Sq> then yDiff / yDistance else 0

    match int xDiff, int yDiff with
    | 0, 0 -> Seq.empty
    | 0, _ -> Seq.singleton [for d in [1..int(yDistance)] -> ( ax, ay + yDirection * d * 1<Sq>) ]
    | _, 0 -> Seq.singleton [for d in [1..int(xDistance)] -> ( ax + xDirection * d * 1<Sq>, ay) ]
    | _ -> seq {
        let moveX = ax + 1<Sq> * xDirection, ay
        for p in GetShortestPaths moveX (bx, by) -> moveX::p

        let moveY = ax, ay + 1<Sq> * yDirection
        for p in GetShortestPaths moveY (bx, by) -> moveY::p
    }


let GetRandomPath random (from : Coords) (to' : Coords) : Path =

    let rec loop (random : Random) path (ax, ay) (bx, by) =

        let moveHorizontally xDiff =
            let xDirection = sign xDiff
            let moveX = ax + 1<Sq> * xDirection, ay
            loop random (moveX::path) moveX (bx, by)

        let moveVertically yDiff =
            let yDirection = sign yDiff
            let moveY = ax, ay + 1<Sq> * yDirection
            loop random (moveY::path) moveY (bx, by)

        match bx - ax, by - ay with
        | 0<Sq>, 0<Sq> -> path
        | 0<Sq>, yDiff -> moveVertically yDiff
        | xDiff, 0<Sq> -> moveHorizontally xDiff
        | xDiff, yDiff -> if random.Next(2) = 1 then moveVertically yDiff else moveHorizontally xDiff

    loop random [to'] to' from


let axis (ax, ay) (bx, by) =
    if   ax = bx then AX
    elif ay = by then AY
    else failwith "Non continuous path"


let SegmentPath (path: Path) : Path list =
    let rec loop result currentChunk restOfPath =
        match currentChunk, restOfPath with
        | [],        [] -> result
        | chunk,     [] -> (List.rev chunk)::result
        | [],        x1::rest -> loop result [x1] rest
        | [x1],      x2::rest -> loop result [x2;x1] rest
        | x2::x1::_, x3::rest ->
            if axis x1 x2 = axis x2 x3
            then loop result (x3::currentChunk) rest
            else loop ((List.rev currentChunk)::result) [x2] restOfPath
    loop [] [] path |> List.rev


let SquareSize = 100;
let AnimationStep = 0.34<Sq>;

let PxPerSq = (float SquareSize) * 1.0<Px/Sq>

let grid2px (grid: float<Sq>): int<Px> =
    let floatPx = grid * ((float SquareSize) / 1.0<Sq>)
    //int floatPx * 1<Px>
    floatPx |> round |> int |> ((*) 1<Px>)
    // let floatPx' = floatPx / 1.0<Px>
    //(int floatPx') * 1<Px>


let px2grid (px: int<Px>) : float<Sq> =
    let floatPx = (float px) * 1.0<Px>
    floatPx / PxPerSq

type PieceType = Regular | Title
type Piece = {
    Content: char
    Color: string
    Type: PieceType
    Left: float<Sq>
    Top: float<Sq>
} with
    member this.LeftPx = grid2px this.Left
    member this.TopPx = grid2px this.Top

type Field = Empty | Occupied of Piece

type Animation = {
    Piece: Piece
    Field: Coords
    TargetLeft: float<Sq>
    TargetTop: float<Sq>
} with
    member this.TargetLeftPx = grid2px this.TargetLeft
    member this.TargetTopPx = grid2px this.TargetTop

type State = {
    Rng: Random
    ScreenWidth: int
    ScreenHeight: int
    EmptyField: int<Sq> * int<Sq>
    Grid: Field[][]
    CurrentAnimations: Animation list
    AnimationQueue: Path list
    Timer: float option
} with
    member this.Center = this.ScreenWidth / 2 * 1<Px>, this.ScreenHeight / 2 * 1<Px>
    member this.GridWidth  = this.Grid.Length * 1<Sq>
    member this.GridHeight = this.Grid.[0].Length * 1<Sq>
    member this.CenterShift =
        let screenCx, screenCy = this.Center
        let gridCx, gridCy = grid2px ((float(this.GridWidth / 2) + 0.5) * 1.0<Sq>), grid2px ((float (this.GridHeight / 2) + 0.5) * 1.0<Sq>)
        screenCx - gridCx, screenCy - gridCy

type Msg =
| CursorMove of int<Px> * int<Px>
| PageResize of int * int
| Tick
| StartedTimer of float
| StopTimer of float


let Animate (state : State) (path : Path) : Animation list =
    let animations = [
        for (toX, toY), (fromX, fromY) in path |> List.pairwise do
            match state.Grid.[int fromX].[int fromY] with
            | Occupied piece ->
                state.Grid.[int toX].[int toY] <- Occupied piece
                yield { Piece = piece
                        Field = toX, toY
                        TargetLeft = float toX * 1.0<Sq>
                        TargetTop = float toY * 1.0<Sq> }
            | Empty -> Browser.Dom.console.warn("Unexpected empty field at", fromX, fromY)
    ]
    let newEmptyX, newEmptyY = List.last path
    state.Grid.[int newEmptyX].[int newEmptyY] <- Empty
    animations


let fRound (x: float<Sq>) = (round (10.0 * float x) / 10.0) * 1.0<Sq>

let AdvanceAnimation (state : State) (animation : Animation) : Animation option =
    let xDiff = animation.TargetLeft - animation.Piece.Left
    let yDiff = animation.TargetTop - animation.Piece.Top
    let xDirection = sign xDiff |> float
    let yDirection = sign yDiff |> float
    let newLeft = fRound (animation.Piece.Left + xDirection * (min AnimationStep (abs xDiff)))
    let newTop = fRound (animation.Piece.Top + yDirection * (min AnimationStep (abs yDiff)))
    let x, y = animation.Field
    let newPiece = { animation.Piece with Left = newLeft; Top = newTop }
    state.Grid.[int x].[int y] <- Occupied newPiece
    if newPiece.LeftPx = animation.TargetLeftPx && newPiece.TopPx = animation.TargetTopPx
    then None
    else Some { animation with Piece = newPiece }


let BasicChars =
    [0..127]
    |> Seq.map (fun i -> (char) i)
    |> Seq.filter (Char.IsControl >> not)
    |> Seq.toArray

let AllChars =
    [0..56000]
    |> Seq.map (fun i -> (char) i)
    |> Seq.filter (Char.IsControl >> not)
    |> Seq.toArray


let init() =
    let makeOdd x = if x % 2 = 0 then x + 1 else x
    let screenW = int Browser.Dom.window.innerWidth
    let screenH = int Browser.Dom.window.innerHeight
    let gridW = max 5 (makeOdd (screenW / SquareSize + 1))
    let gridH = max 3 (makeOdd (screenH / SquareSize + 1))
    let centerX = gridW / 2
    let centerY = gridH / 2


    let random = Random()
    let randomField x y = {
        Content = if random.NextDouble() > 0.2
                  then BasicChars.[random.Next(BasicChars.Length)]
                  else AllChars.[random.Next(AllChars.Length)]
        Left = float x * 1.0<Sq>
        Top  = float y * 1.0<Sq>
        Color = sprintf "hsl(0, 0%%, %f%%)" (random.NextDouble() * 4.0 + 2.0)
        Type = Regular
    }
    let titleField x y char = { randomField x y with Content = char; Type = Title }
    let makeField x y =
        if   y = centerY && x = centerX - 2 then titleField x y 'I'
        elif y = centerY && x = centerX - 1 then titleField x y 'N'
        elif y = centerY && x = centerX     then titleField x y 'N'
        elif y = centerY && x = centerX + 1 then titleField x y 'I'
        elif y = centerY && x = centerX + 2 then titleField x y 'T'
        else randomField x y

    let emptyField = 3, 2
    {
        Rng = random
        ScreenWidth = screenW
        ScreenHeight = screenH
        EmptyField = fst emptyField * 1<Sq>, snd emptyField * 1<Sq>
        Grid = Array.init gridW (fun x ->
               Array.init gridH (fun y ->
                match x, y with
                | z when z = emptyField -> Empty
                | _ -> Occupied (makeField x y)
        ))
        CurrentAnimations = []
        AnimationQueue = []
        Timer = None
    }, Cmd.none

// UPDATE

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
        let shiftX, shiftY = state.CenterShift
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

    | StartedTimer t ->
        { state with Timer = Some t }, Cmd.none

    | StopTimer t ->
        Browser.Dom.window.clearInterval t
        { state with Timer = None }, cmd


let RenderGrid (state : State) =
    let shiftX, shiftY = state.CenterShift
    state.Grid |> Array.collect (Array.map (function
        | Empty -> div [ Class "empty" ] []
        | Occupied piece ->
            div [ Class ("piece" + match piece.Type with Title -> " title" | _ -> "")
                  Style [
                    Top (grid2px piece.Top + shiftY)
                    Left (grid2px piece.Left + shiftX)
                    Width SquareSize
                    Height SquareSize
                    BackgroundColor piece.Color
                  ]
                ]
                [ div [ Class "inner" ]
                      [ p [ Class "symbol" ] [ str (string piece.Content) ]
                        //p [ Class "info" ] [ str (string (int piece.Content))]
                      ] ]
        ))
        |> Array.toList


let view (state : State) dispatch =
  div [ OnMouseMove (fun x -> CursorMove (int x.pageX * 1<Px>, int x.pageY * 1<Px>) |> dispatch )
        Class "Screen"
      ]
      (RenderGrid state)


let resize initial =
    Cmd.ofSub (fun dispatch ->
        Browser.Dom.window.onresize <- (fun _ ->
            PageResize (int Browser.Dom.window.innerWidth, int Browser.Dom.window.innerHeight) |> dispatch))


Program.mkProgram init update view
|> Program.withSubscription resize
|> Program.withReactSynchronous "elmish-app"
// |> Program.withConsoleTrace
|> Program.run