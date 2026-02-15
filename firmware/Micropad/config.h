#ifndef CONFIG_H
#define CONFIG_H

#include <Arduino.h>

// ============================================
// Hardware Configuration
// ============================================

// Device Information
#define DEVICE_NAME "Micropad"
#define FIRMWARE_VERSION "1.0.0"
#define HARDWARE_VERSION "1.0"

// Key Matrix Configuration
#define MATRIX_ROWS 3
#define MATRIX_COLS 4
#define MATRIX_KEYS 12

// Matrix Pin Assignments
const uint8_t ROW_PINS[MATRIX_ROWS] = {16, 17, 18};
const uint8_t COL_PINS[MATRIX_COLS] = {21, 22, 23, 19};

// Encoder 1 (Top-Left)
#define ENC1_PIN_A 32
#define ENC1_PIN_B 33
#define ENC1_PIN_SW 27

// Encoder 2 (Top-Right)
#define ENC2_PIN_A 25
#define ENC2_PIN_B 26
#define ENC2_PIN_SW 13

// ============================================
// Timing Configuration
// ============================================

// Debouncing
#define DEBOUNCE_MS 5

// Key Behaviors
#define HOLD_THRESHOLD_MS 500
#define DOUBLE_TAP_WINDOW_MS 300
#define TURBO_INTERVAL_MS 50

// Encoder
#define ENCODER_STEPS_PER_DETENT 4
#define ENCODER_ACCEL_THRESHOLD_MS 50

// ============================================
// Profile Configuration
// ============================================

#define MAX_PROFILES 8
#define DEFAULT_PROFILE 0

// ============================================
// Communication Configuration
// ============================================

// BLE
#define BLE_DEVICE_NAME DEVICE_NAME
#define BLE_MANUFACTURER "Custom"

// BLE Service UUIDs
#define CONFIG_SERVICE_UUID "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CMD_CHAR_UUID "4fafc201-1fb5-459e-8fcc-c5c9c331914c"
#define EVT_CHAR_UUID "4fafc201-1fb5-459e-8fcc-c5c9c331914d"
#define BULK_CHAR_UUID "4fafc201-1fb5-459e-8fcc-c5c9c331914e"

// WiFi
#define WIFI_AP_SSID "Micropad-"
#define WIFI_AP_PASSWORD "micropad123"
#define WEBSOCKET_PORT 8765
#define MDNS_HOSTNAME "micropad"

// ============================================
// Power Management
// ============================================

#define SLEEP_TIMEOUT_MS 300000  // 5 minutes
#define LOW_BATTERY_THRESHOLD 20  // percentage

// ============================================
// Storage Configuration
// ============================================

#define PROFILES_PATH "/profiles"
#define PREFS_NAMESPACE "micropad"

// ============================================
// Debug Configuration
// ============================================

#define DEBUG_SERIAL Serial
#define DEBUG_ENABLED false

#if DEBUG_ENABLED
    #define DEBUG_PRINT(x) DEBUG_SERIAL.print(x)
    #define DEBUG_PRINTLN(x) DEBUG_SERIAL.println(x)
    #define DEBUG_PRINTF(fmt, ...) DEBUG_SERIAL.printf(fmt, ##__VA_ARGS__)
#else
    #define DEBUG_PRINT(x)
    #define DEBUG_PRINTLN(x)
    #define DEBUG_PRINTF(fmt, ...)
#endif

#endif // CONFIG_H
