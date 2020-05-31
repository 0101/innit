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


let Surroundings (x, y) = set [
    x - 1, y - 1; x, y - 1; x + 1, y - 1
    x - 1, y    ; x, y    ; x + 1, y
    x - 1, y + 1; x, y + 1; x + 1, y + 1
]