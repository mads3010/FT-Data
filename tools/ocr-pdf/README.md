# ocr-pdf

Folketinget publishes the party accounts as **scanned** PDFs (no text layer in most years). This tiny macOS tool
OCRs them with Apple's built-in Vision framework (Danish is supported, ~1.5 s per page on Apple silicon, no
installs) and writes `<file>.ocr.txt` next to the PDF. The importer picks the sidecar up automatically:

```bash
swiftc -O -o tools/ocr-pdf/ocr-pdf tools/ocr-pdf/main.swift
for f in data/partiregnskaber/*.pdf; do tools/ocr-pdf/ocr-pdf "$f"; done
dotnet run --project src/FolketingetVotes.Ingest -- import-party-accounts data/partiregnskaber/*.pdf
```

Options: `--no-correction` disables Vision's language model correction (keeps names/amounts raw), `--dpi N`
(default 300), `--out path`.

On Linux/Windows use any OCR that produces the same format (`=== Page N ===` markers, one line per text row),
e.g. `ocrmypdf --sidecar` or Tesseract with `-l dan`.
