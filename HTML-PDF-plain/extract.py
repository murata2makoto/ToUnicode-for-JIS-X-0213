from pdfminer.high_level import extract_text
import re

def normalize(text):
    text = text.replace('\x00', '')
    text = re.sub(r'All characters in JIS X 0213:2004', ' ', text)
    text = re.sub(r'\r\n|\r|\n', ' ', text)
    text = re.sub(r'[^\S\u00a0]+', '', text)
    text = re.sub(r'(\d+-\d+)', r'\n\1', text)
    return text.lstrip()

for pdf in [
    "IPAexGothic.pdf",
    "IPAexGothic-Chrome.pdf",
    "Meiryo.pdf",
    "Meiryo-Chrome.pdf",
    "NotoSerifJP.pdf",
    "NotoSerifJP-Chrome.pdf",
    "SourceHanJP.pdf",
    "SourceHanJP-Chrome.pdf",
    "YuMincho.pdf",
    "YuMincho-Chrome.pdf"
]:
    txt = normalize(extract_text(pdf))

    out = pdf.replace(".pdf", " pdfminer.txt")

    with open(out, "w", encoding="utf-8") as f:
        f.write(txt)
    
