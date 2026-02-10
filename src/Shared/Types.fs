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

[<CustomEquality; CustomComparison>]
type GamePiece =
    { Position: Position
      // Array (not list) because F# lists do not survive postMessage structured clone
      Targets: Position array }

    override this.Equals(obj) =
        match obj with
        | :? GamePiece as other ->
            this.Position = other.Position
            && this.Targets.Length = other.Targets.Length
            && Array.forall2 (=) this.Targets other.Targets
        | _ -> false

    override this.GetHashCode() =
        hash (this.Position, this.Targets |> Array.toList)

    interface System.IComparable with
        member this.CompareTo(obj) =
            match obj with
            | :? GamePiece as other ->
                match compare this.Position other.Position with
                | 0 -> compare (Array.toList this.Targets) (Array.toList other.Targets)
                | c -> c
            | _ -> invalidArg "obj" "Cannot compare different types"

type GameState =
    { GridW: int
      GridH: int
      EmptySpace: Position
      Pieces: GamePiece Set }

type SolutionType = Complete | Partial of GameState

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
