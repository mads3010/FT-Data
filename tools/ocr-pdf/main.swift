// ocr-pdf: OCR a scanned PDF with Apple's Vision framework (macOS 13+; Danish supported) and write a
// text sidecar the importer understands: one "=== Page N ===" marker per page followed by the page's lines.
//
//   swiftc -O -o ocr-pdf main.swift
//   ./ocr-pdf "Partiregnskaber2023.pdf"            # writes Partiregnskaber2023.ocr.txt next to the PDF
//   ./ocr-pdf --no-correction --dpi 300 file.pdf   # options: --no-correction (keep raw names), --dpi N, --out path, --from N (resume)
//
// Lines are reconstructed from Vision's text blocks by grouping blocks with the same vertical position and
// ordering them left to right, with two spaces between blocks so table columns stay separable.
import Foundation
import PDFKit
import Vision
import AppKit

struct Options {
    var input = ""
    var output: String?
    var dpi: CGFloat = 300
    var languageCorrection = true
    var fromPage = 1
}

func parse(_ args: [String]) -> Options {
    var o = Options()
    var i = 1
    while i < args.count {
        switch args[i] {
        case "--no-correction": o.languageCorrection = false
        case "--dpi": i += 1; o.dpi = CGFloat(Double(args[i]) ?? 300)
        case "--out": i += 1; o.output = args[i]
        case "--from": i += 1; o.fromPage = Int(args[i]) ?? 1
        default: o.input = args[i]
        }
        i += 1
    }
    return o
}

func render(_ page: PDFPage, dpi: CGFloat) -> CGImage? {
    let bounds = page.bounds(for: .mediaBox)
    let scale = dpi / 72.0
    let size = NSSize(width: bounds.width * scale, height: bounds.height * scale)
    let image = NSImage(size: size)
    image.lockFocus()
    NSColor.white.setFill()
    NSRect(origin: .zero, size: size).fill()
    if let ctx = NSGraphicsContext.current?.cgContext {
        ctx.scaleBy(x: scale, y: scale)
        page.draw(with: .mediaBox, to: ctx)
    }
    image.unlockFocus()
    return image.cgImage(forProposedRect: nil, context: nil, hints: nil)
}

func recognise(_ image: CGImage, correction: Bool) throws -> [String] {
    let request = VNRecognizeTextRequest()
    request.recognitionLevel = .accurate
    request.recognitionLanguages = ["da-DK"]
    request.usesLanguageCorrection = correction
    try VNImageRequestHandler(cgImage: image, options: [:]).perform([request])
    let blocks = (request.results ?? []).sorted { $0.boundingBox.midY > $1.boundingBox.midY }
    var rows: [[VNRecognizedTextObservation]] = []
    for block in blocks {
        if let anchor = rows.last?.first, abs(anchor.boundingBox.midY - block.boundingBox.midY) < 0.008 {
            rows[rows.count - 1].append(block)
        } else {
            rows.append([block])
        }
    }
    return rows.map { row in
        row.sorted { $0.boundingBox.minX < $1.boundingBox.minX }
            .compactMap { $0.topCandidates(1).first?.string }
            .joined(separator: "  ")
    }
}

let options = parse(CommandLine.arguments)
guard !options.input.isEmpty, let document = PDFDocument(url: URL(fileURLWithPath: options.input)) else {
    FileHandle.standardError.write("usage: ocr-pdf [--no-correction] [--dpi N] [--out file] input.pdf\n".data(using: .utf8)!)
    exit(2)
}
let outputPath = options.output ?? options.input.replacingOccurrences(of: ".pdf", with: "", options: [.caseInsensitive, .anchored, .backwards]) + ".ocr.txt"
// Output is appended page by page so a crash or interruption keeps what was done; --from N resumes.
if options.fromPage <= 1 || !FileManager.default.fileExists(atPath: outputPath) {
    FileManager.default.createFile(atPath: outputPath, contents: nil)
}
guard let handle = FileHandle(forWritingAtPath: outputPath) else { print("cannot write \(outputPath)"); exit(1) }
handle.seekToEndOfFile()
let started = Date()
for index in max(0, options.fromPage - 1)..<document.pageCount {
    // Each page allocates tens of MB of image and Vision objects; without a pool they accumulate until the process is killed.
    autoreleasepool {
        var text = "=== Page \(index + 1) ===\n"
        if let page = document.page(at: index), let image = render(page, dpi: options.dpi) {
            do {
                text += try recognise(image, correction: options.languageCorrection).joined(separator: "\n") + "\n"
            } catch {
                FileHandle.standardError.write("page \(index + 1): \(error)\n".data(using: .utf8)!)
            }
        }
        handle.write(text.data(using: .utf8)!)
    }
    if (index + 1) % 20 == 0 || index + 1 == document.pageCount {
        FileHandle.standardError.write("\(index + 1)/\(document.pageCount) pages, \(Int(Date().timeIntervalSince(started))) s\n".data(using: .utf8)!)
    }
}
handle.closeFile()
print("wrote \(outputPath): \(document.pageCount) pages in \(Int(Date().timeIntervalSince(started))) s")
