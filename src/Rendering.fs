module Rendering

open Fable.React
open Fable.React.Props


let grid2px (grid: float<Sq>): int<Px> =
    let floatPx = grid * ((float SquareSize) / 1.0<Sq>)
    floatPx |> round |> int |> ((*) 1<Px>)


let px2grid (px: int<Px>) : float<Sq> =
    let floatPx = (float px) * 1.0<Px>
    floatPx / PxPerSq


let CenterShift state =
    let center = state.ScreenWidth / 2 * 1<Px>, state.ScreenHeight / 2 * 1<Px>
    let gridWidth = state.Grid.Length * 1<Sq>
    let gridHeight = state.Grid.[0].Length * 1<Sq>
    let screenCx, screenCy = center
    let gridCx, gridCy = grid2px ((float(gridWidth / 2) + 0.5) * 1.0<Sq>), grid2px ((float (gridHeight / 2) + 0.5) * 1.0<Sq>)
    screenCx - gridCx, screenCy - gridCy


let RandomField (random : System.Random) x y = {
    Content = if random.NextDouble() < BasicCharProbability
              then BasicChars.[random.Next(BasicChars.Length)]
              else AllChars.[random.Next(AllChars.Length)]
    Left = float x * 1.0<Sq>
    Top  = float y * 1.0<Sq>
    Color = PieceColor random
    Type = Regular
}

let TitleField (r: System.Random) x y char =
  { RandomField r x y with Content = char; Type = PieceType.Title }


let RenderGrid (state : State) =
    let shiftX, shiftY = CenterShift state
    state.Grid |> Seq.collect (Seq.map (function
        | Field.Empty -> div [ Class "empty" ] []
        | Occupied piece ->
            div [ Class ("piece" + match piece.Type with PieceType.Title -> " title" | _ -> "")
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
        |> Seq.toList