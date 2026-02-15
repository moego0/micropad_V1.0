#ifndef WEBSOCKET_SERVER_H
#define WEBSOCKET_SERVER_H

#include <Arduino.h>
#include <AsyncTCP.h>
#include <ESPAsyncWebServer.h>
#include "config.h"

// Forward declaration
class ProtocolHandler;

class WebSocketServer {
public:
    WebSocketServer();
    
    void begin(uint16_t port, ProtocolHandler* handler);
    void stop();
    
    // Send messages
    void broadcast(const String& message);
    void sendTo(uint32_t clientId, const String& message);
    
    // Status
    uint32_t getClientCount();
    bool isRunning();
    
private:
    AsyncWebServer* _server;
    AsyncWebSocket* _ws;
    ProtocolHandler* _protocolHandler;
    bool _running;
    
    // Event handlers
    static void onEvent(AsyncWebSocket* server, AsyncWebSocketClient* client,
                       AwsEventType type, void* arg, uint8_t* data, size_t len);
    
    void handleWebSocketMessage(AsyncWebSocketClient* client, void* arg,
                                uint8_t* data, size_t len);
};

#endif // WEBSOCKET_SERVER_H
