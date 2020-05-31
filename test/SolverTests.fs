module SolverTests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

open Mechanics
open Rendering
open Solver


type IntBetween0and3 =
    static member Int32 () = Gen.elements [0..3] |> Arb.fromGen


[<Property(Arbitrary = [| typeof<IntBetween0and3> |])>]
let ``Solver solution works`` () empty piece target =
    empty <> piece ==> lazy

    let gs = {
        GridW = 4
        GridH = 4
        EmptySpace = empty
        Pieces = [{ Position = piece; Targets = set [ target ] }]
    }

    let solution = Solve gs

    let solvedGs = solution |> List.fold (fun gs move -> ApplyMove gs move) gs

    Assert.True (IsSolved solvedGs, sprintf "%A" solution)


[<Property(Arbitrary = [| typeof<IntBetween0and3> |])>]
let ``Solution to a solved state is empty`` gs =
    let gs = { gs with Pieces = [{ Position = (0, 0); Targets = set [ 0, 0 ] }] }

    Assert.True (IsSolved gs)

    let solution = Solve gs

    Assert.Equal<Position list>([], solution)