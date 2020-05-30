module Mechanics

open System

let rec GetShortestPaths ((ax, ay) : Coords) ((bx, by) : Coords) =
    let xDiff = bx - ax
    let yDiff = by - ay
    let xDistance = abs xDiff
    let xDirection = if xDistance <> 0<Sq> then xDiff / xDistance else 0
    let yDistance = abs yDiff
    let yDirection = if yDistance <> 0<Sq> then yDiff / yDistance else 0

    match int xDiff, int yDiff with
    | 0, 0 -> Seq.empty
    | 0, _ -> Seq.singleton [for d in [1..int(yDistance)] -> ( ax, ay + yDirection * d * 1<Sq>) ]
    | _, 0 -> Seq.singleton [for d in [1..int(xDistance)] -> ( ax + xDirection * d * 1<Sq>, ay) ]
    | _ -> seq {
        let moveX = ax + 1<Sq> * xDirection, ay
        for p in GetShortestPaths moveX (bx, by) -> moveX::p

        let moveY = ax, ay + 1<Sq> * yDirection
        for p in GetShortestPaths moveY (bx, by) -> moveY::p
    }

let GetRandomPath random (from : Coords) (to' : Coords) : Path =

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


let SegmentPath (path: Path) : Path list =
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
