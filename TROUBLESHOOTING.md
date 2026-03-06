# Troubleshooting — Micropad

Common issues and fixes for **Windows BLE pairing**, **connection**, and **app/device** behavior.

---

## Windows BLE pairing

### "Try connecting your device again" / Keyboard pairing fails

- **Cause:** Windows rejects the device due to missing or incorrect HID/Device Info descriptors (e.g. PnP ID, Battery descriptors).
- **Fix:** Use firmware that includes the corrected BLE descriptors and re-pair:
  1. See [Windows_Pairing_Fix.md](Windows_Pairing_Fix.md) for firmware changes (Device Info, PnP ID, Battery, advertising).
  2. Re-flash the ESP32 with the updated firmware.
  3. **Remove** Micropad from **Settings → Bluetooth & devices**.
  4. Power-cycle the Micropad, then add the device again via **Add device → Bluetooth**.

### Scan finds no devices

- **Cause:** Device not advertising, wrong BLE stack, or app filtering by name.
- **Fix:**
  - Ensure Micropad is powered and in range; check Serial (115200) for "Advertising started".
  - App discovers by **Config Service UUID** `4fafc201-1fb5-459e-8fcc-c5c9c331914b` — no name filter. If you changed the firmware to advertise that service, it should appear.
  - See [BLE_Not_Appearing_Fix.md](BLE_Not_Appearing_Fix.md) for NimBLE vs BLEDevice, GPIO conflicts, and diagnostics.

### Connect fails / "FromIdAsync returned null"

- **Cause:** Windows cache or stale pairing; device was paired elsewhere (BLE HID allows one host).
- **Fix:**
  1. **Remove** Micropad from **Settings → Bluetooth & devices**.
  2. Power-cycle the Micropad (and, if needed, restart PC).
  3. Scan again in the app and Connect.
  4. If another device (e.g. phone) was paired, forget Micropad on that device first, then pair on PC.

---

## Connection / GATT

### "GATT services error: Unreachable"

- **Cause:** Device may be connected as HID only; Windows may not expose the config service.
- **Fix:** Remove Micropad from Bluetooth, power-cycle the device, then Connect from the app again. Ensure firmware exposes the config GATT service (`4fafc201-...914b`).

### "Device does not expose the Micropad config service"

- **Cause:** Old or different firmware without the config service.
- **Fix:** Flash firmware that includes the config service (CMD/EVT characteristics). See [firmware/README.md](firmware/README.md) and [PROTOCOL_SPEC.md](PROTOCOL_SPEC.md).

### Connection drops on Windows 11

- **Cause:** BLE connection maintenance.
- **Fix:** The app uses `GattSession` with `MaintainConnection = true`. Enable **Settings → Auto Reconnect** so the app reconnects with exponential backoff after a drop.

### Bluetooth cache / stale device

- Remove the device from **Bluetooth & devices**.
- Disable Bluetooth, wait a few seconds, enable again.
- Restart the app (and optionally the PC) and rescan.

---

## App / device behavior

### Profile push fails

- **Cause:** Not connected, or firmware does not implement `setProfile` yet.
- **Fix:** Ensure connection is active. Firmware currently returns "Not implemented yet" for `setProfile`; implement `PUT_PROFILE` in firmware (see [REQUIREMENTS_SYNC.md](REQUIREMENTS_SYNC.md) and [PROTOCOL_SPEC.md](PROTOCOL_SPEC.md)).

### Stats empty

- **Cause:** Firmware does not implement or populate `getStats` (keyPresses, encoderTurns, uptime).
- **Fix:** Implement and return stats in firmware `getStats` handler.

### Build: "file is locked" / "being used by another process"

- **Cause:** Micropad.App is already running and locking DLLs.
- **Fix:** Close the running app, then build/run again; or use `.\run-app.ps1` in `windows-app` to close and run.

---

## Reference docs

| Topic | Document |
|-------|----------|
| Pairing / HID descriptors | [Windows_Pairing_Fix.md](Windows_Pairing_Fix.md) |
| BLE not appearing / NimBLE / GPIO | [BLE_Not_Appearing_Fix.md](BLE_Not_Appearing_Fix.md) |
| Protocol (commands, chunking) | [PROTOCOL_SPEC.md](PROTOCOL_SPEC.md) |
| Requirements vs implementation | [REQUIREMENTS_SYNC.md](REQUIREMENTS_SYNC.md) |
| Windows app usage | [windows-app/README.md](windows-app/README.md) |
| Firmware quick start | [firmware/README.md](firmware/README.md) |
