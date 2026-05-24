"""Screenshot tool - saves to F: drive with configurable upscaling."""
import sys
from pathlib import Path
from datetime import datetime
from PIL import Image
import mss
import mss.tools

# Save to F drive, not C drive
OUT_DIR = Path("F:/screen_automation/screenshots")
OUT_DIR.mkdir(parents=True, exist_ok=True)


def capture(monitor: int = 0, upscale: int = 1) -> Path:
    """Capture screen. monitor 0=all, 1=primary. upscale multiplies resolution."""
    with mss.MSS() as sct:
        mon = sct.monitors[monitor]
        ts = datetime.now().strftime("%Y%m%d_%H%M%S_%f")[:-3]
        base_path = OUT_DIR / f"scr_{monitor}_{ts}.png"

        img = sct.grab(mon)
        pil_img = Image.frombytes("RGB", img.size, img.rgb)

        if upscale > 1:
            new_size = (pil_img.width * upscale, pil_img.height * upscale)
            pil_img = pil_img.resize(new_size, Image.LANCZOS)
            path = OUT_DIR / f"scr_{monitor}_{ts}_x{upscale}.png"
        else:
            path = base_path

        pil_img.save(str(path), "PNG", optimize=True)
        print(path)
        return path


def capture_region(left: int, top: int, width: int, height: int, upscale: int = 2) -> Path:
    """Capture a specific region with upscaling for better OCR."""
    with mss.MSS() as sct:
        ts = datetime.now().strftime("%Y%m%d_%H%M%S_%f")[:-3]
        region = {"left": left, "top": top, "width": width, "height": height}
        img = sct.grab(region)
        pil_img = Image.frombytes("RGB", img.size, img.rgb)

        if upscale > 1:
            new_size = (pil_img.width * upscale, pil_img.height * upscale)
            pil_img = pil_img.resize(new_size, Image.LANCZOS)

        path = OUT_DIR / f"scr_region_{ts}_x{upscale}.png"
        pil_img.save(str(path), "PNG", optimize=True)
        print(path)
        return path


def list_monitors():
    with mss.MSS() as sct:
        for i, m in enumerate(sct.monitors):
            print(f"[{i}] {m['width']}x{m['height']} left={m['left']} top={m['top']}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python scr.py [capture|list|last|region] [args...]")
        print("  list              - show all monitors")
        print("  capture [mon] [x] - capture monitor mon (default 1), optional upscale factor")
        print("  region L T W H [x]- capture region with upscale (default 2x)")
        print("  last              - print path of most recent screenshot")
        sys.exit(1)

    cmd = sys.argv[1]

    if cmd == "list":
        list_monitors()
    elif cmd == "capture":
        mon = int(sys.argv[2]) if len(sys.argv) > 2 else 1
        upscale = int(sys.argv[3]) if len(sys.argv) > 3 else 1
        capture(mon, upscale)
    elif cmd == "region":
        if len(sys.argv) < 6:
            print("Need: region left top width height [upscale]", file=sys.stderr)
            sys.exit(1)
        left, top, w, h = map(int, sys.argv[2:6])
        upscale = int(sys.argv[6]) if len(sys.argv) > 6 else 2
        capture_region(left, top, w, h, upscale)
    elif cmd == "last":
        files = sorted(OUT_DIR.glob("scr_*.png"), key=lambda p: p.stat().st_mtime, reverse=True)
        if files:
            print(files[0])
        else:
            print("No screenshots found", file=sys.stderr)
            sys.exit(1)
    else:
        print(f"Unknown command: {cmd}", file=sys.stderr)
        sys.exit(1)
