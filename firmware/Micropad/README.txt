╔════════════════════════════════════════════════════════════════╗
║              MICROPAD ARDUINO IDE PROJECT                      ║
╚════════════════════════════════════════════════════════════════╝

📁 YOU ARE HERE: firmware/Micropad/

This folder contains the complete Arduino IDE sketch for Micropad.

═══════════════════════════════════════════════════════════════

🚀 QUICK START:

1. Open "Micropad.ino" in Arduino IDE
2. Install libraries (see below)
3. Configure board settings
4. Click Upload
5. Open Serial Monitor (115200 baud)

═══════════════════════════════════════════════════════════════

📚 REQUIRED LIBRARIES:

Install via Library Manager:
  - NimBLE-Arduino (v1.4.1+)
  - ArduinoJson (v6.21.3+)

Download & Add as ZIP:
  - ESPAsyncWebServer (from GitHub)
  - AsyncTCP (from GitHub)

═══════════════════════════════════════════════════════════════

⚙️ BOARD SETTINGS:

Tools → Board: "ESP32 Dev Module"
Tools → Partition Scheme: "Huge APP (3MB No OTA/1MB SPIFFS)"
Tools → Upload Speed: 921600
Tools → Port: (Select your COM port)

═══════════════════════════════════════════════════════════════

📋 FILES IN THIS FOLDER:

Micropad.ino              - Main Arduino sketch
config.h                  - Pin definitions & settings

input/
  matrix.h/cpp            - Key matrix scanning
  encoder.h/cpp           - Rotary encoder handling
  combo_detector.h/cpp    - Key combination detection

comms/
  ble_hid.h/cpp          - BLE keyboard/mouse
  ble_config.h/cpp       - BLE config service
  protocol_handler.h/cpp - JSON protocol
  wifi_manager.h/cpp     - WiFi management
  websocket_server.h/cpp - WebSocket server

actions/
  action_executor.h/cpp  - Execute key actions

profiles/
  profile.h              - Profile data structures
  profile_storage.h/cpp  - LittleFS storage
  profile_manager.h/cpp  - Profile management
  default_profile.h      - Default profile
  profile_templates.h    - VS Code, Creative profiles

═══════════════════════════════════════════════════════════════

🆘 HELP:

Upload Failed?
  → Hold BOOT button during upload

Libraries Not Found?
  → Install via Library Manager first

Keys Not Working?
  → Check serial monitor at 115200 baud

Need More Help?
  → See ../QUICK_START.md
  → See ../DEPLOYMENT_GUIDE.md
  → See ../ARDUINO_IDE_SETUP.md

═══════════════════════════════════════════════════════════════

✅ SUCCESS LOOKS LIKE:

Serial Monitor (115200 baud):
  ========================================
  Micropad Firmware 1.0.0
  ========================================
  Matrix initialized
  Encoder initialized...
  Profile loaded: General
  Micropad ready! Waiting for BLE connection...
  ========================================

Press keys → See "Key X pressed" messages

═══════════════════════════════════════════════════════════════

🎉 Ready? Open Micropad.ino and click Upload! →

For step-by-step guide: ../QUICK_START.md
