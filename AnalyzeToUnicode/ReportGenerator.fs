module AnalyzeToUnicode.ReportGenerator

open System
open System.IO

// 💡 解決策：すべての土台となる判定ロジックを、モジュールの「最上部」に配置します。
// これにより、下方に定義するすべての関数（calculateStatsなど）から確実に参照可能になります。
let private categorizeMapping (uniHex: string) =
    let v = Convert.ToInt32(uniHex, 16)
    
    if v >= 0x2F00 && v <= 0x2FDF then KangxiRadical
    elif v >= 0x2E80 && v <= 0x2EF3 then RadicalSupplement
    elif v >= 0x31C0 && v <= 0x31EF then CjkStroke
    elif (v >= 0xF900 && v <= 0xFAFF) 
        || (v >= 0x2F800 && v <= 0x2FA1F) then Compatibility
    elif (v >= 0x4E00 && v <= 0x9FFF) 
        || (v >= 0x3400 && v <= 0x4DBF) 
        || (v >= 0x20000 && v <= 0x2A6DF) then
        
        // 康煕部首の相方か？
        if Mappings.unifiedToEquivMap.ContainsKey(uniHex) && 
            (let eq = Mappings.unifiedToEquivMap.[uniHex]  
             let eqVal = Convert.ToInt32(eq, 16)  
             eqVal >= 0x2F00 && eqVal <= 0x2FDF) then
            KanjiUnifiedForKangxiRadical
            
        // CJK部首補助の相方か？
        elif Mappings.unifiedToEquivMap.ContainsKey(uniHex) && 
              (let eq = Mappings.unifiedToEquivMap.[uniHex]
               let eqVal = Convert.ToInt32(eq, 16)
               eqVal >= 0x2E80 && eqVal <= 0x2EF3) then
            KanjiUnifiedForRadicalSupplement
            
        // CJK互換漢字ブロックからの往復マッピング先か？
        elif Mappings.equivToUnifiedMap |> Map.exists (fun _ v -> v = uniHex) then
            KanjiUnifiedForCompatibility
            
        else
            Other
    else 
        Other

let private getHumanReadableHint (corruptedUniHex: string) =
    match Mappings.equivToUnifiedMap.TryFind(corruptedUniHex) with
    | Some originalUnifiedHex ->
        try
            let charCode = Convert.ToInt32(originalUnifiedHex, 16)
            let originalChar = Char.ConvertFromUtf32(charCode)
            
            // 💡 コードポイントを「U+XXXX」の形式に整形（16進数大文字4桁以上）
            //4桁未満のパディングを維持しつつ、5桁も想定している
            let uniCodePointStr = sprintf "%04X" charCode
            
            // 💡 文字とコードポイントの両方をヒント文字列に含める
            sprintf " 💡 (相当する統合漢字: %s <%s>)" 
                originalChar uniCodePointStr
        with _ -> ""
    | None -> ""

// 💡 1. 【集計】データパース結果から統計レコードのリストを計算する
let private calculateStats 
    (fontsData: (string * string * string * (string * string) list) list) : FontStats list =
    fontsData 
    |> List.map (fun (fontId, fontName, toUnicodeId, mappings) ->
        let stats = {
            FontId = fontId; FontName = fontName; ToUnicodeId = toUnicodeId
            KangxiRadicalCount = 0; RadicalSupplementCount = 0; CjkStrokeCount = 0; CompatibilityCount = 0
            UnifiedForKangxiCount = 0; UnifiedForSupplementCount = 0; UnifiedForStrokeCount = 0; UnifiedForCompatibilityCount = 0
            OtherCount = 0
        }
        for (_, uniHex) in mappings do
            match categorizeMapping uniHex with
            | KangxiRadical -> 
                stats.KangxiRadicalCount <- stats.KangxiRadicalCount + 1
            | RadicalSupplement -> 
                stats.RadicalSupplementCount      <- stats.RadicalSupplementCount + 1
            | CjkStroke -> 
                stats.CjkStrokeCount              <- stats.CjkStrokeCount + 1
            | Compatibility -> 
                stats.CompatibilityCount          <- stats.CompatibilityCount + 1
            | KanjiUnifiedForKangxiRadical -> 
                stats.UnifiedForKangxiCount       <- stats.UnifiedForKangxiCount + 1
            | KanjiUnifiedForRadicalSupplement -> 
                stats.UnifiedForSupplementCount   <- stats.UnifiedForSupplementCount + 1
            | KanjiUnifiedForCjkStroke -> 
                stats.UnifiedForStrokeCount       <- stats.UnifiedForStrokeCount + 1
            | KanjiUnifiedForCompatibility -> 
                stats.UnifiedForCompatibilityCount <- stats.UnifiedForCompatibilityCount + 1
            | Other -> 
                stats.OtherCount                  <- stats.OtherCount + 1
        stats
    )

// 💡 2. 【画面出力：明細】地雷文字の逆引き明細を描画する
let generateAccessibilityReport 
        (ow: StreamWriter) 
        (fontsData: (string * string * string * (string * string) list) list)=
    fprintfn ow "\n================================================================================"
    fprintfn ow "🔍 各フォント内部 CMap 汚染明細（逆引き検証）"
    fprintfn ow "================================================================================"
    
    for (fontId, fontName, toUnicodeId, mappings) in fontsData do
        let hasIssue = mappings |> List.exists (fun (_, uniHex) -> 
            match categorizeMapping uniHex with
            | KangxiRadical | RadicalSupplement | CjkStroke -> true
            | _ -> false)

        if hasIssue then
            fprintfn ow "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
            fprintfn ow "🚨 汚染検出 フォント [ID: %s] (名前: %s) [ToUnicode ID: %s]" 
                fontId fontName toUnicodeId
            fprintfn ow "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
            
            let groupedMappings = 
                mappings |> List.groupBy (fun (_, uniHex) -> categorizeMapping uniHex)
            for (category, pairs) in groupedMappings do
                match category with
                | KangxiRadical ->
                    fprintfn ow "  [▼ 🔴 康煕部首へのマッピング (U+2F00～)]"
                    for (cid, uni) in pairs do
                        let hint = getHumanReadableHint uni
                        let glyph = 
                            try Char.ConvertFromUtf32(Convert.ToInt32(uni, 16)) with _ -> "?"
                        fprintfn ow "     CID: <%s> -> Uni: <%s> (%s)%s" 
                                    cid uni glyph hint
                | RadicalSupplement ->
                    fprintfn ow "  [▼ 🟡 CJK部首補助へのマッピング (U+2E80～)]"
                    for (cid, uni) in pairs do
                        let hint = getHumanReadableHint uni
                        let glyph = 
                            try Char.ConvertFromUtf32(Convert.ToInt32(uni, 16)) with _ -> "?"
                        fprintfn ow "     CID: <%s> -> Uni: <%s> (%s)%s" 
                                    cid uni glyph hint
                | CjkStroke ->
                    fprintfn ow "  [▼ 🎨 CJK Strokesへのマッピング (U+31C0～)]"
                    for (cid, uni) in pairs do
                        fprintfn ow "     CID: <%s> -> Uni: <%s> (筆画パーツ)" cid uni
                | _ -> ()
        else
            fprintfn ow "✨ 清潔フォント [ID: %s] (名前: %s) ── 汚染なし" 
                            fontId fontName


// 3. 【Excel用 CSV出力】マトリクス形式でフォントごとの集計を一行で出力する
let generateCsvReport  (fontsData: (string * string * string * (string * string) list) list) (csvWriter: StreamWriter) =
    let fontStatsList = calculateStats fontsData
    
    // Excelで直接開いたときの文字化けを防ぐため、BOMを書き込む（ストリーム開始時のみでOKですが、安全のため明示）
    // ※呼び出し側で Encoding.UTF8 を指定して StreamWriter を作るとより確実です。
    
    // ヘッダー行の出力（Excelでパッと見てわかるカラム名）
    let header = "フォントID,フォント名,ToUnicodeID," +
                    "🔴康煕部首,🔴部首補助,🔴CJK筆画,🔴互換漢字," +
                    "🟢康煕部首相当統合漢字,🟢部首補助相当統合漢字,🟢筆画相当統合漢字,🟢互換漢字相当統合漢字," +
                    "⚪その他一般文字"
    fprintfn csvWriter "%s" header

    // データ行の出力
    for stats in fontStatsList do

        fprintfn csvWriter "%s,\"%s\",%s,%d,%d,%d,%d,%d,%d,%d,%d,%d"
                    stats.FontId
                    (stats.FontName.Replace("\"", "\"\"")) // カンマやクォーテーションのエスケープ
                    stats.ToUnicodeId
                    stats.KangxiRadicalCount
                    stats.RadicalSupplementCount
                    stats.CjkStrokeCount
                    stats.CompatibilityCount
                    stats.UnifiedForKangxiCount
                    stats.UnifiedForSupplementCount
                    stats.UnifiedForStrokeCount
                    stats.UnifiedForCompatibilityCount
                    stats.OtherCount