# BLE Windows Connection – How to Verify

Use this checklist after flashing the updated firmware and running the updated Windows app.

---

## Verification checklist (summary)

1. **No COM ports** – Device Manager → Ports: no “Standard Serial over Bluetooth link (COMx)” for Micropad.
2. **Windows pairs as BLE** – Settings → Bluetooth: Micropad appears as a normal BLE device (keyboard), not serial.
3. **App finds Micropad, connects, sees config service** – Scan → Connect → logs show config service UUID `4fafc201-1fb5-459e-8fcc-c5c9c331914b` in discovered services.
4. **Reconnect works** – Disconnect then Connect again, multiple times, without rebooting PC or device.

---

## Before you start

1. **Remove Micropad from Windows Bluetooth**  
   Settings → Bluetooth & devices → find Micropad → Remove device.

2. **Power-cycle Micropad** (unplug USB or reset) so it advertises with the new firmware.

3. **Flash the latest firmware** (NimBLE only, no Classic SPP).  
   Build and upload from `firmware/Micropad/Micropad.ino`.

---

## Verification steps

### 1. No COM ports for Micropad

- Open **Device Manager**.
- Under **Ports (COM & LPT)**, confirm there is **no** “Standard Serial over Bluetooth link (COMx)” for Micropad.
- If you see such a port, the device is still exposing Bluetooth Classic SPP; reflash firmware and remove/pair again.

### 2. Windows shows Micropad as BLE

- **Settings → Bluetooth & devices**.
- Click **Add device → Bluetooth**.
- You should see **Micropad** as a normal Bluetooth device (keyboard), **not** as a serial/COM device.

### 3. Serial debug output (firmware)

With the Micropad connected over USB at **115200 baud** you should see something like:

```
[BLE] Initializing NimBLE (BLE only, no Classic SPP)...
[BLE] Security: bonding=ON, IOCap=NO_INPUT_OUTPUT, secure_conn=ON
[BLE] HID service 0x1812: 2A4A,2A4B,2A4C,2A4E,2A4D+2908 READ_ENC; DeviceInfo 0x180A; Battery 0x180F
[BLE] Advertising started (name + 0x1812, 0x180A, 0x180F, config UUID)
Micropad ready
```

After a client connects (HID reports are delayed 1500 ms to avoid Windows Event 411):

```
[BLE] Client connected, conn_id=...
[BLE] HID ready in 1500 ms (no reports until then)
[BLE] HID ready, reports enabled
```

After disconnect:

```
[BLE] Client disconnected, reason=... (0x...)
[BLE] Advertising restarted
```

If key/encoder events occur before HID is ready you may see (throttled): `[BLE] report blocked (HID not ready)`.

### 4. App Scan finds Micropad

- Open the **Micropad Windows app**.
- Go to the **Devices** (or Bluetooth) tab.
- Click **Scan**.
- Wait up to ~15 seconds.
- **Micropad** should appear in the list (paired or unpaired).

### 5. App Connect pairs and connects

- Select **Micropad** and click **Connect**.
- Pairing should complete (Encryption preferred; fallback to None if needed).
- Status should show **Connected**.
- Device info (ID, FW, HW, Battery) should load.

### 6. Config service in app logs

- In Visual Studio or your logging output, look for lines like:
  - `[BLE] Pairing result: ...`
  - `[BLE] GATT status: Success, discovered services (N): ...` (list of UUIDs).
  - `[BLE] Config service 4fafc201-1fb5-459e-8fcc-c5c9c331914b found.`
- If you see “Device does not expose the Micropad config service”, the log will include the **discovered service UUIDs**; confirm the firmware exposes the config service and that you’re not connecting to an old or different build.

### 7. Disconnect / reconnect without reboot

- Click **Disconnect** in the app.
- Click **Connect** again (or scan again, then connect).
- Connection should succeed again without rebooting the PC or the Micropad.
- Repeat a few times; no disconnect loop and no need to remove/re-pair each time.

---

## If something fails

| Symptom | What to check |
|--------|----------------|
| COM port appears for Micropad | Firmware must be BLE-only (NimBLE). No BluetoothSerial/SPP. Reflash and remove device from Bluetooth, then re-pair. |
| App says “Device does not expose the Micropad config service” | Check app log for “discovered service UUIDs”. Confirm config service `4fafc201-1fb5-459e-8fcc-c5c9c331914b` is in firmware and that advertising is started **after** config service is registered. |
| Pairing fails | App tries Encryption then None. Remove Micropad from Bluetooth, power-cycle Micropad, scan, then Connect again. |
| GATT Unreachable | Remove from Bluetooth, power-cycle Micropad, then Connect. Ensure only one host is paired (no phone/tablet still paired). |
| Connect then immediate disconnect | Update Bluetooth driver; disable power saving for the Bluetooth adapter (Device Manager → adapter → Power Management). |
| **Event 411 / 0xC00000E5** (HID "had a problem starting") | Firmware must expose exactly one HID service (0x1812) with 2A4A, 2A4B, 2A4C, 2A4E, 2A4D+0x2908 and READ_ENC; no duplicate Device Info/Battery. Use NimBLEHIDDevice only; set appearance 0x03C1 (keyboard). Reflash and remove device from Bluetooth, then pair again. |

---

## Summary of fixes applied

- **Firmware:** BLE-only (NimBLE), no BluetoothSerial/SPP → no COM ports. Init order: HID + Config registered, then advertising. Security: bonding, NO_INPUT_OUTPUT, secure connections. **HID timing:** no HID reports until 1500 ms after connect (avoids Event 411). Serial logs: advertising start/stop, connect/disconnect reason, “HID ready”, “report blocked” when not ready.
- **Windows app:** Pairing with Encryption first, fallback None. After connect: enumerate all GATT services and log UUIDs. If config service missing, error includes discovered UUID list. Dispose BluetoothLEDevice/GATT on failure. Reconnect with backoff; no stale handles.
