module Animations

open Rendering


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
            | Field.Empty -> Browser.Dom.console.warn("Unexpected empty field at", fromX, fromY)
    ]
    let newEmptyX, newEmptyY = List.last path
    state.Grid.[int newEmptyX].[int newEmptyY] <- Field.Empty
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
    if grid2px newPiece.Left = grid2px animation.TargetLeft && grid2px newPiece.Top = grid2px animation.TargetTop
    then None
    else Some { animation with Piece = newPiece }