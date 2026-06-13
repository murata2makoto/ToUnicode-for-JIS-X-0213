namespace AnalyzeToUnicode

// --- ① 純粋なデータ構造の定義 ---
type ObjRef = { Id: int; Gen: int }

type CMapLine =
    | BfChar of 
        cid: string * uni: string
    | BfRangeList of 
        startCid: string * endCid: string * uniList: string list
    | BfRangeCalculated of 
        startCid: string * endCid: string * startUni: string

type PdfObject =
    | FontObj of 
        id: int * fontName: string * toUnicode: ObjRef option
    | CMapStreamObj of 
        id: int * lines: CMapLine list
    | OtherObj

// --- ② データを保持する状態（State）のみを定義 ---
module Mappings =

    // 1. 「部首・筆画・互換漢字」から「通常のCJK統合漢字」へ変換するマップ
    // 例: "2F00" (康煕部首) -> "4E00" (統合漢字) / "FA0E" (互換漢字) -> "5022" (統合漢字)
    let mutable equivToUnifiedMap = Map.empty<string, string>

    // 2. 「通常のCJK統合漢字」から「部首・筆画・互換漢字」への逆引きマップ
    // 例: "4E00" -> "2F00" / "5022" -> "FA0E"
    let mutable unifiedToEquivMap = Map.empty<string, string>

    // 3. 調査対象となるすべてのコードポイント（左側も右側も混ざったハッシュセット）
    // 例: "2F00", "4E00", "FA0E" など
    let mutable allTargetCodes = Set.empty<string>