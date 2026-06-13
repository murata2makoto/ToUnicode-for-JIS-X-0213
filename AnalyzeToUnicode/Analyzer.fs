module AnalyzeToUnicode.Analyzer

open System
open System.Text.RegularExpressions

let isTargetUnicode (uni: string) =
    if String.IsNullOrEmpty uni then false
    else Mappings.allTargetCodes.Contains(uni)

// 16進数文字列を数値（int）に安全に変換するヘルパー
let private hexToInt (hex: string) = 
    Convert.ToInt32(hex, 16)

// 数値を16進数文字列（4桁の固定幅、小文字）に戻すヘルパー
// ※ プログラム側の仕様に合わせて 大文字にする場合は "X4" にしてください
let private intToHex (value: int) = 
    value.ToString("x4")
    
let checkTargetInCMap (lines: CMapLine list) =
    lines 
    |> List.filter (function
        | BfChar(_, uni) -> isTargetUnicode uni
        | BfRangeList(_, _, uniList) -> List.exists isTargetUnicode uniList
        | BfRangeCalculated(s, e, su) -> 
            // 1. CIDの差分から、このレンジに含まれる「総文字数」を算出
            let startCidVal = hexToInt s
            let endCidVal = hexToInt e
            let count = endCidVal - startCidVal
            
            // 2. 開始Unicodeの数値を基点にする
            let startUniVal = hexToInt su
            
            // 3. 0 から count までの連番を生成し、Unicodeをインクリメントしながら全文字チェック
            seq { 0 .. count }
            |> Seq.map (fun offset -> (startUniVal + offset) |> intToHex)
            |> Seq.exists isTargetUnicode
    )

let toCharStr (hexStr: string) =
    try
        let codePoint = Int32.Parse(hexStr, System.Globalization.NumberStyles.HexNumber)
        Char.ConvertFromUtf32 codePoint
    with
    | _ -> "？"