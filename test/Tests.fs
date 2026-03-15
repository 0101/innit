module Tests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

open Mechanics


[<Property>]
let ``All random paths between 2 points should have the same length`` (seed: int) (from: Coords) (to': Coords) =

    let random = Random(seed)
    let paths =
        [0..10]
        |> List.map (fun _ -> GetRandomPath random from to')
        |> List.map (fun path -> path, path.Length)
        |> List.groupBy snd

    Assert.Equal (1, paths.Length)


[<Property>]
let ``Non-empty random paths have correct start and end`` (seed: int) (from: Coords) (to': Coords) =
    from <> to' ==> lazy

    let path = GetRandomPath (Random(seed)) from to'

    Assert.Equal (from, path.Head)
    Assert.Equal (to', path |> List.last)


[<Property>]
let ``Random paths are continuous`` (seed: int) (from: Coords) (to': Coords) =
    let path = GetRandomPath (Random(seed)) from to'
    for a, b in path |> List.pairwise do
        Assert.True (axis a b = X || axis a b = Y)


[<Property>]
let ``Random path from and to the same point is empty`` (seed: int) (point : Coords) =
    let path = GetRandomPath (Random(seed)) point point
    Assert.Equal<Path> ([], path)


[<Property>]
let ``Segmented path can be put together again`` (seed: int) (from: Coords) (to': Coords) =
    let unsegmentPath (segments: Path list) =
        seq {
            if not segments.IsEmpty then
                yield segments |> List.head
                yield! segments |> Seq.pairwise |> Seq.map (snd >> List.skip 1)
        } |> Seq.concat |> Seq.toList

    let path = GetRandomPath (Random(seed)) from to'
    let segments = SegmentPath path
    let result = unsegmentPath segments
    Assert.Equal<Path> (path, result)


[<Property>]
let ``Random item locations don't overlap`` (seed: int) gridDimensions =
    for surroundingFunc in [NoSurroundings; XYSurroundings; FullSurroundings] do
        let locs = RandomLocations surroundingFunc (Random(seed)) gridDimensions |> Seq.toList
        Assert.Equal (locs.Length, locs |> List.distinct |> List.length)


[<Property(Arbitrary = [| typeof<IntBetween5and20> |])>]
let ``Random item locations are not next to each other`` (seed: int) gridDimensions =
    let locs = RandomLocations FullSurroundings (Random(seed)) gridDimensions |> Seq.toList
    for l1 in locs do
    for l2 in locs do
    if l1 <> l2 then
        Assert.False ((FullSurroundings l1) |> Set.contains l2, sprintf "Offending locations: %A %A" l1 l2)


[<Property(Arbitrary = [| typeof<IntBetween5and40> |])>]
let ``RandomItems places 4 items away from grid edges`` (seed: int) (gridW, gridH) =
    let item = { Hue = 0; Class = "foo"; Content = Link "foo" }
    let items = Rendering.RandomItems (Random(seed)) (gridW, gridH) [ item; item; item; item ]
    items |> List.iter (fun i ->
        let x = int i.Left
        let y = int i.Top
        Assert.True (x >= 1 && x <= gridW - 2, sprintf "x=%d on outer rim for grid %dx%d seed=%d" x gridW gridH seed)
        Assert.True (y >= 1 && y <= gridH - 2, sprintf "y=%d on outer rim for grid %dx%d seed=%d" y gridW gridH seed))
