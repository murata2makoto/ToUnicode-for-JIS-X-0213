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
    "chrome-bizud-all.pdf",
    "chrome-bizud-limit.pdf",
    "chrome-bizud-all-MM.pdf",
    "chrome-bizud-limit-MM.pdf",
    "chrome-line-all.pdf",
    "chrome-line-limit.pdf",
    "chrome-line-all-MM.pdf",
    "chrome-line-limit-MM.pdf",
    "chrome-mplus-all.pdf",
    "chrome-mplus-limit.pdf",
    "chrome-mplus-all-MM.pdf",
    "chrome-mplus-limit-MM.pdf",
    "chrome-noto-all.pdf",
    "chrome-noto-limit.pdf",
    "chrome-noto-all-MM.pdf",
    "chrome-noto-limit-MM.pdf",
    "firefox-bizud-all.pdf",
    "firefox-bizud-limit.pdf",
    "firefox-line-all.pdf",
    "firefox-line-limit.pdf",
    "firefox-mplus-all.pdf",
    "firefox-mplus-limit.pdf",
    "firefox-noto-all.pdf",
    "firefox-noto-limit.pdf"

]:
    txt = normalize(extract_text(pdf))

    out = pdf.replace(".pdf", " pdfminer.txt")

    with open(out, "w", encoding="utf-8") as f:
        f.write(txt)
    
