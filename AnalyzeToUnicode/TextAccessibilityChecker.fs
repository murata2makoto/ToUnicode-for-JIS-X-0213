module AnalyzeToUnicode.TextAccessibilityChecker

open System
open System.IO
open System.Text
open System.Globalization

type TextStats = {
    FileName : string
    KangxiCount : int
    SupplementCount : int
    StrokeCount : int
    CompatibilityCount : int
    TotalChars : int
}

let analyzeExtractedText (filePath: string) =
    if not (File.Exists(filePath)) then
        None
    else
        let text = File.ReadAllText(filePath)
        let mutable kangxi = 0
        let mutable supp = 0
        let mutable stroke = 0
        let mutable compat = 0
        let mutable totalCount = 0

        let textEnum = 
            StringInfo.GetTextElementEnumerator(text)
        while textEnum.MoveNext() do
            totalCount <- totalCount + 1
            let element = textEnum.GetTextElement()
            let codePoint = Char.ConvertToUtf32(element, 0)
            
            if codePoint >= 0x2F00 && codePoint <= 0x2FD5 then 
                kangxi <- kangxi + 1
            elif codePoint >= 0x2E80 && codePoint <= 0x2EF3 then 
                supp <- supp + 1
            elif codePoint >= 0x31C0 && codePoint <= 0x31E3 then 
                stroke <- stroke + 1
            elif codePoint >= 0xF900 && codePoint <= 0xFAFF then 
                compat <- compat + 1

        Some {
            FileName = Path.GetFileName(filePath)
            KangxiCount = kangxi
            SupplementCount = supp
            StrokeCount = stroke
            CompatibilityCount = compat
            TotalChars = totalCount
        }

let printTextReport  (ow: StreamWriter) (stats: TextStats) =
    fprintfn ow "================================================================================"
    fprintfn ow "📝 抽出テキスト 汚染コード含有量レポート: %s" stats.FileName
    fprintfn ow "--------------------------------------------------------------------------------"
    let totalBad = 
        stats.KangxiCount + stats.SupplementCount 
        + stats.StrokeCount + stats.CompatibilityCount
    if totalBad = 0 then
        fprintfn ow "✨ 清潔なテキストです！"
    else
        fprintfn ow "🚨 検出数: 康煕部首:%d, 部首補助:%d, CJKの筆画:%d, 互換漢字:%d" 
                stats.KangxiCount stats.SupplementCount 
                stats.StrokeCount stats.CompatibilityCount
        fprintfn ow "🔥 汚染合計: %d 文字 (全体の %.2f%%)" 
            totalBad ((float totalBad / float stats.TotalChars) * 100.0)

let executeBatchVerification
        (existentDirs: DirectoryInfo list) 
        (subDirName: string) =

    let textFiles =
        [ for dirInfo in existentDirs do
            for textFile 
                    in dirInfo.GetFiles("*.txt") do
                if textFile.Exists then 
                    yield textFile
        ]

    let pairs = 
        [ for textFile in textFiles do
            let textFileLocalName = textFile.Name
            let textFileDirName = textFile.DirectoryName
            let analysisSubDir = 
                Path.Combine(textFileDirName, subDirName)
            if not (Directory.Exists analysisSubDir) then
                Directory.CreateDirectory analysisSubDir |> ignore
            textFile.FullName,
            Path.Combine(analysisSubDir, textFileLocalName)
        ]

    for textFile, analysisFile in pairs do
        match analyzeExtractedText textFile with
        | Some stats -> 
            printfn "Analyzing text file: %s" 
                (Path.GetFileName(textFile))
            use outWriter = 
                new StreamWriter(analysisFile, false, Encoding.UTF8)
            printTextReport outWriter stats
        | None -> 
            printfn "⚠️ ファイルが見つかりません: %s" textFile
        

