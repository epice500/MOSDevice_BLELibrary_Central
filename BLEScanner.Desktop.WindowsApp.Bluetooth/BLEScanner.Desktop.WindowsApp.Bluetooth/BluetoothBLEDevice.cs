using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Store.Preview.InstallControl;

namespace BLEScanner.Desktop.WindowsApp.Bluetooth
{
    //Information about the bluetooth low energy device
    public class BluetoothBLEDevice
    {
        #region Public Properties
        //The time of the broad casted message
        public DateTimeOffset Broadcasttime { get; }

        //The address of the device
        public ulong Address { get; }

        //The name of the device
        public string Name { get; }

        //The signal strength in DB
        public short SignalStrengthDB { get; }

        //indicated if connected to device
        public bool Connected { get; }

        //indicates if the device supports pairing
        public bool CanPair { get; }

        //indicates if currently paired to this device
        public bool Paired { get; }

        //the permanent UUID of the device
        public string DeviceId { get; }
        #endregion

        #region Constructor 
        public BluetoothBLEDevice(ulong address, string name, short rssi, DateTimeOffset broadcastTime, bool connected, bool canPair, bool paired, string deviceId)
        {
            Address = address;
            Name = name;
            SignalStrengthDB = rssi;
            Broadcasttime = broadcastTime;
            Connected = connected;
            CanPair = canPair;
            Paired = paired;
            DeviceId = deviceId;
        }
        #endregion

        #region toString
        //Overrides toString Output
        public override string ToString()
        {
            return $"{(string.IsNullOrEmpty(Name) ? "[No Name]" : Name)} [{DeviceId}] ({SignalStrengthDB})";
        }
        #endregion
    }
}
