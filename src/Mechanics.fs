module Mechanics

open System


let GetRandomPath random (from : Coords) (to' : Coords) : Path =
    if from = to' then [] else

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
    if   ax = bx then X
    elif ay = by then Y
    else failwith "Non continuous path"


let SegmentPath path =
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


let FullSurroundings (x, y) = set [
    x - 1, y - 1; x, y - 1; x + 1, y - 1
    x - 1, y    ; x, y    ; x + 1, y
    x - 1, y + 1; x, y + 1; x + 1, y + 1
]

let xySurroundings (x, y) = set [
    x, y - 1
    x - 1, y    ; x, y    ; x + 1, y
    x, y + 1
]

let noSurroundings xy = set [ xy ]


let RandomLocations surroundings (random : Random) (gridW, gridH) =
    let rec getLocationsFrom available = seq {
        if not (Set.isEmpty available) then
            let loc = available |> Set.toSeq |> Seq.sortBy (fun _ -> random.Next()) |> Seq.head
            let removeLocations = surroundings loc |> Set
            yield loc
            yield! getLocationsFrom (Set.difference available removeLocations)
    }
    getLocationsFrom (set [ for x in [1..gridW - 2] do
                            for y in [1..gridH - 2] do x, y ])


let FullyRandomLocations r = RandomLocations noSurroundings r


let GetPieces grid =
     set [ for x, row in grid |> Array.mapi (fun x r -> x, r) do
           for y, field in row |> Array.mapi (fun y f -> y, f) do
           match field with
           | Occupied piece when piece.Type = Title -> (x, y), piece
           | _ -> () ]


let GridWidth grid = Array.length grid

let GridHeight (grid: 'a[][]) = grid.[0].Length


let Swap (grid: Field[][]) (x1, y1) (x2, y2) =
    let p1 = match grid.[x1].[y1] with
             | Empty -> Empty
             | Occupied p -> Occupied { p with Left = float x2 * 1.0<Sq>; Top = float y2 * 1.0<Sq>  }
    let p2 = match grid.[x2].[y2] with
             | Empty -> Empty
             | Occupied p -> Occupied { p with Left = float x1 * 1.0<Sq>; Top = float y1 * 1.0<Sq>  }
    grid.[x2].[y2] <- p1
    grid.[x1].[y1] <- p2


let ToCoords (x, y) = x * 1<Sq>, y * 1<Sq>


let IsValidMove state = function
    | [] -> true
    | x::xs -> x = state.EmptyField
