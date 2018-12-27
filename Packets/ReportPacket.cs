using System;

namespace HID_Demo.Packets
{
    #region Flags

    [Flags]
    public enum BatteryStatus : byte
    {
        Mask = 0b11110000,

        Empty = 0,
        Charging = 1 << 4,
        Critical = 2 << 4,
        Low = 4 << 4,
        Medium = 6 << 4,
        Full = 8 << 4
    }

    [Flags]
    public enum ControllerType : byte
    {
        Mask = 0b00001110,

        Pro = 0,
        JoyCon = 3 << 1
    }

    [Flags]
    public enum ConnectionType : byte
    {
        Mask = 0b00000001,

        Bluetooth = 0,
        Usb = 1,
    }

    [Flags]
    public enum RightButtons : byte
    {
        Y = 1,
        X = 1 << 1,
        B = 1 << 2,
        A = 1 << 3,
        SR = 1 << 4,
        SL = 1 << 5,
        R = 1 << 6,
        ZR = 1 << 7
    }

    [Flags]
    public enum SharedButtons : byte
    {
        Minus = 1,
        Plus = 1 << 1,
        RStick = 1 << 2,
        LStick = 1 << 3,
        Home = 1 << 4,
        Capture = 1 << 5
    }

    [Flags]
    public enum LeftButtons : byte
    {
        Down = 1,
        Up = 1 << 1,
        Right = 1 << 2,
        Left = 1 << 3,
        SR = 1 << 4,
        SL = 1 << 5,
        L = 1 << 6,
        ZL = 1 << 7
    }

    #endregion

    [PacketSize(64)]
    class ReportPacket
    {
        [PacketInfo(0)]
        public byte ReportId { get; set; }
        [PacketInfo(1)]
        public byte Timer { get; set; }

        // Size = 1 byte
        [PacketInfo(2)]
        public BatteryStatus Battery { get; set; }
        [PacketInfo(2)]
        public ControllerType ControllerType { get; set; }
        [PacketInfo(2)]
        public ConnectionType ConnectionType { get; set; }
        //

        [PacketInfo(3)]
        public RightButtons RightButtons { get; set; }
        [PacketInfo(4)]
        public SharedButtons ShareButtons { get; set; }
        [PacketInfo(5)]
        public LeftButtons LeftButtons { get; set; }

        [PacketInfo(6, MaxSize = 3)]
        public byte[] LeftStick { get; set; }
        [PacketInfo(9, MaxSize = 3)]
        public byte[] RightStick { get; set; }

        [PacketInfo(12)]
        public byte Vibration;

        // for x21 ReportID only
        [PacketInfo(13)]
        public byte ACK { get; set; }
        [PacketInfo(14)]
        public byte SubCmdId { get; set; }
        [PacketInfo(15, MaxSize = 35)]
        public byte[] SubCmdData { get; set; }
        //
    }
}
