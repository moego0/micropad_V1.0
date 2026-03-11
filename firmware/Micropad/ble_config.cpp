#include "ble_config.h"
#include "protocol_handler.h"

#if defined(ESP32)
#include "mbedtls/base64.h"
#endif

BLEConfigService* BLEConfigService::_instance = nullptr;

// Decode Base64 to String (UTF-8). Used for dataB64 chunk payload from Windows app.
static String base64Decode(const String& in) {
#if defined(ESP32)
    size_t outLen;
    size_t inLen = in.length();
    if (inLen == 0) return String();
    // Decoded size is at most 3/4 of input
    unsigned char* buf = (unsigned char*)malloc((inLen / 4 + 1) * 3);
    if (!buf) return String();
    int ret = mbedtls_base64_decode(buf, (inLen / 4 + 1) * 3, &outLen, (const unsigned char*)in.c_str(), inLen);
    String out;
    if (ret == 0 && outLen > 0) {
        out = String((char*)buf, outLen);
    }
    free(buf);
    return out;
#else
    (void)in;
    return String();
#endif
}

BLEConfigService::BLEConfigService() {
    _server = nullptr;
    _configService = nullptr;
    _cmdChar = nullptr;
    _evtChar = nullptr;
    _bulkChar = nullptr;
    _protocolHandler = nullptr;
    _configClientActive = false;
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
    // Server callbacks (connect/disconnect, restart advertising) are set in ble_hid; do not overwrite here.
    
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
    
    // Start the service (config is on same server; clients see it after connecting)
    _configService->start();
    
    // Advertising is started in main (bleKeyboard.startAdvertising()) after this returns.
    Serial.println("[BLE] Config service 4fafc201-... started (CMD, EVT, BULK chars)");
    DEBUG_PRINTLN("BLE Config Service started");
}

void BLEConfigService::update() {
    if (_server && _server->getConnectedCount() == 0) {
        _configClientActive = false;
    }
}

bool BLEConfigService::isConfigClientActive() {
    return _configClientActive;
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
    _configClientActive = true;  // This connection is using config service (browser or app)
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

// Chunk format from Windows app: {"chunk":0,"total":N,"dataB64":"base64..."} (preferred)
// Legacy: {"chunk":0,"total":N,"data":"..."}
void BLEConfigService::handleChunkedMessage(const String& chunk) {
    int chunkStart = chunk.indexOf("\"chunk\":") + 8;
    int chunkEnd = chunk.indexOf(",", chunkStart);
    int chunkNum = chunk.substring(chunkStart, chunkEnd).toInt();
    
    int totalStart = chunk.indexOf("\"total\":") + 8;
    int totalEnd = chunk.indexOf(",", totalStart);
    if (totalEnd < 0) totalEnd = chunk.indexOf("}", totalStart);
    int totalChunks = chunk.substring(totalStart, totalEnd).toInt();
    
    String data;
    int dataB64Start = chunk.indexOf("\"dataB64\":\"");
    if (dataB64Start >= 0) {
        dataB64Start += 11;
        int dataB64End = chunk.indexOf("\"", dataB64Start);
        String b64 = chunk.substring(dataB64Start, dataB64End);
        data = base64Decode(b64);
    } else {
        int dataStart = chunk.indexOf("\"data\":\"") + 8;
        int dataEnd = chunk.lastIndexOf("\"");
        if (dataEnd > dataStart)
            data = chunk.substring(dataStart, dataEnd);
        data.replace("\\\"", "\"");
        data.replace("\\\\", "\\");
    }
    
    if (chunkNum == 0) {
        _rxBuffer = "";
        _isReceivingChunked = true;
        _chunkIndex = 0;
        _totalChunks = totalChunks;
    }
    
    _rxBuffer += data;
    _chunkIndex++;
    
    DEBUG_PRINTF("Chunk %d/%d received (%d bytes)\n", _chunkIndex, _totalChunks, data.length());
    
    // Do NOT send chunkAck here - it can flood the link and cause disconnect.
    // Web app uses fixed delay between chunks.
    
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
    // Use base64 encoding for safe transport of JSON payloads
    const uint16_t RAW_CHUNK_SIZE = 360;  // ~480 bytes after base64 expansion
    uint16_t totalChunks = (message.length() + RAW_CHUNK_SIZE - 1) / RAW_CHUNK_SIZE;
    
    DEBUG_PRINTF("Sending chunked message: %d bytes in %d chunks (base64)\n", message.length(), totalChunks);
    
    for (uint16_t i = 0; i < totalChunks; i++) {
        uint16_t start = i * RAW_CHUNK_SIZE;
        uint16_t len = min(RAW_CHUNK_SIZE, (uint16_t)(message.length() - start));
        
        // Base64 encode the chunk payload
        const unsigned char* src = (const unsigned char*)message.c_str() + start;
        size_t b64Len = 0;
        // Calculate required output size
        mbedtls_base64_encode(NULL, 0, &b64Len, src, len);
        char* b64Buf = (char*)malloc(b64Len + 1);
        if (!b64Buf) {
            DEBUG_PRINTLN("ERROR: malloc failed for base64 chunk");
            break;
        }
        mbedtls_base64_encode((unsigned char*)b64Buf, b64Len + 1, &b64Len, src, len);
        b64Buf[b64Len] = '\0';
        
        String chunk = "{\"chunk\":" + String(i) + 
                      ",\"total\":" + String(totalChunks) + 
                      ",\"dataB64\":\"" + String(b64Buf) + "\"}";
        
        free(b64Buf);
        
        _evtChar->setValue(chunk.c_str());
        _evtChar->notify();
        
        yield();
        if (i < totalChunks - 1) delay(20);
    }
    
    DEBUG_PRINTLN("Chunked message sent");
}

// Characteristic callbacks
void ConfigCharCallbacks::onWrite(NimBLECharacteristic* pCharacteristic, NimBLEConnInfo& connInfo) {
    if (BLEConfigService::_instance) {
        BLEConfigService::_instance->handleWrite(pCharacteristic);
    }
}

// Server callbacks are set in ble_hid (HidServerCallbacks) so connect/disconnect and restart advertising are handled there.
void ConfigServerCallbacks::onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) {
    (void)pServer;
    (void)connInfo;
}

void ConfigServerCallbacks::onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) {
    (void)pServer;
    (void)connInfo;
    (void)reason;
    // Advertising restart is done in ble_hid HidServerCallbacks::onDisconnect
}
