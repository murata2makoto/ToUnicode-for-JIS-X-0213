module AnalyzeToUnicode.CMapParser

open System
open System.IO
open System.Text.RegularExpressions
open FParsec

// --- 補助パーサー ---
let pObjRef : Parser<ObjRef, unit> =
    tuple2 
        (pint32 .>> spaces) 
        (pint32 .>> spaces .>> pstring "R")
    |>> fun (id, gen) -> { Id = id; Gen = gen }

let pHexToken : Parser<string, unit> =
    pstring "<" >>. many1Satisfy isHex .>> pstring ">"

// --- CMap（ToUnicode中身）のパーサー ---
let pBfCharLine : Parser<CMapLine, unit> =
    tuple2 
        (pHexToken .>> spaces) 
        (pHexToken .>> spaces)
    |>> fun (cid, uni) -> BfChar(cid.ToUpper(), uni.ToUpper())

let pBfRangeLineList : Parser<CMapLine, unit> =
    tuple3 
        (pHexToken .>> spaces) 
        (pHexToken .>> spaces) 
        (pstring "[" 
            >>. spaces 
            >>. many1 (pHexToken .>> spaces) 
            .>> pstring "]")
    |>> fun (sCid, eCid, unis) ->
                let uniList = unis |> List.map (fun u -> u.ToUpper())
                BfRangeList(sCid.ToUpper(), eCid.ToUpper(), uniList)

let pBfRangeLineCalc : Parser<CMapLine, unit> =
    tuple3 
        (pHexToken .>> spaces) 
        (pHexToken .>> spaces) 
        (pHexToken .>> spaces)
    |>> fun (sCid, eCid, sUni) -> 
                BfRangeCalculated(sCid.ToUpper(), eCid.ToUpper(), sUni.ToUpper())

let pValidCMapLine : Parser<CMapLine, unit> =
    attempt pBfCharLine 
    <|> attempt pBfRangeLineList 
    <|> attempt pBfRangeLineCalc

let pCMapContent : Parser<CMapLine list, unit> =
    parse {
        do! (fun stream -> Reply(()))
        let! rest = manyChars anyChar
        let lines = 
            rest.Split([| '\n'; '\r' |], 
                        StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun l -> l.Trim())
            |> Array.choose (fun trimmedLine ->
                if String.IsNullOrWhiteSpace(trimmedLine) then None
                else
                    match run pValidCMapLine trimmedLine with
                    | Success(result, _, _) -> Some result
                    | Failure _ -> None
            )
            |> Array.toList
        return lines
    }

// --- PDFオブジェクト全体のパーサー ---
let pPdfObject : Parser<PdfObject, unit> =
    parse {
        let! objId = 
            pint32 .>> spaces 
            .>> pint32 .>> spaces 
            .>> pstring "obj" .>> spaces
        let! content = 
            manyCharsTill anyChar (pstring "endobj")
        
        if content.Contains("/Type /Font") 
            && not(content.Contains("/Type /FontDescriptor")) then
            let mName = 
                Regex.Match(content, @"/BaseFont\s+/([A-Za-z0-9\+\-]+)")
            let fontName = 
                if mName.Success then mName.Groups.[1].Value else "Unknown"
            let mToUni = 
                Regex.Match(content, @"/ToUnicode\s+(\d+)\s+(\d+)\s+R")
            let refOpt = 
                if mToUni.Success then 
                    Some { Id = int mToUni.Groups.[1].Value; 
                            Gen = int mToUni.Groups.[2].Value } 
                else None
            return FontObj(objId, fontName, refOpt)
            
        elif content.Contains("begincmap") then
            let m = 
                Regex.Match(content, @"stream\s*(.*?)\s*endstream", 
                            RegexOptions.Singleline)
            if m.Success then
                match run pCMapContent m.Groups.[1].Value with
                | Success(lines, _, _) -> 
                    return CMapStreamObj(objId, lines)
                | Failure _ -> 
                    return OtherObj
            else return OtherObj
        else 
            return OtherObj
    }

let parsePdfTextFile (filePath: string) =
    let fileContent = File.ReadAllText(filePath)
    let objBlocks = 
        Regex.Matches(fileContent, @"\d+\s+\d+\s+obj.*?endobj", 
                        RegexOptions.Singleline)
    
    let mutable fonts = []
    let mutable cmaps = Map.empty
    
    for m in objBlocks do
        match run pPdfObject m.Value with
        | Success(FontObj(id, name, toUnicode), _, _) -> 
            fonts <- (id, name, toUnicode) :: fonts
        | Success(CMapStreamObj(id, lines), _, _) -> 
            cmaps <- cmaps.Add(id, lines)
        | _ -> ()
        
    (fonts, cmaps)