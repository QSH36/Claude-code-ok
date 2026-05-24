"""See tool: screenshot + OCR + click. One-stop screen interaction.
Screenshots saved to F: drive. Auto-focuses Claude Code after each operation.
"""
import sys

# Force UTF-8 output
if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

import json
import time
from pathlib import Path

import pygetwindow as gw

from scr import capture as scr_capture, capture_region
from ocr import ocr, find_text, click_target
from act import do_click, do_dblclick, do_move, do_scroll


def focus_claude():
    """Bring Claude Code / terminal window back to the foreground aggressively."""
    time.sleep(0.1)

    # Priority 1: exact patterns that match the Claude Code terminal
    primary_patterns = [
        "request remote computer control",
        "claude",
        "mintty",
        "bash",
        "管理员",
    ]

    # Priority 2: broader terminal patterns
    secondary_patterns = [
        "terminal",
        "powershell",
        "cmd",
        "windows powershell",
        "命令提示符",
        "console",
    ]

    all_windows = gw.getAllWindows()

    # Try primary patterns first
    for pattern in primary_patterns:
        for w in all_windows:
            if w.width < 200 or not w.title.strip():
                continue
            if pattern in w.title.lower():
                try:
                    if w.isMinimized:
                        w.restore()
                    w.activate()
                    time.sleep(0.05)
                    return True
                except Exception:
                    continue

    # Try secondary patterns
    candidates = []
    for w in all_windows:
        title = w.title.strip()
        if not title or w.width < 200:
            continue
        tl = title.lower()
        if any(k in tl for k in secondary_patterns):
            candidates.append((w.width, w))

    if candidates:
        best = max(candidates, key=lambda x: x[0])[1]
        try:
            if best.isMinimized:
                best.restore()
            best.activate()
            return True
        except Exception:
            pass

    # Last resort: find window with "request" or "remote" in title
    for w in all_windows:
        tl = w.title.lower().strip()
        if w.width > 400 and ("request" in tl or "remote" in tl or "claude" in tl):
            try:
                w.activate()
                return True
            except Exception:
                pass

    return False


def see_and_read(monitor: int = 1, lang: str = "chi_sim+eng", upscale: int = 2):
    """Take a screenshot (upscaled for better OCR) and run OCR."""
    path = scr_capture(monitor, upscale=upscale)
    result = ocr(str(path), lang)
    return path, result


def main():
    if len(sys.argv) < 2:
        print("Usage: python see.py <command> [args...]")
        print()
        print("Commands:")
        print("  read [--mon N]          Screenshot + OCR, print all visible text")
        print("  find TEXT [--mon N]     Screenshot + find text positions")
        print("  click TEXT [--mon N]    Screenshot + find text + click it")
        print("  dblclick TEXT [--mon N] Screenshot + find text + double-click")
        print("  move TEXT [--mon N]     Screenshot + find text + move mouse there")
        print("  scroll AMT [TEXT]       Scroll; if TEXT given, move to it first")
        print("  shot                    Just take screenshot (upscaled), print path")
        print("  focus                   Bring Claude Code window to front")
        print()
        print("Options:")
        print("  --mon N    Use monitor N (default 1=primary)")
        print("  --lang L   OCR languages (default: chi_sim+eng)")
        print("  --x1       No upscaling (1x, faster but less accurate)")
        print("  --x2       2x upscaling (default, better for small text)")
        print("  --x4       4x upscaling (best for tiny text)")
        sys.exit(1)

    cmd = sys.argv[1]
    args = sys.argv[2:]

    monitor = 1
    lang = "chi_sim+eng"
    upscale = 2

    # Parse options
    filtered = []
    i = 0
    while i < len(args):
        if args[i] == "--mon" and i + 1 < len(args):
            monitor = int(args[i + 1])
            i += 2
        elif args[i] == "--lang" and i + 1 < len(args):
            lang = args[i + 1]
            i += 2
        elif args[i] == "--x1":
            upscale = 1
            i += 1
        elif args[i] == "--x2":
            upscale = 2
            i += 1
        elif args[i] == "--x4":
            upscale = 4
            i += 1
        else:
            filtered.append(args[i])
            i += 1

    try:
        if cmd == "focus":
            ok = focus_claude()
            print("Focused Claude Code" if ok else "Could not find Claude Code window")

        elif cmd == "shot":
            path = scr_capture(monitor, upscale=upscale)
            print(path)

        elif cmd == "read":
            path, result = see_and_read(monitor, lang, upscale)
            print(f"=== Screenshot: {path} ===\n")
            print(result["text"])

        elif cmd in ("find", "click", "dblclick", "move", "scroll"):
            if not filtered and cmd in ("find", "click", "dblclick", "move"):
                print(f"Error: '{cmd}' requires a text query", file=sys.stderr)
                sys.exit(1)

            query = filtered[0] if filtered else ""

            path, result = see_and_read(monitor, lang, upscale)
            if "error" in result:
                print(result["error"], file=sys.stderr)
                sys.exit(1)

            target = click_target(result["words"], query)

            if cmd == "find":
                print(f"=== Searching for '{query}' ===")
                print(f"Screenshot: {path}\n")
                for m in target.get("all_matches", []):
                    cx = (m["x"] + m["w"] // 2) // 2
                    cy = (m["y"] + m["h"] // 2) // 2
                    print(f"  [{m['conf']}%] '{m['text']}' -> click at ({cx}, {cy})")
                if not target.get("found"):
                    print(f"No match found for '{query}'")
                return

            if not target.get("found"):
                print(f"COULD NOT FIND '{query}' on screen", file=sys.stderr)
                print(f"Screenshot saved: {path}")
                print("Visible text preview:")
                print(result["text"][:500])
                sys.exit(1)

            x, y = target["x"], target["y"]
            print(f"Found '{target['text']}' at ({x}, {y}) [conf={target['conf']}%]")

            if cmd == "click":
                do_click(x, y)
            elif cmd == "dblclick":
                do_dblclick(x, y)
            elif cmd == "move":
                do_move(x, y, duration=0.3)
            elif cmd == "scroll":
                amount = int(query) if query else int(filtered[0])
                do_scroll(amount, x, y)

        elif cmd == "json":
            _, result = see_and_read(monitor, lang, upscale)
            out = {
                "text": result["text"][:8000],
                "words_count": result["word_count"],
                "words": result["words"][:500],
            }
            print(json.dumps(out, ensure_ascii=False, indent=2))

        elif cmd == "region":
            if len(filtered) < 4:
                print("Need: region left top width height", file=sys.stderr)
                sys.exit(1)
            left, top, w, h = map(int, filtered[:4])
            path = capture_region(left, top, w, h, upscale=upscale)
            result = ocr(str(path), lang)
            print(f"=== Region ({left},{top}) {w}x{h} | {path} ===\n")
            print(result["text"])

        else:
            print(f"Unknown command: {cmd}", file=sys.stderr)
            sys.exit(1)

    finally:
        # Always bring Claude Code back to front after operations
        focus_claude()


if __name__ == "__main__":
    main()
