# ToUnicode-for-JIS-X-0213
Testing round-trip fidelity of JIS X 0213 characters through PDF using OOXML and HTML source documents.

# 日本語

## 概要

PDFからUnicodeテキストを作成するときは、PDF文書中のToUnicodeというデータが使われます。このToUnicodeを解析するのがこのプログラムです。特に、復元先のUnicode文字が、康煕部首やCJK部首補助なのか、統合漢字なのかを調べ、分かりやすく表示します。

## 結果例

### CSVファイル

ひとつのPDF文書で使われてるフォントごとに、康煕部首やCJK部首補助を指している行がToUnicode内にいくつあるかを数えます。また競合する統合漢字を指している行も数えます。CJKの筆画、CJK互換漢字についても同じことをやっています。

例 https://github.com/murata2makoto/ToUnicode-for-JIS-X-0213/blob/main/OOXML-PDF-Plain/toUnicode/IPAexGothic%20Acrobat.csv

### txtファイル

ひとつのPDF文書で使われてるフォントごとに、康煕部首やCJK部首補助を指している行を列挙します。また、競合する統合漢字を指している行も列挙します。

例 https://github.com/murata2makoto/ToUnicode-for-JIS-X-0213/blob/main/OOXML-PDF-Plain/toUnicode/IPAexGothic%20Acrobat.txt


## 入力と出力

AnalyzeToUnicode.exeの後にディレクトリ名を指定します。このディレクトリの中にあるPDF文書であって、*-uncompressed.pdfにマッチするものだけを処理します。それらのPDF文書は、mutool clean -dで処理されている（圧縮が解除されている）ことを前提としています。他のツールの出力でも動くかも知れませんが試してはいません。

toUnicodeというディレクトリが、指定したディレクトリの中に作成され、その中にcsvファイルとtxtファイルが格納されます。

指定されたディレクトリの中にtxtファイルがあれば、PDFから抜き出したテキストファイルだとみなして、康煕部首、CJK部首補助、CJKの筆画、CJK互換漢字、競合する統合漢字を数えます。その結果は、textAnalysisというサブディレクトリの中にtxtファイルとして格納されます。


## 実行例
```
C:\Users\eb2m->f:\ToUnicode-for-JIS-X-0213\AnalyzeToUnicode\bin\Release\net8.0\AnalyzeToUnicode.exe F:\ToUnicode-for-JIS-X-0213\HTML-PDF-plain
?? Initializing Unicode Character Database...
Successfully loaded 336 Equivalent Unified Ideograph pairs
Successfully loaded 11992 Compatibility Ideograph pairs
Analyzing CMap: IPAexGothic-uncompressed.pdf
Analyzing CMap: Meiryo-uncompressed.pdf
Analyzing CMap: NotoSerifJP-uncompressed.pdf
Analyzing CMap: SourceHanJP-uncompressed.pdf
Analyzing CMap: YuMincho-uncompressed.pdf
Analyzing text file: IPAexGothic pdfminer.txt
Analyzing text file: IPAexGothic pdftotext.txt
Analyzing text file: IPAexGothic-Chrome pdfminer.txt
Analyzing text file: IPAexGothic-Chrome pdftotext.txt
Analyzing text file: Meiryo pdfminer.txt
Analyzing text file: Meiryo pdftotext.txt
Analyzing text file: Meiryo-Chrome pdfminer.txt
Analyzing text file: Meiryo-Chrome pdftotext.txt
Analyzing text file: NotoSerifJP pdfminer.txt
Analyzing text file: NotoSerifJP pdftotext.txt
Analyzing text file: NotoSerifJP-Chrome pdfminer.txt
Analyzing text file: NotoSerifJP-Chrome pdftotext.txt
Analyzing text file: SourceHanJP pdfminer.txt
Analyzing text file: SourceHanJP pdftotext.txt
Analyzing text file: SourceHanJP-Chrome pdfminer.txt
Analyzing text file: SourceHanJP-Chrome pdftotext.txt
Analyzing text file: YuMincho pdfminer.txt
Analyzing text file: YuMincho pdftotext.txt
Analyzing text file: YuMincho-Chrome pdfminer.txt
Analyzing text file: YuMincho-Chrome pdftotext.txt

? すべての検証フェーズが正常に終了しました！
```
## インストール


## その他

JIS X 0213にあるすべての文字を含むOOXML文書とHTML文書からPDFを作り、さらにplain textを作る。そして、JIS X 0213にあるすべての文字に戻るかを調べる。

HTML-PDF-plainは、HTML文書についてテストしたもの。
OOXML-PDF-Plainは、OOXML文書(WML)についてテストしたもの。

フォントは、游明朝、メイリオ、IPAexGothic, SourceHanJP(源の明朝）、Noto Serif JPについて試した。したがって、OOXML文書(WML)もHTML文書も五つずつある。

OOXMLからのPDF作成はWordとAcrobatで、HTMLからのPDF作成はChromeで行った。

PDFからのプレーンテキストファイル生成はpdfMinerで行った。

もともとのJIS X 0213文字列の差はXXX_diff_from_orig.txtに格納してある。


# English

PDF files were generated from both OOXML and HTML documents containing all characters defined in JIS X 0213. Plain text files were then extracted from those PDFs to verify whether every character could be round-tripped back to the original JIS X 0213 character set.

The directory **HTML-PDF-Plain** contains the test results for HTML documents.

The directory **OOXML-PDF-Plain** contains the test results for OOXML (WordprocessingML) documents.

Five fonts were tested: **Yu Mincho**, **Meiryo**, **IPAex Gothic**, **Source Han Serif JP**, and **Noto Serif JP**. Accordingly, there are five OOXML documents and five HTML documents.

PDF generation from OOXML documents was performed using **Microsoft Word** and **Adobe Acrobat**. PDF generation from HTML documents was performed using **Google Chrome**.

Plain text extraction from PDF files was performed using **pdfminer.six**.

Differences between the extracted text and the original JIS X 0213 character sequence are stored in files named **XXX_diff_from_orig.txt**.

The **ToUnicode** CMaps used for text extraction from PDF files have also been extracted from the PDFs and stored in this repository.

