#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <Arduino.h>
#include <WiFi.h>
#include <ESPmDNS.h>
#include "../config.h"

class WiFiManager {
public:
    WiFiManager();
    
    // AP mode (for initial setup)
    bool startAP(const char* ssid = nullptr, const char* password = nullptr);
    void stopAP();
    
    // Station mode (connect to existing network)
    bool connectSTA(const char* ssid, const char* password, uint32_t timeoutMs = 10000);
    void disconnect();
    
    // Status
    bool isConnected();
    bool isAPMode();
    String getIP();
    String getAPIP();
    
    // mDNS
    bool startMDNS(const char* hostname);
    
    void update();
    
private:
    bool _apMode;
    bool _connected;
    String _hostname;
};

#endif // WIFI_MANAGER_H
