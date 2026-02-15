# BLE Connection Troubleshooting (PC / Windows)

## Do I need to download a driver?

**For using the Micropad as a wireless keyboard (Bluetooth):**  
Usually **no extra download** is needed — Windows uses built-in Bluetooth and HID drivers. If you see "driver error" or the device **connects then disconnects**, it usually means:

- The **Bluetooth driver** on your PC should be updated (see below), or  
- Another device (e.g. your phone) is still connected to the Micropad, or  
- Power saving is turning off the Bluetooth adapter.

**For flashing firmware over USB:**  
You may need a **USB-serial driver** (CH340 or CP2102) so the PC can see the ESP32. That is separate from Bluetooth. See ARDUINO_IDE_SETUP.md or DEPLOYMENT_GUIDE.md for "Failed to connect to ESP32".

---

## "Driver error" or connects then disconnects

If the keyboard **connects then immediately disconnects** and Windows shows a **driver error**:

1. **Update Bluetooth driver**
   - Open **Device Manager** (Win+X → Device Manager).
   - Expand **Bluetooth**.
   - Right‑click your Bluetooth adapter → **Update driver** → **Search automatically**.
   - Or download the latest driver from your laptop/PC manufacturer's support site (Dell, HP, Lenovo, etc.) and install it.

2. **Remove and re‑pair**
   - **Settings → Bluetooth & devices** → remove **Micropad** (or the keyboard entry).
   - Restart the PC.
   - Power cycle the Micropad (unplug/replug or reset).
   - **Add device** → **Bluetooth** → pair **Micropad** again.

3. **Try another Bluetooth adapter**
   - If you use a **USB Bluetooth dongle**, try the laptop's **built‑in** Bluetooth (or the other way around). Some adapters have poor BLE HID support.

4. **Windows Update**
   - **Settings → Windows Update** → **Check for updates** and install any optional updates related to Bluetooth or drivers.

5. **Disable power saving for Bluetooth**
   - **Device Manager** → **Bluetooth** → right‑click adapter → **Properties** → **Power Management**.
   - Uncheck **Allow the computer to turn off this device to save power** → OK.

If it still fails on that laptop, the Bluetooth stack or HID driver on that machine may not work well with BLE keyboards; the same Micropad usually works on other PCs or phones after updating drivers and re‑pairing.

---

## One host at a time

**BLE HID allows only one connected host.** If your iPhone is connected (or was the last device connected), the PC cannot connect until the Micropad is free.

### To connect from PC when iPhone worked first

1. **On iPhone:** Settings → Bluetooth → find **Micropad** → tap (i) → **Forget This Device** (or turn off Bluetooth on the iPhone).
2. **Power cycle the Micropad:** unplug USB or press reset so it starts advertising again.
3. **On PC:** Settings → Bluetooth & devices → **Add device** → Bluetooth → select **Micropad** when it appears.

### To connect from iPhone when PC was used last

1. **On PC:** Settings → Bluetooth & devices → remove **Micropad** / Keyboard.
2. Power cycle the Micropad.
3. On iPhone, add **Micropad** in Bluetooth settings.

---

## Windows still won't connect

- **Remove and re-add:** In Bluetooth & devices, remove Micropad if it's there, then Add device → Bluetooth and pair again.
- **Bluetooth adapter:** If you use a USB BLE dongle, try the PC's built-in Bluetooth (or the other way around). Update the Bluetooth driver from the PC/laptop manufacturer.
- **Troubleshooter:** Run **Troubleshoot Bluetooth connections** (link shown when pairing fails), then try again.
- **Restart:** Restart the PC and the Micropad, then try pairing once.

---

## Summary

| Step | Action |
|------|--------|
| 1 | Disconnect or forget Micropad on the **other** device (e.g. iPhone) first. |
| 2 | Power cycle the Micropad (unplug/replug or reset). |
| 3 | On the device you want to use (e.g. PC), remove Micropad if listed, then Add device → Bluetooth → Micropad. |
