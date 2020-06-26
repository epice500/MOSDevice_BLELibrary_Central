/*
 * Dev: Mitchell Ottaway 
 * Date: June 2020
 * Description: Prototype for BLE IMU sensor to be mounted on the back of monitor
 * 
 * TODO: Battery level indicator
 */

 //Import BLE library
#include <ArduinoBLE.h>

//Define Gatt Service
BLEService imuService("E95D0753251D470AA062FA1922DFA9A8");

//Define Gatt Characteristic
BLEByteCharacteristic writeIMUCharacteristic("E95D0753251D470AA062FA1922DFA9A8", BLERead | BLEWrite);

//Pin to use for the LED
const int ledPin = LED_BUILTIN; 

//Set up IMU and BLE
void setup() {
  
  //Use the LED pin as an output
  pinMode(ledPin, OUTPUT); 

  // begin initialization
  if (!BLE.begin()) {
    while (1);
  }

  // set the local name peripheral advertises
  BLE.setLocalName("MOSDeviceService");
  
  // set the UUID for the service this peripheral advertises
  BLE.setAdvertisedService(imuService);

  // add the characteristic to the service
  imuService.addCharacteristic(writeIMUCharacteristic);

  // add service
  BLE.addService(imuService);

  //Connected event handler
  BLE.setEventHandler(BLEConnected, blePeripheralConnectHandler);

  //Disconnected Event handler
  BLE.setEventHandler(BLEDisconnected, blePeripheralDisconnectHandler);

  // assign event handlers for characteristic
 // writeIMUCharacteristic.setEventHandler(BLEWritten, BLECharacteristicEvent);
  
  // set an initial value for the characteristic
 // writeIMUCharacteristic.setValue(0); //TODO: Change to whatever value the IMU is currently at when device is turned on

  // start advertising
  BLE.advertise();

}

//Program loop
void loop() {
  
//Polls for BLE events
BLE.poll(); 

}

void blePeripheralConnectHandler(BLEDevice central) {
  //Turns on LED when connected to central (Computer)
  digitalWrite(ledPin, HIGH);
}

void blePeripheralDisconnectHandler(BLEDevice central) {
  //Turns off LED when disconnected from central (Computer)
  digitalWrite(ledPin, LOW);
}
