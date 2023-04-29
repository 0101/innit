[<AutoOpen>]
module Types

open Feliz.UseWorker
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
      Targets: Position array }

type GameState =
    { GridW: int
      GridH: int
      EmptySpace: Position
      Pieces: GamePiece Set }

type SolutionType = Complete | Partial of GameState

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
    | SetWorker of Worker<GameState * float, SolutionType * Solution>
    | ChangeWorkerState of WorkerStatus

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
      Worker: Worker<GameState * float, SolutionType * Solution> option
      WorkerTimeout: float }
    interface System.IDisposable with
        member this.Dispose() = this.Worker |> Option.iter (fun w -> w.Dispose())
