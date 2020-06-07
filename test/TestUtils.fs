[<AutoOpen>]
module TestUtils

open FsCheck


type IntBetween0and3 =
    static member Int32 () = Gen.elements [0..3] |> Arb.fromGen


type IntBetween5and20 =
    static member Int32 () = Gen.elements [5..20] |> Arb.fromGen


type IntBetween100and2000 =
    static member Int32 () = Gen.elements [100..2000] |> Arb.fromGen