[<AutoOpen>]
module Config

open System

let SquareSize = 100<Px>;
let PxPerSq = (float SquareSize) * 1.0<Px/Sq>
let AnimationStep = 0.4<Sq>
let TargetFPS = 40

let private isDisplayChar c =
    Char.IsLetterOrDigit c || Char.IsPunctuation c ||
    match Char.GetUnicodeCategory c with
    | Globalization.UnicodeCategory.MathSymbol
    | Globalization.UnicodeCategory.CurrencySymbol
    | Globalization.UnicodeCategory.ModifierSymbol -> true
    | _ -> false

let _getChars = Seq.map (fun i -> (char) i) >> Seq.filter isDisplayChar >> Seq.toArray
let BasicChars = _getChars [0..127]
let AllChars = _getChars [0..56000]

let BasicCharProbability = 0.8

let PieceColor (random : Random) = sprintf "hsl(0, 0%%, %f%%)" (random.NextDouble() * 4.0 + 3.0)
let HighlightColor = sprintf  "hsl(%d, 70%%, 12%%)"
let HighlightGlow = HighlightColor >> sprintf "0px 0px 17px 5px %s"

let InitialEmptySquare = 0, 0

let ContactEmail = "nbjmup;nbjmAjooju/d{" |> Seq.map (int >> ((+) -1) >> char >> string) |> String.concat ""
let ScLink = "https://soundcloud.com/architech"
let GhLink = "https://github.com/0101"
let SpLink = "https://open.spotify.com/artist/1A9tG644srKp6tTXn4qSBb"

let IdleSeconds = 2.0

let SolverInitialTimeout = 0.5
let SolverMaxTimeout = 2.5
let SolverTimeoutStep = 0.5