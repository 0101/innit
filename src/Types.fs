[<AutoOpen>]
module Types

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
}

type Field = Empty | Occupied of Piece

type Animation = {
    Piece: Piece
    Field: Coords
    TargetLeft: float<Sq>
    TargetTop: float<Sq>
}

type State = {
    Rng: System.Random
    ScreenWidth: int
    ScreenHeight: int
    EmptyField: int<Sq> * int<Sq>
    Grid: Field[][]
    CurrentAnimations: Animation list
    AnimationQueue: Path list
    Timer: float option
}

type Msg =
| CursorMove of int<Px> * int<Px>
| PageResize of int * int
| StartedTimer of float
| StopTimer of float
| Tick
