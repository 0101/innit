[<AutoOpen>]
module Types

open System


[<Measure>] type Sq
[<Measure>] type Px

type Coords = int<Sq> * int<Sq>
type Path = Coords list

type Axis = X | Y

type PieceType = Regular | Title

type Piece =
    { Content: char
      Color: string
      Type: PieceType
      Left: float<Sq>
      Top: float<Sq>
      TargetPosition: Coords Set }

type Square = Empty | Occupied of Piece

type Animation =
    { Piece: Piece
      Square: Coords
      TargetLeft: float<Sq>
      TargetTop: float<Sq> }

// Solver types /////

type Position = int * int
type Solution = Position list

type GamePiece =
    { Position: Position
      Targets: Position list }

type GameState =
    { GridW: int
      GridH: int
      EmptySpace: Position
      Pieces: GamePiece Set }

type SolutionType = Complete | Partial of GameState

type WireGamePiece =
    { Position: Position
      Targets: Position array }

type WireGameState =
    { WireGridW: int
      WireGridH: int
      WireEmptySpace: Position
      WirePieces: WireGamePiece array }

module Wire =
    open Fable.Core.JsInterop

    let pieceToWire (p: GamePiece) : WireGamePiece =
        { Position = p.Position; Targets = List.toArray p.Targets }

    let wireToPiece (wp: WireGamePiece) : GamePiece =
        { Position = wp.Position; Targets = Array.toList wp.Targets }

    let gameStateToWire (gs: GameState) : WireGameState =
        { WireGridW = gs.GridW; WireGridH = gs.GridH; WireEmptySpace = gs.EmptySpace
          WirePieces = gs.Pieces |> Set.toArray |> Array.map pieceToWire }

    let wireToGameState (ws: WireGameState) : GameState =
        { GridW = ws.WireGridW; GridH = ws.WireGridH; EmptySpace = ws.WireEmptySpace
          Pieces = ws.WirePieces |> Array.map wireToPiece |> Set.ofArray }

    let inline readInt (o: obj) (idx: int) : int = o?(idx)

    let parsePair (o: obj) : int * int = readInt o 0, readInt o 1

    let parsePiece (p: obj) : GamePiece =
        let targets : obj array = p?Targets
        { Position = parsePair p?Position
          Targets = targets |> Array.map parsePair |> Array.toList }

    let parseGameStateFromJs (raw: obj) : GameState =
        let rawPieces : obj array = raw?WirePieces
        { GridW = raw?WireGridW
          GridH = raw?WireGridH
          EmptySpace = parsePair raw?WireEmptySpace
          Pieces = rawPieces |> Array.map parsePiece |> Set.ofArray }

    let parseWorkerResponseFromJs (event: obj) : SolutionType * Solution =
        let data : obj = event?data
        let tag : int = data?tag
        let rawSolution : obj array = data?solution
        let solution : Solution =
            rawSolution |> Array.map parsePair |> Array.toList
        let solutionType =
            match tag with
            | 1 ->
                let ws : obj = data?wireState
                Partial (parseGameStateFromJs ws)
            | _ -> Complete
        solutionType, solution

    let encodeGameStateToJs (gameState: GameState) (timeout: float) : obj =
        (gameStateToWire gameState |> box, timeout)
        |> box

    let encodeResultToJs (solutionType: SolutionType) (solution: Solution) : obj =
        let tag, wireState =
            match solutionType with
            | Complete -> 0, null
            | Partial gs -> 1, gameStateToWire gs |> box
        createObj [
            "tag" ==> tag
            "wireState" ==> wireState
            "solution" ==> (solution |> List.toArray |> Array.map (fun (x, y) -> [| x; y |]))
        ]

/////////////////////

type Msg =
    | CursorMove of int<Px> * int<Px>
    | PageResize of int<Px> * int<Px>
    | StartTimer
    | StartedTimer of float
    | StopTimer of float
    | Tick
    | IdleCheck
    | Idle
    | Solution of SolutionType * Solution
    | Shuffle
    | IntroFinished

type ItemContent =
    | Link of string
    | LinkNew of string
    | Control of Msg

type HiddenItemSpec =
    { Class: string
      Hue: int
      Content: ItemContent }

type HiddenItem =
    { Class: string
      Hue: int
      Content: ItemContent
      Top: float<Sq>
      Left: float<Sq> }

type Phase = Intro1 | Intro2 | RegularOperation

type State =
    { Rng: Random
      ScreenWidth: int<Px>
      ScreenHeight: int<Px>
      EmptySquare: Coords
      Grid: Square [] []
      Title: ((int * int) * char) list
      CurrentAnimations: Animation list
      AnimationQueue: Path list
      AnimationTimer: float option
      Items: HiddenItem list
      LastUpdate: DateTime
      IdleCheckInProgress: bool
      Idle: bool
      Phase: Phase
      SolverTimeout: float }
