"""V11 Deploy - final version with UTF-8 and known VMX paths."""
import sys
import ctypes
from ctypes import c_int, c_char_p, c_void_p, c_ulonglong

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.stderr.reconfigure(encoding='utf-8', errors='replace')

dll = ctypes.WinDLL(r"C:\Program Files (x86)\VMware\VMware VIX\Workstation-17.0.0\64bit\vix.dll")

VixHandle = c_int
VixError = c_ulonglong
null_cb = ctypes.CFUNCTYPE(None, c_int, c_int, c_int, c_void_p)(0)

PROP_HANDLE = 3010
PROP_ITEM_NAME = 3035
PROP_FOUND_LOCATION = 4010

def setup(name, restype, *argtypes):
    fn = getattr(dll, name)
    fn.restype = restype
    fn.argtypes = list(argtypes)
    return fn

VixHost_Connect = setup('VixHost_Connect', VixHandle,
    c_int, c_int, c_char_p, c_int, c_char_p, c_char_p, c_int, VixHandle,
    ctypes.CFUNCTYPE(None, c_int, c_int, c_int, c_void_p), c_void_p)

VixJob_Wait = setup('VixJob_Wait', VixError, VixHandle, c_int, VixHandle)
Vix_ReleaseHandle = setup('Vix_ReleaseHandle', None, VixHandle)
Vix_GetErrorText = setup('Vix_GetErrorText', c_char_p, VixError, c_char_p, c_int)
Vix_GetProperties = setup('Vix_GetProperties', None, VixHandle, c_int, c_void_p, c_int)
VixHost_Disconnect = setup('VixHost_Disconnect', None, VixHandle)

VixVM_Open = setup('VixVM_Open', VixHandle,
    VixHandle, c_char_p, ctypes.CFUNCTYPE(None, c_int, c_int, c_int, c_void_p), c_void_p)

VixVM_LoginInGuest = setup('VixVM_LoginInGuest', VixHandle,
    VixHandle, c_char_p, c_char_p, c_int,
    ctypes.CFUNCTYPE(None, c_int, c_int, c_int, c_void_p), c_void_p)

VixVM_CopyFileFromHostToGuest = setup('VixVM_CopyFileFromHostToGuest', VixHandle,
    VixHandle, c_char_p, c_char_p, c_int, VixHandle,
    ctypes.CFUNCTYPE(None, c_int, c_int, c_int, c_void_p), c_void_p)


def get_prop(handle, prop_id, as_string=False):
    if as_string:
        buf = ctypes.create_string_buffer(4096)
        Vix_GetProperties(handle, prop_id, buf, 0)
        raw = buf.raw.split(b'\x00')[0]
        return raw.decode('utf-8', errors='replace')
    else:
        result = c_int(0)
        Vix_GetProperties(handle, prop_id, ctypes.byref(result), 0)
        return result.value


def wait(job):
    err = VixJob_Wait(job, 0, 0)
    return err


def etext(code):
    buf = ctypes.create_string_buffer(1024)
    Vix_GetErrorText(code, buf, 1024)
    return buf.value.decode(errors='replace') if buf.value else f"E{code}"


print("=" * 60)
print("V11 Installer -> VM Deploy")
print("=" * 60)

# Connect
print("\n[1] Connect to VMware...")
job = VixHost_Connect(-1, 3, None, 0, None, None, 0, 0, null_cb, None)
err = wait(job)
print(f"    Connect: {etext(err)}")
host = get_prop(job, PROP_HANDLE)
Vix_ReleaseHandle(job)
print(f"    Host handle: {host}")

# Try known VMX paths
vmx_paths = [
    r"D:\mvos\其他 Linux 6.x 内核 64 位.vmx",
]

# Also try FindItems approach
from ctypes import c_int as CI, c_char_p as CP, c_void_p as VP
VixHost_FindItems = setup('VixHost_FindItems', VixHandle,
    VixHandle, CI, VixHandle, CI,
    ctypes.CFUNCTYPE(None, CI, CI, CI, VP), VP)

find_job = VixHost_FindItems(host, 1, 0, -1, null_cb, None)
find_err = wait(find_job)
print(f"\n[2] FindItems: {etext(find_err)}")

# Try getting VM location from FindItems
vm_loc = get_prop(find_job, PROP_FOUND_LOCATION, as_string=True)
print(f"    VM location: '{vm_loc}'")

vm_name = get_prop(find_job, PROP_ITEM_NAME, as_string=True)
print(f"    VM name: '{vm_name}'")

if vm_loc and vm_loc != '��':
    vmx_paths.insert(0, vm_loc)

Vix_ReleaseHandle(find_job)

# Try each VMX path
success = False
for vmx in vmx_paths:
    print(f"\n[3] Trying VMX: {vmx}")

    vm_job = VixVM_Open(host, vmx.encode('utf-8'), null_cb, None)
    err = wait(vm_job)
    if err != 0:
        print(f"    Open: {etext(err)}")
        continue

    vm = get_prop(vm_job, PROP_HANDLE)
    Vix_ReleaseHandle(vm_job)
    print(f"    VM handle: {vm}")

    # Login
    login_job = VixVM_LoginInGuest(vm, b"shimizu", b"101022", 0, null_cb, None)
    login_err = wait(login_job)
    print(f"    Login: {etext(login_err)}")
    Vix_ReleaseHandle(login_job)

    # Copy
    src = r"F:\ClaudeCodeInstaller\publish\ClaudeCodeInstaller.exe"
    dst = r"C:\Users\shimizu\Desktop\ClaudeCodeInstaller.exe"
    print(f"    Copy: {src} -> {dst}")

    copy_job = VixVM_CopyFileFromHostToGuest(vm, src.encode('utf-8'), dst.encode('utf-8'), 0, 0, null_cb, None)
    copy_err = wait(copy_job)
    print(f"    Copy: {etext(copy_err)}")
    Vix_ReleaseHandle(copy_job)

    if copy_err == 0:
        print("\n" + "=" * 60)
        print("SUCCESS! File deployed to VM desktop!")
        print("=" * 60)
        success = True
    else:
        # Try alternate guest paths
        for alt_dst in [r"C:\cc-install.exe", r"C:\Users\Public\Desktop\cc-install.exe"]:
            print(f"    Retry: {alt_dst}")
            cj = VixVM_CopyFileFromHostToGuest(vm, src.encode('utf-8'), alt_dst.encode('utf-8'), 0, 0, null_cb, None)
            ce = wait(cj)
            print(f"    Copy: {etext(ce)}")
            Vix_ReleaseHandle(cj)
            if ce == 0:
                print(f"    SUCCESS at {alt_dst}!")
                success = True
                break

    Vix_ReleaseHandle(vm)
    if success:
        break

if not success:
    print("\nFalling back to HTTP download via keyboard automation...")
    # The deploy_to_vm.py script handles this

VixHost_Disconnect(host)
print("\nDone.")
