using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Perception;

namespace BLEScanner.Desktop.WindowsApp.Bluetooth
{

    //Class Library for MOS BLE bluetooth
    public class MOSBluetoothLEAdvertisementWatcher
    {
        #region Private Members
        //The bluetooth watcher class
        private readonly BluetoothLEAdvertisementWatcher watcher;

        //List of discovered devices
        private readonly Dictionary<string, BluetoothBLEDevice> DiscoveredDevices_Dic = new Dictionary<string, BluetoothBLEDevice>();

        //The details about the GATT services
        private readonly GattServiceIDs GattServiceIDs;

        //ThreadLock for class
        private object ThreadLock = new object();
        #endregion

        #region Public Properties

        //Indicates if the watcher is listening for devices
        public bool Listening => watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        //Timeout Value removes devices from list if they are not visible after this time
        public int Timeout { get; set; } = 30;

        //Converts to read only list
        public IReadOnlyCollection<BluetoothBLEDevice> DiscoveredDevices //***Note*** Check which region this is in. Its access modifier seems to be off ***fix*** BluetoothBLEDevice class was not declared public...
        {
            get
            {
                //cleanup timeouts
                CleanupTimeouts();

                lock (ThreadLock)
                {
                    return DiscoveredDevices_Dic.Values.ToList().AsReadOnly(); 
                }
            }

        }

        #endregion

        #region Public Events
        //used when the bluetooth watcher stops listening
        public event Action StoppedListening = () => { };

        //used when the bluetooth watcher starts listening
        public event Action StartedListening = () => { };

        //used when new device is discovered
        public event Action <BluetoothBLEDevice> NewDeviceDiscovered = (device) => { };

        //used when a device is discovered
        public event Action<BluetoothBLEDevice> DeviceDiscovered = (device) => { };

        //used when a devices name is changed
        public event Action<BluetoothBLEDevice> DeviceNameChanged = (device) => { };

        //
        public event Action<BluetoothBLEDevice> DeviceTimedOut = (device) => { };
        #endregion

        #region Constructor
        //Constructor
        public MOSBluetoothLEAdvertisementWatcher(GattServiceIDs gattIds)
        {
            GattServiceIDs = gattIds ?? throw new ArgumentNullException(nameof(gattIds)); 

            //bluetooth listener
            watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            //listens for new advertisements
            watcher.Received += WatcherAdvertisementReceivedAsync;

            //listening for when the watcher stops listening
            watcher.Stopped += (watcher, e) =>
            {
                StoppedListening();
            };

        }
        #endregion

        #region Public Methods

        //Listens for advertisements
        public void startListening()
        {
            lock (ThreadLock)
            {
                if (Listening)
                {
                    return;
                }
                watcher.Start();
                StartedListening();
            } 
            //if already listening
            if(Listening == true)
            {
                return;
            }

            //starts the watcher
            watcher.Start();

            //inform listeners of change
            StartedListening();
        }

        //Stops listening for advertisements
        public void stopListening()
        {
            lock (ThreadLock)
            {
                if (!Listening)
                {
                    return;
                }

                //Stops the listener
                watcher.Stop();

                //inform listeners of change
                //StoppedListening(); possibly do not need this line
                DiscoveredDevices_Dic.Clear();
            }
        }

        //Pair to a BLE device using the device ID
        public async Task PairToDeviceAsync(string deviceId)
        {
            using var device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask();

            var deviceInformationCustomPairing = device.DeviceInformation.Pairing.Custom;

            //Null guard
            if (device == null)
            {
                //TODO: Localize
                throw new ArgumentNullException("Failed to get information about the bluetooth device");
            }

            //If devices are already paired
            if (device.DeviceInformation.Pairing.IsPaired)
            {
                return; //does nothing
            }

            //Listen for pairing request (NEW)
            deviceInformationCustomPairing.PairingRequested += (sender, args) =>
            {
                //Log
                Console.WriteLine("Accepting Pairing Request");

                //Accept all
                args.Accept();
            };

            //Try and pair device
            var result = await deviceInformationCustomPairing.PairAsync(DevicePairingKinds.ConfirmOnly).AsTask();

            //Logs results
            //TODO: Remove Console.WriteLine()'s
            Console.WriteLine(result.Status);

            if (result.Status == DevicePairingResultStatus.Paired)
            {
                //TODO: Remove Console.WriteLine()'s
                Console.WriteLine("Pairing Successful");
            }
            else
            {
                //TODO: Remove Console.WriteLine()'s
                Console.WriteLine($"Pairing Failed: {result.Status}");
            }


            /*
            device.DeviceInformation.Pairing.Custom.PairingRequested += (sender, args) =>
            {
                //var pairingKind = args.PairingKind;
                //var pin = args.Pin;

                //log request
                //TODO: Remove Console.WriteLine()'s
                Console.WriteLine("Accepting pairing request");

                args.Accept(); //can enter a pin here
            };
            */

            //TODO Test this
            //var result = await device.DeviceInformation.Pairing.Custom.PairAsync(DevicePairingKinds.ProvidePin).AsTask();

            //try 

        }

        #endregion

        #region Private Methods
        //listens for watcher advertisements
        private async void WatcherAdvertisementReceivedAsync(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            //cleanup timeouts
            CleanupTimeouts();

            //gets bluetooth BLE device info
            var device = await GetBluetoothBLEDeviceAsync(args.BluetoothAddress, args.Timestamp, (ushort) args.RawSignalStrengthInDBm); //***RawSignalStrengthInDBm is temporally being casted to ushort***

            //Null Guard
            if (device == null)
            {
                return;
            }

            //is new discovery
            var newDiscovery = false;
            var existingName = default(string);
            //new discovery check (checks if device already exists in list)
            lock (ThreadLock)
            {
                //check if is new discovery 
                newDiscovery = !DiscoveredDevices_Dic.ContainsKey(device.DeviceId);

                if (!newDiscovery)
                {
                    existingName = DiscoveredDevices_Dic[device.DeviceId].Name;
                }
            }

            //Name change
            var nameChanged = !newDiscovery && !string.IsNullOrEmpty(device.Name) && existingName != device.Name; //If already exists and is not a blank name

            lock (ThreadLock)
            {
                //Adds update to dictionary
                DiscoveredDevices_Dic[device.DeviceId] = device;
            }

            //Inform listener
            DeviceDiscovered(device);

            //Name changed
            if (nameChanged)
            {
                DeviceNameChanged(device);
            }

            //new discovery
            if (newDiscovery)
            {
                //Informs listener
                NewDeviceDiscovered(device);
            }
        }

        //connects to the BLE device and extracts more information from the device. parameter is the bluetooth address of the device to connect to
        private async Task<BluetoothBLEDevice> GetBluetoothBLEDeviceAsync(ulong address, DateTimeOffset broadcastTime, ushort rssi)
        {
            //get bluetooth device info
            using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask();

            //Null guard
            if (device == null)
            {
                return null;
            }

            //Device name
            var name = device.Name;

            //Note this can throw a system exception for failures
            //Get GATT services that are available
            var gatt = await device.GetGattServicesAsync().AsTask();

            //If there are any services
            if (gatt.Status == GattCommunicationStatus.Success)
            {
                //Loop each GATT service
                foreach (var service in gatt.Services)
                {
                    //this id contains the GATT profile assigned number we want ***NEED TO DO***: Get more info and connect
                    var gattProfileId = service.Uuid;
                }
            }
            //returns device id, device address, device broadcast time and device rssi
            return new BluetoothBLEDevice(deviceId: device.DeviceId, address: device.BluetoothAddress, name: device.Name,
                broadcastTime: broadcastTime, rssi: (short) rssi, connected: device.ConnectionStatus == BluetoothConnectionStatus.Connected, //***rssi casted to short***
                canPair: device.DeviceInformation.Pairing.CanPair, paired: device.DeviceInformation.Pairing.IsPaired);
        }
        //Prunes timed out devices
        private void CleanupTimeouts()
        {
            lock (ThreadLock)
            {
                //date time that if less than means the device has timed out 
                var threshold = DateTime.UtcNow - TimeSpan.FromSeconds(Timeout);

                DiscoveredDevices_Dic.Where(f => f.Value.Broadcasttime < threshold).ToList().ForEach(device =>
                {
                    //remove the device
                    DiscoveredDevices_Dic.Remove(device.Key);
                    //inform listeners
                    DeviceTimedOut(device.Value);
                });
            }
        }
        #endregion
    }
}
