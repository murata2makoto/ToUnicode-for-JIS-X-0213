module AnalyzeToUnicode.BatchProcessor

open System
open System.IO
open AnalyzeToUnicode.CMapParser
open AnalyzeToUnicode.ReportGenerator

// 💡 CMapLineのリストを (CID, Unicode) のフラットなペアリストに展開する（純粋なデータ変換）
let private transformToReportData (lines: CMapLine list) : (string * string) list =
    lines 
    |> List.collect (function
        | BfChar(c, u) -> [ (c, u) ]
        | BfRangeList(s, e, us) -> 
            us |> List.mapi (fun i u -> 
                let cidVal = Convert.ToInt32(s, 16) + i
                let cidHex = sprintf "%04X" cidVal
                (cidHex, u)
            )
        | BfRangeCalculated(s, e, su) ->
            let startCid = Convert.ToInt32(s, 16)
            let endCid = Convert.ToInt32(e, 16)
            let startUni = Convert.ToInt32(su, 16)
            [ for i in 0 .. (endCid - startCid) -> 
                let cidHex = sprintf "%04X" (startCid + i)
                let uniHex = sprintf "%04X" (startUni + i)
                (cidHex, uniHex)
            ]
    )

/// 💡 外部（Program.fs）から呼び出される単一ファイル処理のエンドポイント
/// パスの決定権はすべて呼び出し側（Program.fs）に委ねる設計
let private processSingleFile targetFile (ow: StreamWriter) (csvWriter: StreamWriter) =
    let fonts, cmaps = parsePdfTextFile targetFile

    let fontsDataForReport = 
        fonts 
        |> List.choose (fun (fontId, fontName, toUnicodeOpt) ->
            match toUnicodeOpt with
            | Some ref ->
                match cmaps.TryFind(ref.Id) with
                | Some lines ->
                    let mappings = transformToReportData lines
                    Some (string fontId, fontName, string ref.Id, mappings)
                | None -> None
            | None -> None
        )

    if not (List.isEmpty fontsDataForReport) then
        generateAccessibilityReport ow fontsDataForReport |> ignore
        generateCsvReport fontsDataForReport csvWriter |> ignore
    else
        fprintfn ow "⚠️ 解析対象のフォントオブジェクト（ToUnicodeを持つもの）が見つかりませんでした。"


let executeBatchProcessing 
         (existentDirs: DirectoryInfo list) 
        (subDirName: string) =

    let uncompressedPdfFiles =
        [ for dirInfo in existentDirs do
            for uncompressedPdfFile 
                    in dirInfo.GetFiles("*-uncompressed.pdf") do
                if uncompressedPdfFile.Exists then 
                    yield uncompressedPdfFile
        ]
    // 💡 ここで「出力先をどこにするか」のルールを自由に変更可能！
    let triplets = 
        uncompressedPdfFiles
        |> List.map 
            (fun pdfFile -> 
                let pdfFileLocalName = pdfFile.Name
                let pdfFileDirName = pdfFile.DirectoryName
                let analysisSubDir = Path.Combine(pdfFileDirName, subDirName)
                if not (Directory.Exists analysisSubDir) then
                    Directory.CreateDirectory(analysisSubDir) |> ignore
                let originalRootName = pdfFileLocalName.Replace("-uncompressed.pdf", "")
                pdfFile.FullName,
                sprintf "%s/%s.txt" analysisSubDir originalRootName,
                sprintf "%s/%s.csv" analysisSubDir originalRootName
            )
 
    for pdfFile, outputFile, csvFile in triplets do
        if File.Exists pdfFile then
            printfn "Analyzing CMap: %s" (Path.GetFileName(pdfFile))
            use outWriter = new StreamWriter(outputFile, false, System.Text.Encoding.UTF8)
            use csvWriter = new StreamWriter(csvFile, false, System.Text.Encoding.UTF8)
            
            // コアロジックのモジュールを呼び出す
            processSingleFile pdfFile outWriter csvWriter
        else
            printfn "⚠️ Skip: %s が見つかりません" pdfFile
