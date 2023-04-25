[<AutoOpen>]
module Utils


let mapSnd f (x, y) = x, f y


let coordsToPosition (x: int<Sq>, y: int<Sq>) = int x, int y