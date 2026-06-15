module AnalyzeToUnicode.Program

open System
open System.IO
open AnalyzeToUnicode
open AnalyzeToUnicode.BatchProcessor
open AnalyzeToUnicode.TextAccessibilityChecker

/// 💡 メイン側に配置した一括処理ロジック
/// 出力ディレクトリの変更や、出力スタイルの変更はここを弄るだけで完結します

let createExistentDirs 
        (dirNames: string list) = 
    [ for dirName in dirNames do
        if Directory.Exists dirName then 
            yield new DirectoryInfo(dirName)
        else printfn "⚠️ Warning: ディレクトリ '%s' が見つかりません。スキップします。" dirName
    ]


[<EntryPoint>]
let main argv =
    // --- 1. マッピングデータの準備（UCDファイルのロード） ---
    printfn "🚀 Initializing Unicode Character Database..."
    let equivFile = "f:/ToUnicode-for-JIS-X-0213/AnalyzeToUnicode/EquivalentUnifiedIdeograph.txt"
    MappingLoader.loadEquivalentUnifiedIdeographs equivFile |> ignore
    
    let compatFile = "f:/ToUnicode-for-JIS-X-0213/AnalyzeToUnicode/DerivedNormalizationProps.txt"
    MappingLoader.loadCompatibilityIdeographs compatFile |> ignore

    // --- 2. CMap 汚染のバッチ分析を実行 ---
    if argv.Length = 0 then
        printfn "⚠️ 処理対象のディレクトリが指定されていません。引数にパスを渡してください。"
    else
        let existentDirs = 
            argv 
            |> List.ofArray 
            |> createExistentDirs 
        executeBatchProcessing existentDirs "toUnicode"

        executeBatchVerification existentDirs "textAnalysis"

    printfn "\n✨ すべての検証フェーズが正常に終了しました！"
    0