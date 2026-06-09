# ToUnicode-for-JIS-X-0213
Testing round-trip fidelity of JIS X 0213 characters through PDF using OOXML and HTML source documents.

# 日本語

JIS X 0213にあるすべての文字を含むOOXML文書とHTML文書からPDFを作り、さらにplain textを作る。そして、JIS X 0213にあるすべての文字に戻るかを調べる。

HTML-PDF-plainは、HTML文書についてテストしたもの。
OOXML-PDF-Plainは、OOXML文書(WML)についてテストしたもの。

フォントは、游明朝、メイリオ、IPAexGothic, SourceHanJP(源の明朝）、Noto Serif JPについて試した。したがって、OOXML文書(WML)もHTML文書も五つずつある。

OOXMLからのPDF作成はWordとAcrobatで、HTMLからのPDF作成はChromeで行った。

PDFからのプレーンテキストファイル生成はpdfMinerで行った。

もともとのJIS X 0213文字列の差はXXX_diff_from_orig.txtに格納してある。

PDFからプレーンテキストを作成するのに使われるるToUnicodeはPDFから抜き出して格納してある。

# English

PDF files were generated from both OOXML and HTML documents containing all characters defined in JIS X 0213. Plain text files were then extracted from those PDFs to verify whether every character could be round-tripped back to the original JIS X 0213 character set.

The directory **HTML-PDF-Plain** contains the test results for HTML documents.

The directory **OOXML-PDF-Plain** contains the test results for OOXML (WordprocessingML) documents.

Five fonts were tested: **Yu Mincho**, **Meiryo**, **IPAex Gothic**, **Source Han Serif JP**, and **Noto Serif JP**. Accordingly, there are five OOXML documents and five HTML documents.

PDF generation from OOXML documents was performed using **Microsoft Word** and **Adobe Acrobat**. PDF generation from HTML documents was performed using **Google Chrome**.

Plain text extraction from PDF files was performed using **pdfminer.six**.

Differences between the extracted text and the original JIS X 0213 character sequence are stored in files named **XXX_diff_from_orig.txt**.

The **ToUnicode** CMaps used for text extraction from PDF files have also been extracted from the PDFs and stored in this repository.

