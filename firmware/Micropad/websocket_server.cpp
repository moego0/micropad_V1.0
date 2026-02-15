#include "comms/websocket_server.h"
#include "comms/protocol_handler.h"

WebSocketServer::WebSocketServer() {
    _server = nullptr;
    _ws = nullptr;
    _protocolHandler = nullptr;
    _running = false;
}

void WebSocketServer::begin(uint16_t port, ProtocolHandler* handler) {
    _protocolHandler = handler;
    
    DEBUG_PRINTF("Starting WebSocket server on port %d...\n", port);
    
    // Create server
    _server = new AsyncWebServer(port);
    _ws = new AsyncWebSocket("/ws");
    
    // Set up WebSocket handler
    _ws->onEvent([](AsyncWebSocket* server, AsyncWebSocketClient* client,
                    AwsEventType type, void* arg, uint8_t* data, size_t len) {
        // Static callback - need to get instance
        // For simplicity, we'll handle it inline
        switch (type) {
            case WS_EVT_CONNECT:
                DEBUG_PRINTF("WebSocket client #%u connected\n", client->id());
                break;
                
            case WS_EVT_DISCONNECT:
                DEBUG_PRINTF("WebSocket client #%u disconnected\n", client->id());
                break;
                
            case WS_EVT_DATA: {
                AwsFrameInfo* info = (AwsFrameInfo*)arg;
                if (info->final && info->index == 0 && info->len == len) {
                    // Complete message in one frame
                    if (info->opcode == WS_TEXT) {
                        data[len] = 0;  // Null terminate
                        String message = (char*)data;
                        DEBUG_PRINTF("WebSocket RX: %s\n", message.substring(0, 100).c_str());
                        
                        // TODO: Pass to protocol handler
                        // For now, echo back
                        client->text(message);
                    }
                }
                break;
            }
                
            case WS_EVT_PONG:
            case WS_EVT_ERROR:
                break;
        }
    });
    
    _server->addHandler(_ws);
    
    // Simple HTTP handler for testing
    _server->on("/", HTTP_GET, [](AsyncWebServerRequest* request) {
        request->send(200, "text/plain", "Micropad WebSocket Server");
    });
    
    _server->begin();
    _running = true;
    
    DEBUG_PRINTLN("WebSocket server started");
}

void WebSocketServer::stop() {
    if (_running && _server) {
        _server->end();
        delete _ws;
        delete _server;
        _server = nullptr;
        _ws = nullptr;
        _running = false;
        DEBUG_PRINTLN("WebSocket server stopped");
    }
}

void WebSocketServer::broadcast(const String& message) {
    if (_running && _ws) {
        _ws->textAll(message);
    }
}

void WebSocketServer::sendTo(uint32_t clientId, const String& message) {
    if (_running && _ws) {
        _ws->text(clientId, message);
    }
}

uint32_t WebSocketServer::getClientCount() {
    return _running && _ws ? _ws->count() : 0;
}

bool WebSocketServer::isRunning() {
    return _running;
}
