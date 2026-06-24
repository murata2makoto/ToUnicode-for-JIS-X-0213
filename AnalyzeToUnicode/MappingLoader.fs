module AnalyzeToUnicode.MappingLoader

open System
open System.IO
open System.Text.RegularExpressions 
open System.Reflection

//リソースから全行を読み込み
let private readEmbeddedLines resourceName =
    let asm = Assembly.GetExecutingAssembly()

    use stream = asm.GetManifestResourceStream(resourceName)

    if isNull stream then
        failwith $"Resource not found: {resourceName}"

    use reader = new StreamReader(stream)

    [while not reader.EndOfStream do
            yield reader.ReadLine()
    ]

// --- ①：16進数レンジを同期展開してペアを作る（共通ヘルパー） ---
let private getSynchronizedPairs 
        (startLeftHex: string) 
        (endLeftHex: string)   
        (startRightHex: string) =
    let startLeftVal = Convert.ToInt32(startLeftHex, 16)
    let endLeftVal = Convert.ToInt32(endLeftHex, 16)
    let startRightVal = Convert.ToInt32(startRightHex, 16)
    let count = endLeftVal - startLeftVal
    
    [for i in 0 .. count ->
        let l = sprintf "%04X" (startLeftVal + i)
        let r = sprintf "%04X" (startRightVal + i)
        (l, r)
    ]

// --- ②：【共通】綺麗に分かれた「左辺」と「右辺」を処理する、真のコア関数 ---
// ※行（line）の文字列は一切受け取らず、すでに分離されたデータだけを扱う
let private parseKeyValuePairCore 
        (leftPart: string) 
        (rightPart: string) 
        (cleanRight: string -> string option) =
    
    match cleanRight rightPart with
    | None -> [] 
    | Some cleanRightPart -> 
        let rangeParts = 
            leftPart.Split([| ".." |], 
                StringSplitOptions.RemoveEmptyEntries)
        match rangeParts |> List.ofArray with
        | []  -> []
        | [ _ ]  -> [ (leftPart, cleanRightPart) ] // 単一文字マッピング（クレンジング済みの右辺を使用）
        | startLeft :: endLeft :: _ -> 
            // レンジ（連番）マッピングの同期展開へ
            getSynchronizedPairs 
                (startLeft.Trim()) 
                (endLeft.Trim()) 
                cleanRightPart


// --- ③：各ファイルが「行」を受け取って、左辺・右辺に切り分けてからコアに流す ---

// 💡 共通の「コメント除去 ＆ セミコロン分割」を担うトップレベルのフィルター
// 💡 行をセミコロンで分割してトリムされたトークンの配列を返すだけの、極めて純粋な道具
let private trySplitLine (line: string) : string[] option =
    let trimmed = line.Trim()
    if String.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") then None
    else
        // 行の途中のコメントをカット
        let content = if trimmed.Contains("#") then trimmed.Split('#').[0] else trimmed
        // セミコロンで分割し、空の要素を排除
        let parts = content.Split([| ';' |], StringSplitOptions.RemoveEmptyEntries)
        
        if parts.Length > 0 then
            // 全要素の前後の空白を一括でトリムして配列で返す
            Some (parts |> Array.map (fun p -> p.Trim()))
        else
            None

// EquivalentUnifiedIdeograph.txt 用
let private parseEquivalentLine line =
    match trySplitLine line with
    // 💡 配列が「左辺」「右辺」の2つの要素だけで構成されている場合
    | Some [| leftPart; rightPart |] ->
        // 従来の等価マッピング処理
        if not (leftPart.Contains("..")) then
            [ (leftPart, rightPart) ]
        else []
    | _ -> []

// DerivedNormalizationProps.txt 用
let private parseCompatibilityLine line =
    match trySplitLine line with
    // 💡 配列が「左辺」「プロパティ名」「右辺」の3つの要素で構成されている場合
    | Some [| leftPart; propName; rightPart |] ->
        
        if propName = "NFKC_CF" || propName = "NFKC_SCF" then
            if not (leftPart.Contains("..")) then
                let rightTokens = rightPart.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
                if rightTokens.Length > 0 then
                    [ (leftPart, rightTokens.[0].Trim()) ]
                else []
            else []
        else []
    | _ -> [] // 要素数が合わない行や None はすべて安全にスルー


// --- ④：共通コア関数（データの流し込み） ---
let private loadMappingFile 
        (resourceName: string) 
        (parser: string -> (string * string) list) 
        (label: string) =
    let allPairs = 
        readEmbeddedLines resourceName 
        |> List.collect parser

    let mutable eToU = Mappings.equivToUnifiedMap
    let mutable uToE = Mappings.unifiedToEquivMap
    let mutable targets = Mappings.allTargetCodes

    for (equivCode, unifiedCode) in allPairs do
        eToU <- eToU.Add(equivCode, unifiedCode)
        uToE <- uToE.Add(unifiedCode, equivCode)
        targets <- targets.Add(equivCode).Add(unifiedCode)

    Mappings.equivToUnifiedMap <- eToU
    Mappings.unifiedToEquivMap <- uToE
    Mappings.allTargetCodes <- targets
    printfn "Successfully loaded %d %s pairs" 
        allPairs.Length label

// --- ⑤：パブリックAPI ---
let loadEquivalentUnifiedIdeographs () = 
    loadMappingFile 
        "AnalyzeToUnicode.data.EquivalentUnifiedIdeograph.txt" 
        parseEquivalentLine "Equivalent Unified Ideograph"
let loadCompatibilityIdeographs () = 
    loadMappingFile 
        "AnalyzeToUnicode.data.DerivedNormalizationProps.txt" 
        parseCompatibilityLine "Compatibility Ideograph"