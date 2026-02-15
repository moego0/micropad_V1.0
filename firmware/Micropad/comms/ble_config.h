#ifndef BLE_CONFIG_H
#define BLE_CONFIG_H

#include <Arduino.h>
#include <NimBLEDevice.h>
#include "../config.h"

// Forward declaration
class ProtocolHandler;

class BLEConfigService {
public:
    BLEConfigService();
    
    void begin(ProtocolHandler* handler);
    void update();
    
    // Send events to connected clients
    void sendEvent(const String& jsonEvent);
    
    // Connection status
    bool isConnected();
    uint32_t getClientCount();
    
    friend class ConfigCharCallbacks;
    
private:
    NimBLEServer* _server;
    NimBLEService* _configService;
    NimBLECharacteristic* _cmdChar;
    NimBLECharacteristic* _evtChar;
    NimBLECharacteristic* _bulkChar;
    
    ProtocolHandler* _protocolHandler;
    
    String _rxBuffer;
    bool _isReceivingChunked;
    uint16_t _chunkIndex;
    uint16_t _totalChunks;
    
    static BLEConfigService* _instance;
    
    // Callbacks
    static void onWrite(NimBLECharacteristic* pChar);
    void handleWrite(NimBLECharacteristic* pChar);
    
    // Chunking support
    void handleChunkedMessage(const String& chunk);
    void sendChunked(const String& message);
};

// Server callbacks
class ConfigServerCallbacks : public NimBLEServerCallbacks {
    void onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) override;
    void onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) override;
};

// Characteristic write callbacks (for CMD and BULK)
class ConfigCharCallbacks : public NimBLECharacteristicCallbacks {
    void onWrite(NimBLECharacteristic* pCharacteristic, NimBLEConnInfo& connInfo) override;
};

#endif // BLE_CONFIG_H
