# Using PC HID and Browser Config at the Same Time

The Micropad can accept **two BLE connections at once**: one from Windows (as HID keyboard/mouse) and one from the browser (for config). You can edit profiles in the web app while the device is connected to the PC and keys/encoders still work.

## Requirements

1. **NimBLE must allow at least 2 connections.**  
   The default in many Arduino NimBLE builds is **1**. You need **2** (or more).

2. **How to set it (pick one):**

   - **Arduino IDE**  
     Find the NimBLE library (e.g. `NimBLE-Arduino` by h2zero). In its `src` folder look for `nimconfig.h` or a file that defines `CONFIG_BT_NIMBLE_MAX_CONNECTIONS`. Set:
     ```c
     #define CONFIG_BT_NIMBLE_MAX_CONNECTIONS 2
     ```
     If the library has no such file, check the library’s README or open an issue for “max connections”.

   - **PlatformIO**  
     In `platformio.ini` (or your env), add a build flag so the NimBLE config sees a larger max:
     ```ini
     build_flags = -DCONFIG_BT_NIMBLE_MAX_CONNECTIONS=2
     ```
     (Exact flag name may depend on the NimBLE package; check its docs.)

3. **Firmware**  
   Use the firmware that:
   - Restarts advertising after the first connection (so a second central can connect).
   - Tracks the “HID host” connection so HID-ready is only cleared when that connection drops, not when the browser disconnects.

## Behavior

- **First connection**  
  - If it’s the **PC** (pairs as HID, subscribes to HID reports): device becomes HID-ready and keys/encoders work on the PC. Advertising is restarted so the browser can still see the device.
  - If it’s the **browser** (config only): you can edit/push/pull profiles. Advertising is restarted so the PC can still connect.

- **Second connection**  
  - You can have **PC (HID) + browser (config)** at the same time: edit in the web app while using the Micropad as a keyboard/encoder on the PC.

- **getConnectionStatus**  
  - `configConnected`: at least one client has used the config service.  
  - `hidHostConnected` / `hidReady`: the connection that subscribed to HID reports (usually the PC) is still connected and ready.

If you did not set `CONFIG_BT_NIMBLE_MAX_CONNECTIONS >= 2`, only one central can be connected at a time (either PC or browser, not both).
