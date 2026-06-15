namespace AnalyzeToUnicode

open System

type UnicodeCategory =
    | KangxiRadical
    | RadicalSupplement
    | CjkStroke
    | Compatibility
    | KanjiUnifiedForKangxiRadical
    | KanjiUnifiedForRadicalSupplement
    | KanjiUnifiedForCjkStroke
    | KanjiUnifiedForCompatibility
    | Other

type FontStats = {
    FontId : string
    FontName : string
    ToUnicodeId : string
    mutable KangxiRadicalCount           : int
    mutable RadicalSupplementCount       : int
    mutable CjkStrokeCount               : int
    mutable CompatibilityCount           : int
    mutable UnifiedForKangxiCount        : int
    mutable UnifiedForSupplementCount    : int
    mutable UnifiedForStrokeCount        : int
    mutable UnifiedForCompatibilityCount : int
    mutable OtherCount                   : int
}