#include <Arduino.h>
#include <WiFi.h>
#include <WebSocketsServer.h>
#include <ArduinoJson.h>
#include <BLEDevice.h>

// --- הגדרות רשת (חובה לשנות!) ---
const char* ssid = "Leiba";
const char* password = "0549994806";

// --- הגדרות שרת WebSocket ---
WebSocketsServer webSocket = WebSocketsServer(81); // פורט 81 הוא הסטנדרט

// --- הגדרות BLE ---
static String macAddress = "F8:33:31:FC:70:BB";
static BLEUUID serviceUUID("FFE0");
static BLEUUID charUUID("FFE1");
static BLERemoteCharacteristic* pRemoteCharacteristic;
static bool bleConnected = false;

// 1. קבלת טלמטריה מהרובוט ב-BLE, אריזה ל-JSON, ושליחה ל-WebSockets
static void notifyCallback(BLERemoteCharacteristic* pChar, uint8_t* pData, size_t length, bool isNotify) {
  String rawStatus = "";
  for (int i = 0; i < length; i++) rawStatus += (char)pData[i];

  // Serial.print("[BLE INPUT] Raw status from robot: ");
  // Serial.println(rawStatus);

  if (rawStatus.startsWith("$STATUS,")) {
    int distance, safety, mode;
    float battery_v;

    if (sscanf(rawStatus.c_str(), "$STATUS,%d,%d,%d,%f", &distance, &safety, &mode, &battery_v) == 4) {

      // Serial.printf("[BLE PARSED] Distance: %d, Safety: %d, Mode: %d, Battery: %.2f V\n", distance, safety, mode, battery_v);

      StaticJsonDocument<200> doc;
      doc["type"] = "telemetry";
      doc["distance"] = distance;
      doc["safety"] = safety;
      doc["mode"] = mode;
      char batteryStr[10];
      snprintf(batteryStr, sizeof(batteryStr), "%.2f", battery_v);
      doc["battery"] = serialized(batteryStr);

      String jsonString;
      serializeJson(doc, jsonString);

      // Serial.print("[BLE OUTPUT] Sending to WebSocket clients: ");
      // Serial.println(jsonString);

      webSocket.broadcastTXT(jsonString);
    } else {
      Serial.println("[BLE ERROR] Failed to parse STATUS message");
    }
  }
}

// 2. קבלת פקודות JSON מה-WebSockets ושליחתן לרובוט ב-BLE
void webSocketEvent(uint8_t num, WStype_t type, uint8_t * payload, size_t length) {
  if (type == WStype_TEXT) {
    // Serial.printf("[WS INPUT] Client %u - Raw payload: %s\n", num, payload);

    StaticJsonDocument<200> doc;
    DeserializationError error = deserializeJson(doc, payload);

    if (error) {
      Serial.print("[WS ERROR] JSON Deserialization failed: ");
      Serial.println(error.f_str());
      return;
    }

    // Serial.printf("[WS PARSED] Client %u - Successfully parsed JSON\n", num);

    if (bleConnected && pRemoteCharacteristic != nullptr) {
      String cmdType = doc["cmd"];
      // Serial.printf("[WS COMMAND] Parsed command type: %s\n", cmdType.c_str());

      if (cmdType == "move") {
        int left = doc["left"];
        int right = doc["right"];

        char blePayload[30];
        sprintf(blePayload, "$M,%d,%d\n", left, right);

        // Serial.printf("[WS TO BLE] Sending move command - Left: %d, Right: %d\n", left, right);
        // Serial.printf("[WS TO BLE] BLE payload: %s", blePayload);

        pRemoteCharacteristic->writeValue(blePayload, strlen(blePayload));
        // Serial.println("[WS TO BLE] Move command written to BLE characteristic");
      }
      else if (cmdType == "stop") {
        // Serial.println("[WS TO BLE] Sending STOP command");
        pRemoteCharacteristic->writeValue("$S\n", 3);
        // Serial.println("[WS TO BLE] STOP command written to BLE characteristic");
      }
      else {
        Serial.printf("[WS ERROR] Unknown command type: %s\n", cmdType.c_str());
      }
    } else {
      Serial.println("[WS ERROR] Command received but BLE is NOT connected - cannot forward to robot");
    }
  }
  else if (type == WStype_CONNECTED) {
    Serial.printf("[WS EVENT] Client %u connected\n", num);
  }
  else if (type == WStype_DISCONNECTED) {
    Serial.printf("[WS EVENT] Client %u disconnected\n", num);
  }
}

// מחלקה לניהול חיבור ה-BLE
class MyClientCallback : public BLEClientCallbacks {
  void onConnect(BLEClient* pclient) { bleConnected = true; }
  void onDisconnect(BLEClient* pclient) { bleConnected = false; }
};

bool connectToBLE() {
  BLEAddress pAddress(macAddress.c_str());
  // Serial.printf("[BLE CONNECT] Attempting connection to device: %s\n", macAddress.c_str());

  BLEClient* pClient = BLEDevice::createClient();
  pClient->setClientCallbacks(new MyClientCallback());

  if (!pClient->connect(pAddress)) {
    Serial.println("[BLE ERROR] Failed to connect to device");
    return false;
  }

  // Serial.println("[BLE SUCCESS] Device connected");

  BLERemoteService* pRemoteService = pClient->getService(serviceUUID);
  if (pRemoteService == nullptr) {
    Serial.println("[BLE ERROR] Service not found");
    return false;
  }

  // Serial.println("[BLE SUCCESS] Service found");

  pRemoteCharacteristic = pRemoteService->getCharacteristic(charUUID);
  if (pRemoteCharacteristic == nullptr) {
    Serial.println("[BLE ERROR] Characteristic not found");
    return false;
  }

  // Serial.println("[BLE SUCCESS] Characteristic found");

  if(pRemoteCharacteristic->canNotify()) {
    pRemoteCharacteristic->registerForNotify(notifyCallback);
    // Serial.println("[BLE SUCCESS] Registered for notifications");
  } else {
    Serial.println("[BLE WARNING] Characteristic cannot notify");
  }

  return true;
}

void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("\n\n=== ESP32 Robot Gateway Starting ===\n");

  // התחברות ל-Wi-Fi
  Serial.printf("[WIFI] Connecting to SSID: %s\n", ssid);
  WiFi.begin(ssid, password);

  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 20) {
    delay(500);
    // Serial.print(".");
    attempts++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("[WIFI SUCCESS] Wi-Fi Connected!");
    Serial.print("[WIFI] ESP32 IP Address: ");
    Serial.println(WiFi.localIP());
  } else {
    Serial.println("[WIFI ERROR] Wi-Fi connection failed!");
  }

  // הפעלת שרת ה-WebSockets
  webSocket.begin();
  webSocket.onEvent(webSocketEvent);
  Serial.println("[WEBSOCKET] Server started on port 81");

  // התחברות לבלוטות'
  // Serial.println("[SYSTEM] Initializing BLE Device...");
  BLEDevice::init("");
  // Serial.println("[BLE] Attempting to connect to Robot...");

  if(connectToBLE()) {
    Serial.println("[BLE SUCCESS] Connected to Robot!");
  } else {
    Serial.println("[BLE ERROR] BLE Connection failed - robot will not respond to commands");
  }

  Serial.println("\n=== ESP32 Robot Gateway Ready ===\n");
}

static unsigned long lastBleReconnectMs = 0;

void loop() {
  webSocket.loop();

  if (!bleConnected) {
    unsigned long now = millis();
    if (now - lastBleReconnectMs >= 5000) {
      lastBleReconnectMs = now;
      Serial.println("[BLE] Disconnected — attempting reconnect...");
      if (connectToBLE()) {
        Serial.println("[BLE] Reconnected to robot!");
      } else {
        Serial.println("[BLE] Reconnect failed, retrying in 5s");
      }
    }
  }

  delay(2);
}
