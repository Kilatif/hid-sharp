using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HIDInterface;
using HID_Demo.Packets;

namespace HID_Demo
{
    class Program
    {
        /*------------------------------------------------------------------------------------------
         *                      HID Interface class demo code
         *                      
         * This demo code returns the details of all connected HID devices, then selects one of them
         * and connects to it. A synchronous read / write operation is performed and the device is 
         * closed.
         * Two methods are included to show the synchronous and asynchronous operations of this software.
         * 
         * The intention of this code is to provide a demonstration which can be run in a debug
         * environment, and the sample methods can be cut and pasted into your program
         * ----------------------------------------------------------------------------------------*/

        static void Main(string[] args)
        {
            HID_demo demo = new HID_demo();
            
            //call one or other of these methods to demonstrate each type of operation - sync and async
            demo.startAsyncOperation();             
            //demo.useSynchronousOperation();
        }
    }

    public class HID_demo
    {

        // Apologies for the repeated code, however i feel it provides a better demonstration
        // of the functionality of this code.
        public void useSynchronousOperation()
        {
            //Get the details of all connected USB HID devices
            HIDDevice.interfaceDetails[] devices = HIDDevice.getConnectedDevices();

            //Arbitrarily select one of the devices which we found in the previous step
            //record the details of this device to be used in the class constructor
            int selectedDeviceIndex = 2;
            ushort VID = devices[selectedDeviceIndex].VID;
            ushort PID = devices[selectedDeviceIndex].PID;
            int SN = devices[selectedDeviceIndex].serialNumber;
            string devicePath = devices[selectedDeviceIndex].devicePath;

            //create a handle to the device by calling the constructor of the HID class
            //This can be done using either the VID/PID/Serialnumber, or the device path (string) 
            //all of these details are available from the HIDDevice.interfaceDetails[] struct array created above
            //The "false" boolean in the constructor tells the class we only want synchronous operation
            HIDDevice device = new HIDDevice(devicePath, false);
            //OR, the normal usage when you know the VID and PID of the device
            //HIDDevice device = new HIDDevice(VID, PID, (ushort)SN, false);

            //Write some data to the device (the write method throws an exception if the data is longer than the report length
            //specified by the device, this length can be found in the HIDDevice.interfaceDetails struct)
            //byte[] writeData = { 0x00, 0x01, 0x02, 0x03, 0x04 };
            //device.write(writeData);    //Its that easy!!

            //Read some data synchronously from the device. This method blocks the calling thread until the data
            //is returned. This takes 1-20ms for most HID devices

            while (true)
            {
                byte[] readData = device.read();    //again, that easy!
                WriteData(readData);
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            //close the device to release all handles etc
            device.close();
        }

        private void WriteData(byte[] data, int splitCount = -1)
        {
            var splitData = Split(data, splitCount < 0 ? data.Length : splitCount);
            Console.WriteLine(string.Join("\n", splitData.Select(arr => $"{string.Join("|", arr.Select(v => Convert.ToString(v, 16).PadLeft(2, '-')).ToArray())}").ToArray()));
            Console.WriteLine();

           // var x = BitConverter.ToInt16(new byte[] {data[15], data[14]}, 0);
            //var y = BitConverter.ToInt16(new byte[] {10 , 0 }, 0);

            //Console.WriteLine("{0} : {1}, {2}", x, data[14], data[15]);
        }

        private T[][] Split<T>(T[] data, int arrayCount)
        {
            var result = new T[(int)Math.Ceiling((decimal)data.Length / arrayCount)][];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new T[arrayCount];
                for (var j = 0; j < arrayCount; j++)
                {
                    var index = i * arrayCount + j;
                    result[i][j] = index >= data.Length ? default(T) : data[index];
                }
            }

            return result;
        }

        private byte[] _curMessage = new byte[32];
        private void Reciver(byte[] buffer, int joyConId)
        {
            lock (_curMessage)
            {
                Array.Copy(buffer, 0, _curMessage, 16 * joyConId, 16);
                Console.SetCursorPosition(0, 0);
                WriteData(_curMessage, 16);
            }
        }

        public void startAsyncOperation()
        {
            //Get the details of all connected USB HID devices
            HIDDevice.interfaceDetails[] devices = HIDDevice.getConnectedDevices();

            var joyConsDevices = devices.Where(dev => dev.manufacturer.Contains("Sony") ||
                                                      dev.product.Contains("Xbox") ||
                                                      dev.manufacturer.Contains("Nintendo"))
                .OrderBy(dev => dev.PID).Select(dev => new HIDDevice(dev.devicePath, true)).ToList();

            for (var i = 0; i < joyConsDevices.Count; i++)
            {
                var ii = i;
                joyConsDevices[i].dataReceived += buf => Reciver(buf, ii);
            }

            Console.ReadKey();

            foreach (var device in joyConsDevices)
            {
                var output = new byte[49];
                var buffer = new byte[] { 0x01, 0x01, 0x00, 0x01, 0x40, 0x40, 0x00, 0x01, 0x40, 0x40, 0x03, 0x30 };
                Array.Copy(buffer, output, buffer.Length);

                device.write(output);
            }
            
            Console.ReadKey();

            //close the device to release all handles etc
            foreach (var device in joyConsDevices)
            {
                device.close();
            }
        }
    }
}
