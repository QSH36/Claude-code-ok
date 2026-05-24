"""OCR tool - enhanced with image preprocessing for better accuracy."""
import sys
import subprocess
import json
from pathlib import Path
from datetime import datetime

# Force UTF-8 output on Windows
if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

from PIL import Image, ImageEnhance, ImageFilter

TESSERACT = r"C:\Program Files\Tesseract-OCR\tesseract.exe"
SCREENSHOT_DIR = Path("F:/screen_automation/screenshots")
SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)


def preprocess_for_ocr(image_path: str, upscale: int = 2) -> str:
    """Enhance image for better OCR: upscale, sharpen, increase contrast.
    Returns path to the processed image."""
    img = Image.open(image_path)

    # Upscale for better small-text recognition
    if upscale > 1:
        new_size = (img.width * upscale, img.height * upscale)
        img = img.resize(new_size, Image.LANCZOS)

    # Convert to grayscale for better OCR
    if img.mode != "L":
        img = img.convert("L")

    # Enhance contrast
    enhancer = ImageEnhance.Contrast(img)
    img = enhancer.enhance(2.0)

    # Enhance sharpness
    enhancer = ImageEnhance.Sharpness(img)
    img = enhancer.enhance(2.0)

    # Save processed image
    processed_path = SCREENSHOT_DIR / f"ocr_processed_{datetime.now().strftime('%Y%m%d_%H%M%S_%f')[:-3]}.png"
    img.save(str(processed_path), "PNG")

    return str(processed_path)


def ocr(image_path: str, lang: str = "chi_sim+eng", preprocess: bool = True) -> dict:
    """Run OCR on an image. Returns dict with text and word positions."""
    img = Path(image_path)
    if not img.exists():
        return {"error": f"Image not found: {image_path}"}

    # Preprocess image for better OCR
    ocr_input = preprocess_for_ocr(str(img)) if preprocess else str(img)

    creationflags = subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0

    # Text only
    result = subprocess.run(
        [TESSERACT, ocr_input, "stdout", "-l", lang, "--psm", "3"],
        capture_output=True, encoding="utf-8", errors="replace",
        timeout=120, creationflags=creationflags
    )
    text = (result.stdout or "").strip()
    stderr = (result.stderr or "").strip()

    # TSV for word positions (use processed image)
    tsv_result = subprocess.run(
        [TESSERACT, ocr_input, "stdout", "-l", lang, "--psm", "3", "tsv"],
        capture_output=True, encoding="utf-8", errors="replace",
        timeout=120, creationflags=creationflags
    )

    # Parse TSV
    words = []
    lines = (tsv_result.stdout or "").strip().split("\n")
    if len(lines) > 1:
        for line in lines[1:]:
            cols = line.split("\t")
            if len(cols) >= 12:
                try:
                    conf = int(float(cols[10]))
                    level = int(cols[0])
                    word_text = cols[11].strip()
                    # level 5 = word level
                    if level == 5 and word_text:
                        words.append({
                            "text": word_text,
                            "conf": conf,
                            "x": int(cols[6]),
                            "y": int(cols[7]),
                            "w": int(cols[8]),
                            "h": int(cols[9]),
                        })
                except (ValueError, IndexError):
                    continue

    return {
        "text": text,
        "words": words,
        "word_count": len(words),
        "stderr": stderr,
        "processed_image": ocr_input if preprocess else image_path,
    }


def find_text(words: list, query: str) -> list:
    """Find text in OCR words. Returns list of matching word positions."""
    results = []
    query_lower = query.lower()
    for w in words:
        if query_lower == w["text"].lower():
            results.append(w)
    if not results:
        for w in words:
            if query_lower in w["text"].lower():
                results.append(w)
    return results


def click_target(words: list, query: str) -> dict:
    """Find best matching text and return its center coordinates for clicking.
    Note: coordinates need to be divided by upscale factor if preprocessing was used.
    """
    results = find_text(words, query)
    if not results:
        return {"found": False, "query": query}

    best = max(results, key=lambda w: w["conf"])
    # Coordinates are from the upscaled image, divide by 2 to get screen coords
    center_x = (best["x"] + best["w"] // 2) // 2
    center_y = (best["y"] + best["h"] // 2) // 2
    return {
        "found": True,
        "query": query,
        "text": best["text"],
        "x": center_x,
        "y": center_y,
        "conf": best["conf"],
        "all_matches": results,
    }


def main():
    if len(sys.argv) < 2:
        print("Usage: python ocr.py <image_path> [options]")
        print()
        print("Options:")
        print("  --find TEXT     Search for text in OCR results")
        print("  --click TEXT    Find text and output click coords (JSON)")
        print("  --json          Output full results as JSON")
        print("  --lang LANG     Languages (default: chi_sim+eng)")
        print("  --no-preprocess Skip image preprocessing")
        sys.exit(1)

    image_path = sys.argv[1]
    args = sys.argv[2:]

    lang = "chi_sim+eng"
    mode = "text"
    query = None
    do_preprocess = True

    i = 0
    while i < len(args):
        if args[i] == "--lang" and i + 1 < len(args):
            lang = args[i + 1]
            i += 2
        elif args[i] == "--find" and i + 1 < len(args):
            mode = "find"
            query = args[i + 1]
            i += 2
        elif args[i] == "--click" and i + 1 < len(args):
            mode = "click"
            query = args[i + 1]
            i += 2
        elif args[i] == "--json":
            mode = "json"
            i += 1
        elif args[i] == "--no-preprocess":
            do_preprocess = False
            i += 1
        else:
            i += 1

    result = ocr(image_path, lang, preprocess=do_preprocess)

    if "error" in result:
        print(result["error"], file=sys.stderr)
        sys.exit(1)

    if result.get("stderr"):
        pass  # Tesseract logs like "Estimating resolution" go to stderr

    if mode == "text":
        print(result["text"])
    elif mode == "find":
        matches = find_text(result["words"], query)
        for m in matches:
            cx = (m["x"] + m["w"] // 2) // 2
            cy = (m["y"] + m["h"] // 2) // 2
            print(f"[{m['conf']}%] '{m['text']}' -> click at ({cx}, {cy})")
        if not matches:
            print(f"No matches found for '{query}'")
    elif mode == "click":
        target = click_target(result["words"], query)
        print(json.dumps(target, ensure_ascii=False, indent=2))
    elif mode == "json":
        out = {
            "text": result["text"][:5000],
            "words_count": result["word_count"],
            "words": result["words"][:500],
        }
        print(json.dumps(out, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
