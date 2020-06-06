[<AutoOpen>]
module Types

open Fable.React
open System

[<Measure>] type Sq
[<Measure>] type Px

type Coords = int<Sq> * int<Sq>
type Path = Coords list
type Axis = X | Y

type PieceType = Regular | Title
type Piece = {
    Content: char
    Color: string
    Type: PieceType
    Left: float<Sq>
    Top: float<Sq>
    TargetPositions: List<Coords>
}

type Field = Empty | Occupied of Piece

type Animation = {
    Piece: Piece
    Field: Coords
    TargetLeft: float<Sq>
    TargetTop: float<Sq>
}

// Solver types /////

type Position = int * int


type GamePiece = {
    Position: Position
    Targets: Set<Position>
}

type GameState = {
    GridW: int
    GridH: int
    EmptySpace: Position
    Pieces: Set<GamePiece>
}


type SolutionType = Complete | Partial of GameState

//////////////////////


type Msg =
    | CursorMove of int<Px> * int<Px>
    | PageResize of int * int
    | StartTimer
    | StartedTimer of float
    | StopTimer of float
    | Tick
    | IdleCheck
    | Idle
    | Solution of SolutionType * Path list
    | Shuffle


type ItemContent = Link of string | Control of Msg

type HiddenItemSpec = {
    Class: string
    Hue: int
    Content: ItemContent
}

type HiddenItem = {
    Class: string
    Hue: int
    Content: ItemContent
    Top: float<Sq>
    Left: float<Sq>
}

type State = {
    Rng: System.Random
    ScreenWidth: int
    ScreenHeight: int
    EmptyField: int<Sq> * int<Sq>
    Grid: Field[][]
    CurrentAnimations: Animation list
    AnimationQueue: Path list
    AnimationTimer: float option
    Items: HiddenItem list
    LastUpdate: DateTime
    IdleCheckInProgress: bool
    Idle: bool
}


