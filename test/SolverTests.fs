module SolverTests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

open Mechanics
open Workers.Solver


type IntBetween0and3 =
    static member Int32 () = Gen.elements [0..3] |> Arb.fromGen


[<Property(Arbitrary = [| typeof<IntBetween0and3> |])>]
let ``Simple solver solution works`` () empty piece target =
    empty <> piece ==> lazy

    let gs = {
        GridW = 4
        GridH = 4
        EmptySpace = empty
        Pieces = set [{ Position = piece; Target = target }]
    }

    let sType, solution = Solve gs

    let solvedGs = solution |> List.fold (fun gs move -> gs |> ApplyMove move) gs

    Assert.Equal(Complete, sType)
    Assert.True (IsSolved solvedGs, sprintf "%A" solution)


[<Property(Arbitrary = [| typeof<IntBetween0and3> |])>]
let ``Solution to a solved state is empty`` gs =
    let gs = { gs with Pieces = set [{ Position = (0, 0); Target = 0, 0 }] }

    Assert.True (IsSolved gs)

    let sType, solution = Solve gs

    Assert.Equal(Complete, sType)
    Assert.Equal<Position list>([], solution)


[<Property>]
let ``Solution doesn't contain any back&forth moves`` () =
    let w, h = 5, 5
    let locations = FullyRandomLocations (Random()) (w, h) |> Seq.take 7 |> Seq.toArray
    let gs' = {
        GridW = w
        GridH = h
        EmptySpace = locations.[0]
        Pieces = set [
            { Position = locations.[1]; Target = locations.[2] }
            { Position = locations.[3]; Target = locations.[4] }
            { Position = locations.[5]; Target = locations.[6] }
        ]
    }

    let gs = {
        GridW = 5
        GridH = 5
        EmptySpace = (1, 1)
        Pieces = set
                [{ Position = (1, 2)
                   Target = (3, 1) }; { Position = (3, 2)
                                        Target = (2, 1) };
                 { Position = (2, 3)
                   Target = (1, 3) }] }

    let _, solution = Solve gs
    let backAndForth = solution |> List.windowed 3 |> List.filter (fun w -> w <> List.distinct w)
    Assert.True (backAndForth.Length = 0, sprintf "%A /// %A /// %A" gs solution backAndForth)


[<Property>]
let ``Moving back & forth doesn't change the state`` () =
    let w, h = 5, 5
    let locations = FullyRandomLocations (Random()) (w, h) |> Seq.take 7 |> Seq.toArray
    let x, y = locations.[0]
    let gs = {
        GridW = w
        GridH = h
        EmptySpace = x, y
        Pieces = set [
            { Position = locations.[1]; Target = locations.[2] }
            { Position = locations.[3]; Target = locations.[4] }
            { Position = locations.[5]; Target = locations.[6] }
        ]
    }
    let gs' =
        gs
        |> ApplyMove (x + 1, y)
        |> ApplyMove (x, y)

    Assert.Equal (gs, gs')
