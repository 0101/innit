module SolverTests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

open Mechanics
open Solver

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
            { Position = locations.[1]; Targets = [|locations.[2]|] }
            { Position = locations.[3]; Targets = [|locations.[4]|] }
            { Position = locations.[5]; Targets = [|locations.[6]|] }
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
            { Position = locations.[1]; Targets = [|locations.[2]|] }
            { Position = locations.[3]; Targets = [|locations.[4]|] }
            { Position = locations.[5]; Targets = [|locations.[6]|] }
        ]
    }
    let gs' =
        gs
        |> ApplyMove (x + 1, y)
        |> ApplyMove (x, y)

    Assert.Equal (gs, gs')


[<Property(Arbitrary = [| typeof<IntBetween100and400> |])>]
let ``Solver can solve real world use cases`` screen =
    let state, _ = Init.initialSetup screen false |> Update.update Shuffle

    let gs = CreateGameState state
    let sType, solution = Solve (gs, SolverMaxTimeout * 2.0)

    match sType with
    | Complete ->
        let solvedGs = solution |> List.fold (fun gs move -> gs |> ApplyMove move) gs
        Assert.True (IsSolved solvedGs, sprintf "Complete but not solved: %A" solvedGs)
    | Partial _ ->
        ()


[<Property(Arbitrary = [| typeof<IntBetween100and2000> |])>]
let ``Chained solve eventually completes or stays partial`` screen =
    let state, _ = Init.initialSetup screen false |> Update.update Shuffle
    let initialGs = CreateGameState state

    let rec chain gs iteration =
        if iteration >= 20 then None
        else
            match Solve (gs, 0.1) with
            | Complete, solution -> Some (gs, solution)
            | Partial partialGs, _ -> chain partialGs (iteration + 1)

    match chain initialGs 0 with
    | Some (lastInputGs, finalSolution) ->
        let solvedGs = finalSolution |> List.fold (fun gs move -> gs |> ApplyMove move) lastInputGs
        Assert.True (IsSolved solvedGs, sprintf "Chained solve reached Complete but not IsSolved: %A" solvedGs)
    | None ->
        ()


[<Fact>]
let ``Two pieces with shared targets on 4x4 grid are solved correctly`` () =
    let gs = {
        GridW = 4
        GridH = 4
        EmptySpace = (0, 0)
        Pieces = set [
            { Position = (2, 1); Targets = [|(1, 1); (1, 2)|] }
            { Position = (2, 2); Targets = [|(1, 1); (1, 2)|] }
        ]
    }

    let sType, solution = Solve (gs, SolverMaxTimeout * 2.0)

    Assert.Equal(Complete, sType)
    let solvedGs = solution |> List.fold (fun gs move -> gs |> ApplyMove move) gs
    Assert.True (IsSolved solvedGs, sprintf "Shared-target pieces not solved: %A" solvedGs)
