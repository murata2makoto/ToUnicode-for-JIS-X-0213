module AnalyzeToUnicode.MappingLoader

open System
open System.IO
open System.Text.RegularExpressions 

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
let private trySplitLine (line: string) =
    let trimmed = line.Trim()
    if String.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") then None
    else
        // 最初の # でコメントをパージ
        let noComment = 
            trimmed.Split([| '#' |], 
                StringSplitOptions.None).[0].Trim()
        if String.IsNullOrEmpty(noComment) then None
        else
            // セミコロンで左辺と右辺に分解
            let parts = 
                noComment.Split([| ';' |], 
                    StringSplitOptions.RemoveEmptyEntries)
            match parts |> List.ofArray with
            | left :: right :: _ -> Some (left.Trim().ToUpper(), right.Trim().ToUpper())
            | _ -> None

// EquivalentUnifiedIdeograph.txt 用
let private parseEquivalentLine line =
    match trySplitLine line with
    | None -> []
    | Some (leftPart, rightPart) -> 
        parseKeyValuePairCore leftPart rightPart (fun right -> Some right)

// DerivedNormalizationProps.txt 用
let private parseCompatibilityLine line =
    match trySplitLine line with
    | None -> []
    | Some (leftPart, rightPart) -> 
        // 1. 左辺のコードポイント（またはレンジ開始値）が互換漢字ブロックに属しているか厳格チェック
        let startLeft = 
            leftPart.Split([| ".." |], 
                StringSplitOptions.RemoveEmptyEntries).[0]
        let valLeft = Convert.ToInt32(startLeft, 16)
        let isCompatibilityBlock = (valLeft >= 0xF900 && valLeft <= 0xFAFF) || (valLeft >= 0x2F800 && valLeft <= 0x2FA1F)

        // 2. さらに NFKC_CF プロパティの記述が右辺に含まれているかチェック
        if not isCompatibilityBlock || not (rightPart.Contains("NFKC_CF")) then []
        else
            parseKeyValuePairCore leftPart rightPart (fun right ->
                // 右辺から16進数のコードポイントを抽出
                let hexMatches = 
                        Regex.Matches(right, @"\b[0-9A-FA-f]{4,5}\b")
                if hexMatches.Count = 0 then None
                else
                    let cleanRight = hexMatches.[0].Value
                    // 左辺がレンジなら展開用に開始コードを返し、単一なら右辺のマッチ数が1つのものに限定
                    if leftPart.Contains("..") then Some cleanRight
                    elif hexMatches.Count = 1 then Some cleanRight
                    else None
            )

// --- ④：共通コア関数（データの流し込み） ---
let private loadMappingFile 
        (filePath: string) 
        (parser: string -> (string * string) list) 
        (label: string) =
    if File.Exists(filePath) then
        let lines = File.ReadAllLines(filePath)
        let allPairs = lines |> List.ofArray |> List.collect parser

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
        printfn "Successfully loaded %d %s pairs from %s" 
            allPairs.Length label (Path.GetFileName(filePath))
    else
        printfn "⚠️ Warning: %s が見つかりません。スキップされました。" filePath

// --- ⑤：パブリックAPI ---
let loadEquivalentUnifiedIdeographs filePath = 
    loadMappingFile filePath parseEquivalentLine "Equivalent Unified Ideograph"
let loadCompatibilityIdeographs filePath = 
    loadMappingFile filePath parseCompatibilityLine "Compatibility Ideograph"