[<AutoOpen>]
module Utils


let mapSnd f (x, y) = x, f y


module Async =

    let map f computation = async {
        let! result = computation
        return f result
    }