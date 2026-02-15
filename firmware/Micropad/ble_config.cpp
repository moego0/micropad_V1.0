#include "ble_config.h"
#include "protocol_handler.h"

BLEConfigService* BLEConfigService::_instance = nullptr;

BLEConfigService::BLEConfigService() {
    _server = nullptr;
    _configService = nullptr;
    _cmdChar = nullptr;
    _evtChar = nullptr;
    _bulkChar = nullptr;
    _protocolHandler = nullptr;
    _isReceivingChunked = false;
    _chunkIndex = 0;
    _totalChunks = 0;
    _instance = this;
}

void BLEConfigService::begin(ProtocolHandler* handler) {
    _protocolHandler = handler;
    
    DEBUG_PRINTLN("Starting BLE Config Service...");
    
    // Get the server (should already be created by BLE HID)
    _server = NimBLEDevice::getServer();
    if (!_server) {
        DEBUG_PRINTLN("ERROR: BLE Server not initialized!");
        return;
    }
    
    // Set server callbacks
    _server->setCallbacks(new ConfigServerCallbacks());
    
    // Create config service
    _configService = _server->createService(CONFIG_SERVICE_UUID);
    
    // CMD characteristic (Write)
    _cmdChar = _configService->createCharacteristic(
        CMD_CHAR_UUID,
        NIMBLE_PROPERTY::WRITE | NIMBLE_PROPERTY::WRITE_NR
    );
    _cmdChar->setCallbacks(new ConfigCharCallbacks());
    
    // EVT characteristic (Notify)
    _evtChar = _configService->createCharacteristic(
        EVT_CHAR_UUID,
        NIMBLE_PROPERTY::NOTIFY
    );
    
    // BULK characteristic (Write, for large transfers)
    _bulkChar = _configService->createCharacteristic(
        BULK_CHAR_UUID,
        NIMBLE_PROPERTY::WRITE | NIMBLE_PROPERTY::WRITE_NR
    );
    _bulkChar->setCallbacks(new ConfigCharCallbacks());
    
    // Start the service
    _configService->start();
    
    // Update advertising to include config service
    NimBLEAdvertising* advertising = NimBLEDevice::getAdvertising();
    advertising->addServiceUUID(_configService->getUUID());
    advertising->start();
    
    DEBUG_PRINTLN("BLE Config Service started");
}

void BLEConfigService::update() {
    // Nothing to do in update for now
}

void BLEConfigService::sendEvent(const String& jsonEvent) {
    if (!isConnected()) {
        return;
    }
    
    // Check size
    if (jsonEvent.length() > 512) {
        // Need chunking
        sendChunked(jsonEvent);
    } else {
        // Send directly
        _evtChar->setValue(jsonEvent.c_str());
        _evtChar->notify();
    }
}

bool BLEConfigService::isConnected() {
    return _server && _server->getConnectedCount() > 0;
}

uint32_t BLEConfigService::getClientCount() {
    return _server ? _server->getConnectedCount() : 0;
}

void BLEConfigService::handleWrite(NimBLECharacteristic* pChar) {
    String value = pChar->getValue().c_str();
    
    if (value.length() == 0) {
        return;
    }
    
    DEBUG_PRINTF("BLE Config RX: %s\n", value.substring(0, 100).c_str());
    
    // Check if this is a chunked message
    if (value.indexOf("\"chunk\":") >= 0) {
        handleChunkedMessage(value);
    } else {
        // Complete message, pass to protocol handler
        if (_protocolHandler) {
            _protocolHandler->handleMessage(value);
        }
    }
}

void BLEConfigService::handleChunkedMessage(const String& chunk) {
    // Parse chunk info
    // Format: {"chunk":0, "total":3, "data":"..."}
    
    int chunkStart = chunk.indexOf("\"chunk\":") + 8;
    int chunkEnd = chunk.indexOf(",", chunkStart);
    int chunkNum = chunk.substring(chunkStart, chunkEnd).toInt();
    
    int totalStart = chunk.indexOf("\"total\":") + 8;
    int totalEnd = chunk.indexOf(",", totalStart);
    int totalChunks = chunk.substring(totalStart, totalEnd).toInt();
    
    int dataStart = chunk.indexOf("\"data\":\"") + 8;
    int dataEnd = chunk.lastIndexOf("\"");
    String data = chunk.substring(dataStart, dataEnd);
    
    if (chunkNum == 0) {
        // First chunk, reset buffer
        _rxBuffer = "";
        _isReceivingChunked = true;
        _chunkIndex = 0;
        _totalChunks = totalChunks;
    }
    
    // Append data
    _rxBuffer += data;
    _chunkIndex++;
    
    DEBUG_PRINTF("Chunk %d/%d received (%d bytes)\n", _chunkIndex, _totalChunks, data.length());
    
    // Check if complete
    if (_chunkIndex >= _totalChunks) {
        DEBUG_PRINTLN("All chunks received, processing...");
        
        if (_protocolHandler) {
            _protocolHandler->handleMessage(_rxBuffer);
        }
        
        // Reset
        _rxBuffer = "";
        _isReceivingChunked = false;
        _chunkIndex = 0;
        _totalChunks = 0;
    }
}

void BLEConfigService::sendChunked(const String& message) {
    const uint16_t CHUNK_SIZE = 480;  // Leave room for JSON overhead
    uint16_t totalChunks = (message.length() + CHUNK_SIZE - 1) / CHUNK_SIZE;
    
    DEBUG_PRINTF("Sending chunked message: %d bytes in %d chunks\n", message.length(), totalChunks);
    
    for (uint16_t i = 0; i < totalChunks; i++) {
        uint16_t start = i * CHUNK_SIZE;
        uint16_t len = min(CHUNK_SIZE, (uint16_t)(message.length() - start));
        String data = message.substring(start, start + len);
        
        // Create chunk envelope
        String chunk = "{\"chunk\":" + String(i) + 
                      ",\"total\":" + String(totalChunks) + 
                      ",\"data\":\"" + data + "\"}";
        
        _evtChar->setValue(chunk.c_str());
        _evtChar->notify();
        
        delay(10);  // Small delay between chunks
    }
    
    DEBUG_PRINTLN("Chunked message sent");
}

// Characteristic callbacks
void ConfigCharCallbacks::onWrite(NimBLECharacteristic* pCharacteristic, NimBLEConnInfo& connInfo) {
    if (BLEConfigService::_instance) {
        BLEConfigService::_instance->handleWrite(pCharacteristic);
    }
}

// Server callbacks
void ConfigServerCallbacks::onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) {
    DEBUG_PRINTLN("BLE Config client connected");
    // Update connection parameters for Windows stability (30-50ms interval, 4s timeout)
    pServer->updateConnParams(connInfo.getConnHandle(), 24, 40, 0, 400);
}

void ConfigServerCallbacks::onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) {
    DEBUG_PRINTLN("BLE Config client disconnected");
    // Restart advertising so Windows (or other hosts) can connect again
    NimBLEDevice::startAdvertising();
}
