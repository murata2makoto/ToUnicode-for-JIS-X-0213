open System
open System.IO
open AnalyzeToUnicode
open AnalyzeToUnicode.CMapParser
open AnalyzeToUnicode.Analyzer
open AnalyzeToUnicode.MappingLoader
// Program.fs 内（getFonts の手前に追加）

// 各グループの出力を共通化したヘルパー関数
let private printGroup 
        (ow: StreamWriter) 
        (label: string) 
        (predicate: CMapLine -> bool) 
        (badLines: CMapLine list) =
    let filteredLines = badLines |> List.filter predicate
    if not (List.isEmpty filteredLines) then
        fprintfn ow "  [▼ %s]" label
        for line in filteredLines do
            match line with
            | BfChar(c, u) -> 
                fprintfn 
                    ow 
                    "     CID: <%s> -> Uni: <%s> (%s)"
                    c u (toCharStr u)
            | BfRangeList(s, e, us) -> 
                let chars = us |> List.map toCharStr |> String.concat ", "
                fprintfn 
                    ow 
                    "     CID: <%s>-<%s> -> UniList: %A (%s)"
                    s e us chars
            | BfRangeCalculated(s, e, su) -> 
                fprintfn 
                    ow 
                    "     CID: <%s>-<%s> -> StartUni: <%s> (連番: 開始文字=%s)"
                    s e su (toCharStr su)

let private printFontHeader ow fontId fontName toUnicodeOpt =
    fprintfn ow 
        "\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    fprintfn ow 
        "🚨 フォント [ID: %d] (名前: /%s) [ToUnicode ID: %s]" 
        fontId fontName 
        (match toUnicodeOpt with Some r -> string r.Id | None -> "なし")
    fprintfn ow 
        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
                    
// 各グループの条件定義（パターンマッチ用の部分適用)
// 各グループの条件定義（小文字コードポイント対応・完全排他版)
let isKx = function
    | BfChar(_, u) -> u.StartsWith("2F")
    | BfRangeList(_, _, us) -> 
        List.exists (fun (u: string) -> u.StartsWith("2F")) us
    | BfRangeCalculated(_, _, su) -> 
        su.StartsWith("2F")

let isRs = function
    | BfChar(_, u) -> u.StartsWith("2E")
    | BfRangeList(_, _, us) -> 
        List.exists (fun (u: string) -> u.StartsWith("2E")) us
    | BfRangeCalculated(_, _, su) -> 
        su.StartsWith("2E")

let isComp = function
    | BfChar(_, up) -> 
        Mappings.equivToUnifiedMap.ContainsKey(up) 
        && not (up.StartsWith("2F") || up.StartsWith("2E"))
    | BfRangeList(_, _, us) -> 
        List.exists 
            (fun (up: string) -> 
                Mappings.equivToUnifiedMap.ContainsKey(up) 
                && not (up.StartsWith("2F") || up.StartsWith("2E"))) 
            us
    | BfRangeCalculated(_, _, up) -> 
        Mappings.equivToUnifiedMap.ContainsKey(up) 
        && not (up.StartsWith("2F") || up.StartsWith("2E"))

let isCjk = function
    | BfChar(_, u) -> 
        Mappings.unifiedToEquivMap.ContainsKey(u)
    | BfRangeList(_, _, us) -> 
        List.exists 
            (fun (u: string) -> 
                Mappings.unifiedToEquivMap.ContainsKey(u)) 
            us
    | BfRangeCalculated(_, _, su) -> 
        Mappings.unifiedToEquivMap.ContainsKey(su)

let getFonts rootName targetFile (ow: StreamWriter) =
    fprintfn ow "Analyzing fonts for %s" rootName 
    let fonts, cmaps = parsePdfTextFile targetFile

    for fontId, fontName, toUnicodeOpt in fonts do
        match toUnicodeOpt with
        | Some ref ->
            match cmaps.TryFind(ref.Id) with
            | Some lines ->
                let badLines = checkTargetInCMap lines
                if not (List.isEmpty badLines) then
                    printFontHeader ow fontId fontName toUnicodeOpt

                    // 共通関数に条件とラベルを流し込んで一気に出力
                    badLines 
                    |> printGroup ow "🔴 康煕部首への誤マッピング (U+2F00～)" isKx
                    badLines 
                    |> printGroup ow "🟡 CJK部首補助への誤マッピング (U+2E80～)" isRs
                    badLines 
                    |> printGroup ow "🟠 CJK互換漢字へのマッピング (U+F900～ / U+FA00～)" isComp
                    badLines 
                    |> printGroup ow "🟢 正常なCJK統合漢字へのマッピング" isCjk
            | None -> ()
        | None -> ()

[<EntryPoint>]
let main argv =
    let dirName = "f:/ToUnicode-for-JIS-X-0213/HimorinExperiment/"
    
    // --- 1. マッピングデータの準備（2つのファイルから連続流し込み） ---
    
    // ① 従来の等価統合漢字ファイル（部首・部首補助・筆画）
    let equivFile = "f:/ToUnicode-for-JIS-X-0213/AnalyzeToUnicode/EquivalentUnifiedIdeograph.txt"
    loadEquivalentUnifiedIdeographs equivFile
    
    // ② 【追加！】標準等価正規化ファイル（互換漢字）
    // ※ UCDからダウンロードした DerivedNormalizationProps.txt のパスを指定してください
    let compatFile = "f:/ToUnicode-for-JIS-X-0213/AnalyzeToUnicode/DerivedNormalizationProps.txt"
    loadCompatibilityIdeographs compatFile

    printfn "--- 全マッピングデータの準備が完了しました ---"
    printfn "総ターゲット文字数: %d" Mappings.allTargetCodes.Count


    let rootNames = [
        "chrome-bizud-all"; "chrome-bizud-limit";
        "chrome-line-all"; "chrome-line-limit";
        "chrome-mplus-all"; "chrome-mplus-limit";
        "chrome-noto-all"; "chrome-noto-limit";
        "firefox-bizud-all"; "firefox-bizud-limit";
        "firefox-line-all"; "firefox-line-limit";
        "firefox-mplus-all"; "firefox-mplus-limit";
        "firefox-noto-all"; "firefox-noto-limit"
    ] 
    
    let pdfFileNames =
        rootNames
        |> List.map (fun name -> 
                        name,
                        sprintf "%s%s-uncompressed.pdf" dirName name,
                        sprintf "%s%s-analysis.txt" dirName name)
                        
    printfn "Starting batch analysis for %d files..." pdfFileNames.Length
    
    // 指定されたファイルを順番に処理
    for rootName, pdfFile, outputFile in pdfFileNames do
        if File.Exists pdfFile then
            printfn 
                "  Analyzing: %s-uncompressed.pdf -> %s-analysis.txt"
                rootName rootName
            use outWriter = new StreamWriter(outputFile)
            getFonts rootName pdfFile outWriter
        else
            printfn "  ⚠️ Skip: %s が見つかりません" pdfFile
            
    printfn "Batch analysis completed successfully."
    0