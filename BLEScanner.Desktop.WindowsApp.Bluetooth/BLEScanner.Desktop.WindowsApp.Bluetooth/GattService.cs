using System;
using System.Collections.Generic;
using System.Text;

namespace BLEScanner.Desktop.WindowsApp.Bluetooth
{
    //Details about the device GATT Service (https://www.bluetooth.com/specifications/gatt/services/)
    public class GattService
    {
        #region Public Properties
        public string Name { get; } //Name of the Gatt device
        public string UniformTypeIdentifier { get; } //Uniform type identifier of the Gatt device which is unique to its service
        public ushort AssignedNumber { get; } //Assigned number is the bluetooth GATT service UUID (Important)
        public string Specification { get; } //Specification of the service
        public string ProfileSpecification { get; } //
        #endregion

        #region Constructor
        public GattService(string name, string uniformTypeIdentifier, ushort assignedNumber, string specification, string profileSpecification)
        {
            Name = name;
            UniformTypeIdentifier = uniformTypeIdentifier;
            AssignedNumber = assignedNumber;
            Specification = specification;
            ProfileSpecification = profileSpecification;
        }
        #endregion


    }
}
