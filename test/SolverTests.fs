module SolverTests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

open Mechanics
open Workers.Solver


[<Property(Arbitrary = [| typeof<IntBetween0and3> |])>]
let ``Simple solver solution works`` () empty piece target =
    empty <> piece ==> lazy

    let gs = {
        GridW = 4
        GridH = 4
        EmptySpace = empty
        Pieces = set [{ Position = piece; Targets = [|target|] }]
    }

    let sType, solution = Solve (gs, SolverInitialTimeout)

    let solvedGs = solution |> List.fold (fun gs move -> gs |> ApplyMove move) gs

    Assert.Equal(Complete, sType)
    Assert.True (IsSolved solvedGs, sprintf "%A" solution)


[<Property(Arbitrary = [| typeof<IntBetween0and3> |])>]
let ``Solution to a solved state is empty`` gs =
    let gs = { gs with Pieces = set [{ Position = (0, 0); Targets = [|0, 0|] }] }

    Assert.True (IsSolved gs)

    let sType, solution = Solve (gs, SolverInitialTimeout)

    Assert.Equal(Complete, sType)
    Assert.Equal<Position list>([], solution)


[<Property>]
let ``Solution doesn't contain any back&forth moves`` () =
    let w, h = 5, 5
    let locations = FullyRandomLocations (Random()) (w, h) |> Seq.take 7 |> Seq.toArray
    let gs = {
        GridW = w
        GridH = h
        EmptySpace = locations.[0]
        Pieces = set [
            { Position = locations.[1]; Targets = [| locations.[2] |] }
            { Position = locations.[3]; Targets = [| locations.[4] |] }
            { Position = locations.[5]; Targets = [| locations.[6] |] }
        ]
    }
    let _, solution = Solve (gs, SolverInitialTimeout)
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
            { Position = locations.[1]; Targets = [| locations.[2] |] }
            { Position = locations.[3]; Targets = [| locations.[4] |] }
            { Position = locations.[5]; Targets = [| locations.[6] |] }
        ]
    }
    let gs' =
        gs
        |> ApplyMove (x + 1, y)
        |> ApplyMove (x, y)

    Assert.Equal (gs, gs')


[<Property(Arbitrary = [| typeof<IntBetween100and2000> |])>]
let ``Solver can solve real world use cases`` screen =
    let state, _ = Init.initialSetup screen false |> Update.update Shuffle

    let gs = CreateGameState state
    let sType, solution = Solve (gs, SolverMaxTimeout * 2.0)

    Assert.True ((Complete = sType), sprintf "%A" gs)
