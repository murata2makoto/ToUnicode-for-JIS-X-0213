from pdfminer.pdfpage import PDFPage
from pdfminer.pdfinterp import PDFResourceManager, PDFPageInterpreter
from pdfminer.converter import PDFLayoutAnalyzer
from pdfminer.layout import LTTextBox, LTChar

class CMapExtractor(PDFLayoutAnalyzer):
    def __init__(self, rsrcmgr):
        super().__init__(rsrcmgr)
        self.seen_fonts = set()

    def render_char(self, matrix, font, fontsize, scaling, rise, cid, nbytes, fontname):
        # まだ調査していないフォントを見つけたら
        if font.fontname not in self.seen_fonts:
            self.seen_fonts.add(font.fontname)
            print(f"\n================ Font: {font.fontname} ================")
            
            # ToUnicode CMap が存在するか確認
            if hasattr(font, 'is_vertical') and hasattr(font, 'cid2unicode'):
                print("[ToUnicode Mapping Data (CID -> Unicode)]")
                # pdfminerが内部で既に解釈したマッピングテーブル(dict)を表示
                for cid_code, unicode_char in sorted(font.cid2unicode.items()):
                    print(f"  CID: {cid_code:<5} -> Unicode: U+{ord(unicode_char):04X} ({unicode_char})")
            else:
                print("  ToUnicode CMap is not embedded or available for this font.")
        return 0

def extract_to_unicode(pdf_path):
    rsrcmgr = PDFResourceManager()
    device = CMapExtractor(rsrcmgr)
    interpreter = PDFPageInterpreter(rsrcmgr, device)
    
    with open(pdf_path, 'rb') as fp:
        for page in PDFPage.get_pages(fp):
            interpreter.process_page(page)

# 実行
extract_to_unicode('IPAexGothic.pdf')
extract_to_unicode('Meiryo.pdf')
extract_to_unicode('NotoSerifJP.pdf')
extract_to_unicode('SourceHanJP.pdf')
extract_to_unicode('YuMincho.pdf')
