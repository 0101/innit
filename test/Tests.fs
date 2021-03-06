module Tests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

open Mechanics


[<Property>]
let ``All random paths between 2 points should have the same length`` (from: Coords) (to': Coords) =

    let random = Random()
    let paths =
        [0..10]
        |> List.map (fun _ -> GetRandomPath random from to')
        |> List.map (fun path -> path, path.Length)
        |> List.groupBy snd

    Assert.Equal (1, paths.Length)


[<Property>]
let ``Non-empty random paths have correct start and end`` (from: Coords) (to': Coords) =
    from <> to' ==> lazy

    let path = GetRandomPath (Random()) from to'

    Assert.Equal (from, path.Head)
    Assert.Equal (to', path |> List.last)


[<Property>]
let ``Random paths are continuous`` (from: Coords) (to': Coords) =
    let path = GetRandomPath (Random()) from to'
    for a, b in path |> List.pairwise do
        Assert.True (axis a b = X || axis a b = Y)


[<Property>]
let ``Random path from and to the same point is empty`` (point : Coords) =
    let path = GetRandomPath (Random()) point point
    Assert.Equal<Path> ([], path)


[<Property>]
let ``Segmented path can be put together again`` (from: Coords) (to': Coords) =
    let unsegmentPath (segments: Path list) =
        seq {
            if not segments.IsEmpty then
                yield segments |> List.head
                yield! segments |> Seq.pairwise |> Seq.map (snd >> List.skip 1)
        } |> Seq.concat |> Seq.toList

    let path = GetRandomPath (Random()) from to'
    let segments = SegmentPath path
    let result = unsegmentPath segments
    Assert.Equal<Path> (path, result)


[<Property>]
let ``Random item locations don't overlap`` gridDimensions =
    for surroundingFunc in [NoSurroundings; XYSurroundings; FullSurroundings] do
        let locs = RandomLocations surroundingFunc (Random()) gridDimensions |> Seq.toList
        Assert.Equal (locs.Length, locs |> List.distinct |> List.length)


[<Property(Arbitrary = [| typeof<IntBetween5and20> |])>]
let ``Random item locations are not next to each other`` gridDimensions =
    let locs = RandomLocations FullSurroundings (Random()) gridDimensions |> Seq.toList
    for l1 in locs do
    for l2 in locs do
    if l1 <> l2 then
        Assert.False ((FullSurroundings l1) |> Set.contains l2, sprintf "Offending locations: %A %A" l1 l2)


[<Property(Arbitrary = [| typeof<IntBetween5and20> |])>]
let ``Item generator can place 4 items`` grid =
    let item = { Hue = 0; Class = "foo"; Content = Link "foo" }
    let result = Rendering.RandomItems (Random()) grid [ item; item; item; item ]
    ()