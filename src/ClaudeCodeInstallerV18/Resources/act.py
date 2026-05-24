"""Action tool - performs mouse and keyboard operations."""
import sys
import time
import pyautogui

pyautogui.FAILSAFE = True  # Move to corner (0,0) to abort
pyautogui.PAUSE = 0.05     # Small pause between actions for safety


def do_move(x: int, y: int, duration: float = 0.3):
    """Move mouse to (x, y)."""
    pyautogui.moveTo(x, y, duration=duration)
    print(f"Moved to ({x}, {y})")


def do_click(x: int = None, y: int = None, button: str = "left", clicks: int = 1):
    """Click at current position or (x, y)."""
    if x is not None and y is not None:
        pyautogui.moveTo(x, y, duration=0.2)
    pyautogui.click(clicks=clicks, button=button)
    pos = pyautogui.position()
    print(f"Clicked {button} at {pos}")


def do_dblclick(x: int = None, y: int = None):
    """Double-click at current position or (x, y)."""
    if x is not None and y is not None:
        pyautogui.moveTo(x, y, duration=0.2)
    pyautogui.doubleClick()
    pos = pyautogui.position()
    print(f"Double-clicked at {pos}")


def do_rightclick(x: int = None, y: int = None):
    """Right-click at current position or (x, y)."""
    if x is not None and y is not None:
        pyautogui.moveTo(x, y, duration=0.2)
    pyautogui.rightClick()
    pos = pyautogui.position()
    print(f"Right-clicked at {pos}")


def do_drag(x1: int, y1: int, x2: int, y2: int, duration: float = 0.5):
    """Drag from (x1, y1) to (x2, y2)."""
    pyautogui.moveTo(x1, y1, duration=0.2)
    pyautogui.drag(x2 - x1, y2 - y1, duration=duration)
    print(f"Dragged ({x1},{y1}) -> ({x2},{y2})")


def do_type(text: str, interval: float = 0.02):
    """Type the given text."""
    pyautogui.typewrite(text, interval=interval)
    print(f"Typed: {text}")


def do_press(*keys: str, times: int = 1, interval: float = 0.1):
    """Press key combination(s), e.g. ctrl+c."""
    for _ in range(times):
        pyautogui.hotkey(*keys, interval=interval)
    print(f"Pressed: {'+'.join(keys)}")


def do_scroll(amount: int, x: int = None, y: int = None):
    """Scroll. Positive=up, negative=down."""
    if x is not None and y is not None:
        pyautogui.moveTo(x, y, duration=0.1)
    pyautogui.scroll(amount)
    print(f"Scrolled {amount}")


def do_position():
    """Print current mouse position."""
    x, y = pyautogui.position()
    print(f"Current position: ({x}, {y})")


def do_sleep(seconds: float):
    """Sleep for given seconds."""
    time.sleep(seconds)
    print(f"Slept {seconds}s")


def do_screenshot(region: str = None):
    """Take a screenshot and print the path. Region = x,y,w,h."""
    from pathlib import Path
    from datetime import datetime
    out_dir = Path("F:/screen_automation/screenshots")
    out_dir.mkdir(parents=True, exist_ok=True)
    ts = datetime.now().strftime("%Y%m%d_%H%M%S_%f")[:-3]
    path = out_dir / f"scr_region_{ts}.png"

    if region:
        parts = [int(p.strip()) for p in region.split(",")]
        if len(parts) == 4:
            img = pyautogui.screenshot(region=(parts[0], parts[1], parts[2], parts[3]))
            img.save(str(path))
        else:
            print("Region must be: x,y,width,height", file=sys.stderr)
            sys.exit(1)
    else:
        pyautogui.screenshot(str(path))
    print(path)


COMMANDS = {
    "move": (do_move, "x y [duration=0.3]"),
    "click": (do_click, "[x y] [button=left]"),
    "dblclick": (do_dblclick, "[x y]"),
    "rightclick": (do_rightclick, "[x y]"),
    "drag": (do_drag, "x1 y1 x2 y2 [duration=0.5]"),
    "type": (do_type, "text"),
    "press": (do_press, "key1 [key2 key3...]"),
    "scroll": (do_scroll, "amount [x y]"),
    "pos": (do_position, ""),
    "sleep": (do_sleep, "seconds"),
    "shot": (do_screenshot, "[x,y,w,h]"),
}


def main():
    if len(sys.argv) < 2:
        print("Usage: python act.py <command> [args...]")
        print()
        for name, (fn, args) in COMMANDS.items():
            print(f"  {name:<12} {args}")
        sys.exit(1)

    cmd = sys.argv[1]
    args = sys.argv[2:]

    if cmd not in COMMANDS:
        print(f"Unknown command: {cmd}", file=sys.stderr)
        print(f"Available: {', '.join(COMMANDS)}", file=sys.stderr)
        sys.exit(1)

    fn, _ = COMMANDS[cmd]
    try:
        # Parse numeric args
        parsed = []
        for a in args:
            try:
                parsed.append(float(a) if "." in a else int(a))
            except ValueError:
                parsed.append(a)
        fn(*parsed)
    except TypeError as e:
        print(f"Wrong arguments for '{cmd}': {e}", file=sys.stderr)
        sys.exit(1)
    except pyautogui.FailSafeException:
        print("FAILSAFE TRIGGERED: mouse moved to corner (0,0)", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
