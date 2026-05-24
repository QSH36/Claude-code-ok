"""Browser automation - fast, reliable web control via Playwright.
Each call opens a browser, executes ALL given actions in sequence, then closes.
Supports both individual commands and batch JSON execution.
"""
import sys
import json
import time
from pathlib import Path
from playwright.sync_api import sync_playwright, TimeoutError as PwTimeout

OUT_DIR = Path("F:/screen_automation/screenshots")
OUT_DIR.mkdir(parents=True, exist_ok=True)


def run_actions(actions: list, headless: bool = False):
    """Execute a list of actions in a single browser session. Returns results."""
    results = []
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=headless)
        page = browser.new_page()
        page.set_default_timeout(8000)

        try:
            for i, action in enumerate(actions):
                cmd = action.get("cmd", action[0] if isinstance(action, list) else "")
                args = action.get("args", action[1:] if isinstance(action, list) else [])
                if isinstance(args, str):
                    args = [args]

                try:
                    r = _exec(page, cmd, args)
                    results.append({"ok": True, "cmd": cmd, "result": r})
                except Exception as e:
                    results.append({"ok": False, "cmd": cmd, "error": str(e)[:200]})
                    # Continue despite errors on non-critical commands
        finally:
            browser.close()

    return results


def _exec(page, cmd: str, args: list):
    """Execute a single command on the page."""
    if cmd == "goto":
        url = args[0] if args else "about:blank"
        page.goto(url, wait_until="domcontentloaded", timeout=15000)
        return {"title": page.title(), "url": page.url}

    elif cmd == "text":
        sel = args[0] if args else "body"
        return page.locator(sel).first.inner_text()

    elif cmd == "click":
        text = args[0]
        el = page.get_by_text(text).first
        el.click(timeout=4000)
        try:
            page.wait_for_load_state("networkidle", timeout=3000)
        except Exception:
            pass
        return f"clicked '{text}'"

    elif cmd == "click-sel":
        sel = args[0]
        page.locator(sel).first.click()
        return f"clicked '{sel}'"

    elif cmd == "fill":
        sel, value = args[0], args[1]
        page.fill(sel, value)
        return f"filled '{sel}'"

    elif cmd == "type":
        page.keyboard.type(args[0])
        return f"typed '{args[0]}'"

    elif cmd == "press":
        page.keyboard.press(args[0])
        return f"pressed '{args[0]}'"

    elif cmd == "shot":
        name = args[0] if args else "page"
        ts = time.strftime("%Y%m%d_%H%M%S")
        path = OUT_DIR / f"browser_{name}_{ts}.png"
        page.screenshot(path=str(path), full_page=False)
        return str(path)

    elif cmd == "html":
        sel = args[0] if args else "body"
        return page.locator(sel).first.inner_html()

    elif cmd == "js":
        return page.evaluate(args[0])

    elif cmd == "wait":
        page.wait_for_selector(args[0], timeout=10000)
        return f"found '{args[0]}'"

    elif cmd == "wait-ms":
        ms = int(args[0]) if args else 1000
        time.sleep(ms / 1000)
        return f"waited {ms}ms"

    elif cmd == "url":
        return page.url

    elif cmd == "title":
        return page.title()

    elif cmd == "links":
        links = page.locator("a[href]").all()
        result = []
        for link in links[:80]:
            try:
                href = link.get_attribute("href")
                text = link.inner_text().strip()[:80]
                if text:
                    result.append({"text": text, "href": href})
            except Exception:
                pass
        return result

    elif cmd == "screenshot":
        name = args[0] if args else "full"
        ts = time.strftime("%Y%m%d_%H%M%S")
        path = OUT_DIR / f"browser_{name}_{ts}.png"
        page.screenshot(path=str(path), full_page=True)
        return str(path)

    else:
        raise ValueError(f"Unknown command: {cmd}")


# ── CLI mode ──

def cli_single():
    """Handle single-command CLI (legacy mode)."""
    if len(sys.argv) < 2:
        print_usage()
        sys.exit(1)

    cmd = sys.argv[1]
    args = sys.argv[2:]

    actions = [{"cmd": cmd, "args": args}]
    results = run_actions(actions)
    for r in results:
        if r["ok"]:
            val = r["result"]
            if isinstance(val, str):
                print(val)
            elif isinstance(val, dict):
                print(json.dumps(val, ensure_ascii=False, indent=2))
            elif isinstance(val, list):
                for item in val:
                    if isinstance(item, dict):
                        print(f"  {item.get('text','')}")
                    else:
                        print(item)
            else:
                print(val)
        else:
            print(f"Error: {r['error']}", file=sys.stderr)


def print_usage():
    print("Usage: python browser.py <cmd> [args]       (single command)")
    print("       python browser.py --batch <json>     (batch from JSON string)")
    print("       python browser.py --file <jsonfile>  (batch from JSON file)")
    print("       python browser.py --headless <json>  (batch, no visible window)")
    print()
    print("Commands: goto | click | fill | type | press | text | html | js")
    print("          links | shot | screenshot | wait | wait-ms | url | title")
    print()
    print("Batch format: [{\"cmd\":\"goto\",\"args\":[\"https://...\"]}, {\"cmd\":\"click\",\"args\":[\"登录\"]}]")


def main():
    if len(sys.argv) < 2:
        print_usage()
        sys.exit(1)

    if sys.argv[1] == "--batch":
        if len(sys.argv) < 3:
            print("Need JSON string", file=sys.stderr)
            sys.exit(1)
        actions = json.loads(sys.argv[2])
        results = run_actions(actions)
        print(json.dumps(results, ensure_ascii=False, indent=2))

    elif sys.argv[1] == "--headless":
        if len(sys.argv) < 3:
            print("Need JSON string", file=sys.stderr)
            sys.exit(1)
        actions = json.loads(sys.argv[2])
        results = run_actions(actions, headless=True)
        print(json.dumps(results, ensure_ascii=False, indent=2))

    elif sys.argv[1] == "--file":
        if len(sys.argv) < 3:
            print("Need JSON file path", file=sys.stderr)
            sys.exit(1)
        with open(sys.argv[2], "r", encoding="utf-8") as f:
            actions = json.load(f)
        results = run_actions(actions)
        print(json.dumps(results, ensure_ascii=False, indent=2))

    else:
        cli_single()


if __name__ == "__main__":
    main()
