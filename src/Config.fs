[<AutoOpen>]
module Config

open System

let SquareSize = 100;
let PxPerSq = (float SquareSize) * 1.0<Px/Sq>
let AnimationStep = 0.34<Sq>;

let _getChars = Seq.map (fun i -> (char) i) >> Seq.filter (Char.IsControl >> not) >> Seq.toArray
let BasicChars = _getChars [0..127]
let AllChars = _getChars [0..56000]

let BasicCharProbability = 0.8

let PieceColor (random : Random) = sprintf "hsl(0, 0%%, %f%%)" (random.NextDouble() * 4.0 + 2.0)

let InitialEmptyField = 0, 0

let ContactEmail = "nbjmup;nbjmAjooju/d{" |> Seq.map (int >> ((+) -1) >> char >> string ) |> String.concat ""
let ScLink = "https://soundcloud.com/architech"
let GhLink = "https://github.com/0101"