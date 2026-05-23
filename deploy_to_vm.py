"""Deploy V11 to VM - direct pyautogui approach, no OCR needed."""
import time
import pygetwindow as gw
import pyautogui

pyautogui.FAILSAFE = True

# 1. Find and focus VMware VM window
vm_title = None
for w in gw.getAllWindows():
    if 'Windows 10' in w.title and 'VMware' in w.title and w.width > 400:
        vm_title = w
        break

if not vm_title:
    for w in gw.getAllWindows():
        if 'VMware' in w.title and w.width > 800:
            vm_title = w
            break

if not vm_title:
    print("ERROR: VMware VM window not found!")
    for w in gw.getAllWindows():
        if w.width > 200 and w.title.strip():
            print(f"  '{w.title}' {w.width}x{w.height}")
    exit(1)

print(f"VM window: '{vm_title.title}' ({vm_title.width}x{vm_title.height})")
print(f"Position: left={vm_title.left}, top={vm_title.top}")

# Focus
if vm_title.isMinimized:
    vm_title.restore()
    time.sleep(0.3)
vm_title.activate()
time.sleep(0.5)

# 2. Click in VM display area (center of window)
# The VM display area starts below the VMware toolbar (~50px from top)
# and excludes the status bar at bottom (~20px)
vm_display_x = vm_title.left + vm_title.width // 2
vm_display_y = vm_title.top + vm_title.height // 2
print(f"Clicking VM display at ({vm_display_x}, {vm_display_y})")
pyautogui.click(vm_display_x, vm_display_y)
time.sleep(0.2)

# 3. Try typing Ctrl+G to release grab, then click again
pyautogui.hotkey('ctrl', 'g')
time.sleep(0.2)

# Another click to ensure VM grabs input
pyautogui.click(vm_display_x, vm_display_y)
time.sleep(0.3)

# Click once more for good measure
pyautogui.click(vm_display_x, vm_display_y)
time.sleep(0.3)

# 4. Now press Win key (should open Start menu in VM)
print("Pressing Win key...")
pyautogui.press('win')
time.sleep(0.5)

# Press Escape to close Start menu if it opened
pyautogui.press('esc')
time.sleep(0.2)

# 5. Win+R for Run dialog
print("Win+R...")
pyautogui.hotkey('win', 'r')
time.sleep(1.0)

# 6. Type the download command
cmd = 'powershell -c "iwr http://192.168.44.1:8888/ClaudeCodeInstaller.exe -OutFile $env:USERPROFILE\\Desktop\\cc-install.exe"'
print(f"Typing command...")
pyautogui.typewrite(cmd, interval=0.02)
time.sleep(0.3)

# 7. Press Enter
print("Pressing Enter...")
pyautogui.press('enter')

print("\nDone! Command should be executing in VM.")
print("Check HTTP server logs for download request.")
