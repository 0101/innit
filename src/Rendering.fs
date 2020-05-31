module Rendering

open System
open Fable.React
open Fable.React.Props


open Mechanics


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
    StartingPosition = x * 1<Sq>, y * 1<Sq>
}

let TitleField (r: System.Random) x y char =
  { RandomField r x y with Content = char; Type = PieceType.Title }


let RenderGrid (state : State) =
    let shiftX, shiftY = CenterShift state
    state.Grid |> Seq.collect (Seq.map (function
        | Field.Empty -> div [ Class "empty" ] []
        | Occupied piece ->
            let bg, shadow, z, cls =
                match state.Items |> List.tryFind (fun i -> i.Left = piece.Left && i.Top = piece.Top ) with
                | Some item ->
                    let c = sprintf "hsl(%d, 80%%, 12%%)" item.Hue
                    c, (sprintf "0px 0px 20px 8px %s" c), "9000", " highlighted"
                | None -> piece.Color, "none", "auto", ""
            div [ Class ("piece" + cls + match piece.Type with PieceType.Title -> " title" | _ -> "")
                  Style [
                    Top (grid2px piece.Top + shiftY)
                    Left (grid2px piece.Left + shiftX)
                    Width SquareSize
                    Height SquareSize
                    BackgroundColor bg
                    BoxShadow shadow
                    ZIndex z
                  ]
                ]
                [ div [ Class "inner" ]
                      [ p [ Class "symbol" ] [ str (string piece.Content) ]
                        //p [ Class "info" ] [ str (string (int piece.Content))]
                      ] ]
        ))
        |> Seq.toList


let RandomLocations (random : Random) (gridW, gridH) =
    let rec getLocationsFrom available = seq {
        if not (Set.isEmpty available) then
            let loc = available |> Set.toSeq |> Seq.sortBy (fun _ -> random.Next()) |> Seq.head
            let surroundings = Surroundings loc |> Set
            yield loc
            yield! getLocationsFrom (Set.difference available surroundings)
    }
    getLocationsFrom (set [ for x in [1..gridW - 2] do
                            for y in [1..gridH - 2] do x, y ])


let RandomItems random dimensions (items : (int * ReactElement) list) =
    let locations = RandomLocations random dimensions |> Seq.take items.Length |> Seq.toList

    List.zip locations items
    |> List.map (fun (location, (hue, item)) -> {
            Left = fst location |> float |> (*) 1.0<Sq>
            Top =  snd location |> float |> (*) 1.0<Sq>
            Content = item
            Hue = hue
    })


let RenderItems state =
    let shiftX, shiftY = CenterShift state
    state.Items
    |> Seq.map (fun item ->
        div [ Class "item"
              Style [
                    Top (grid2px item.Top + shiftY)
                    Left (grid2px item.Left + shiftX)
                    Width SquareSize
                    Height SquareSize
                    Color (sprintf "hsl(%d, 80%%, 40%%)" item.Hue)
              ] ]
            [ item.Content ] )
