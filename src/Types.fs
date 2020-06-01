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

type HiddenItem = {
    Left: float<Sq>
    Top: float<Sq>
    Hue: int
    Content: ReactElement
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
}

type Msg =
| CursorMove of int<Px> * int<Px>
| PageResize of int * int
| StartedTimer of float
| StopTimer of float
| Tick
| Tock
