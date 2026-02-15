#include "comms/wifi_manager.h"

WiFiManager::WiFiManager() {
    _apMode = false;
    _connected = false;
}

bool WiFiManager::startAP(const char* ssid, const char* password) {
    DEBUG_PRINTLN("Starting WiFi AP mode...");
    
    // Generate SSID if not provided
    String apSSID = ssid ? String(ssid) : String(WIFI_AP_SSID) + String((uint32_t)ESP.getEfuseMac(), HEX);
    String apPassword = password ? String(password) : String(WIFI_AP_PASSWORD);
    
    WiFi.mode(WIFI_AP);
    
    if (!WiFi.softAP(apSSID.c_str(), apPassword.c_str())) {
        DEBUG_PRINTLN("ERROR: Failed to start AP");
        return false;
    }
    
    _apMode = true;
    
    DEBUG_PRINTF("AP started: %s\n", apSSID.c_str());
    DEBUG_PRINTF("AP IP: %s\n", WiFi.softAPIP().toString().c_str());
    
    return true;
}

void WiFiManager::stopAP() {
    if (_apMode) {
        WiFi.softAPdisconnect(true);
        _apMode = false;
        DEBUG_PRINTLN("AP stopped");
    }
}

bool WiFiManager::connectSTA(const char* ssid, const char* password, uint32_t timeoutMs) {
    DEBUG_PRINTF("Connecting to WiFi: %s\n", ssid);
    
    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid, password);
    
    uint32_t startTime = millis();
    
    while (WiFi.status() != WL_CONNECTED) {
        if (millis() - startTime > timeoutMs) {
            DEBUG_PRINTLN("WiFi connection timeout");
            return false;
        }
        delay(100);
    }
    
    _connected = true;
    _apMode = false;
    
    DEBUG_PRINTLN("WiFi connected!");
    DEBUG_PRINTF("IP: %s\n", WiFi.localIP().toString().c_str());
    
    return true;
}

void WiFiManager::disconnect() {
    WiFi.disconnect(true);
    _connected = false;
    DEBUG_PRINTLN("WiFi disconnected");
}

bool WiFiManager::isConnected() {
    return WiFi.status() == WL_CONNECTED;
}

bool WiFiManager::isAPMode() {
    return _apMode;
}

String WiFiManager::getIP() {
    return WiFi.localIP().toString();
}

String WiFiManager::getAPIP() {
    return WiFi.softAPIP().toString();
}

bool WiFiManager::startMDNS(const char* hostname) {
    _hostname = hostname;
    
    if (!MDNS.begin(hostname)) {
        DEBUG_PRINTLN("ERROR: Failed to start mDNS");
        return false;
    }
    
    DEBUG_PRINTF("mDNS started: %s.local\n", hostname);
    
    return true;
}

void WiFiManager::update() {
    // Update connection status
    _connected = (WiFi.status() == WL_CONNECTED);
}
