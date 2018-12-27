//Supported types:
//byte, ushort, uint, ulong.
//Supported Arrays for types above, flags based on types above.
//Several flags can be parsed from one start index.
//When flag parsing, bit mask apply from enum value with name 'Mask' if exists

//
//!!!IMPORTANT!!!
//

//This sources was made for personal using and can be unstable and with bad code practise.

using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace HID_Demo
{
    class PacketInfoAttribute : Attribute
    {
        public int StartIndex { get; }
        public int MaxSize { get; set; } = 0;
        public bool IsLittleEndian { get; set; } = false;

        public PacketInfoAttribute(int startIndex)
        {
            StartIndex = startIndex;
        }
    }

    class PacketSizeAttribute : Attribute
    {
        public int Size { get; }
        public PacketSizeAttribute(int size)
        {
            Size = size;
        }
    }

    public class PacketConstructorException : Exception
    {
        public PacketConstructorException(string message) : base(message) { }
    }

    public class PacketConstructorTypeException : PacketConstructorException
    {
        public PacketConstructorTypeException(string propName, string objType)
            : base($"Property {propName} from type {objType} has unsupported type for packet parsing")
        {

        }
    }

    public class PacketConstructorArrayException : PacketConstructorException
    {
        public PacketConstructorArrayException(string propName, string objType)
            : base($"Property {propName} from type {objType} should have non-zero MaxSize value")
        {

        }
    }

    static class PacketConstructor
    {
        private static bool IsSupportedType(Type type)
        {
            var checkedType = type.IsArray
                ? type.GetElementType()
                : type.IsEnum
                    ? type.GetEnumUnderlyingType()
                    : type;

            switch (Type.GetTypeCode(checkedType))
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
        private static byte[] GetNumericBytes(object value, bool isLittleEndian)
        {
            byte[] result = null;
            var type = value.GetType();
            var checkedType = type.IsEnum
                    ? type.GetEnumUnderlyingType()
                    : type;

            switch (Type.GetTypeCode(checkedType))
            {
                case TypeCode.Byte:
                    result = new[] { (byte)value };
                    break;
                case TypeCode.UInt16:
                    result = BitConverter.GetBytes((ushort)value);
                    break;
                case TypeCode.UInt32:
                    result = BitConverter.GetBytes((uint)value);
                    break;
                case TypeCode.UInt64:
                    result = BitConverter.GetBytes((ulong)value);
                    break;
            }

            if (result != null && BitConverter.IsLittleEndian != isLittleEndian)
            {
                result = result.Reverse().ToArray();
            }

            return result;
        }
        private static object GetNumericValue(byte[] valueArray, Type valueType, bool isLittleEndian)
        {
            if (isLittleEndian != BitConverter.IsLittleEndian)
            {
                valueArray = valueArray.Reverse().ToArray();
            }

            var checkedType = valueType.IsEnum
                ? valueType.GetEnumUnderlyingType()
                : valueType;

            var maskIndex = valueType.IsEnum
                ? valueType.GetEnumNames().ToList().IndexOf("Mask")
                : -1;

            var maskValue = maskIndex >= 0
                ? valueType.GetEnumValues().GetValue(maskIndex)
                : null;

            switch (Type.GetTypeCode(checkedType))
            {
                case TypeCode.Byte:
                    return (byte)(maskValue != null ? valueArray[0] & (byte)maskValue : valueArray[0]);
                case TypeCode.UInt16:
                    return (ushort)(maskValue != null ? BitConverter.ToUInt16(valueArray, 0) & (ushort)maskValue : BitConverter.ToUInt16(valueArray, 0));
                case TypeCode.UInt32:
                    return (uint)(maskValue != null ? BitConverter.ToUInt32(valueArray, 0) & (uint)maskValue : BitConverter.ToUInt32(valueArray, 0));
                case TypeCode.UInt64:
                    return (ulong)(maskValue != null ? BitConverter.ToUInt64(valueArray, 0) & (ulong)maskValue : BitConverter.ToUInt64(valueArray, 0));
                default:
                    return default(ValueType);
            }
        }
        private static byte[] GetBytes(object value, bool isLittleEndian)
        {
            if (value == null)
            {
                return new byte[1];
            }

            return (value is Array valueArray)
                ? valueArray.Cast<object>().SelectMany(val => GetNumericBytes(val, isLittleEndian)).ToArray()
                : GetNumericBytes(value, isLittleEndian);
        }
        private static object GetValue(byte[] packet, Type valueType, int startIndex, int maxSize, bool isLittleEndian)
        {
            if (valueType.IsArray)
            {
                var elementType = valueType.GetElementType();
                var elementSize = Marshal.SizeOf(elementType);

                var valueArray = new object[maxSize];
                for (var i = 0; i < valueArray.Length; i++)
                {
                    var elementArray = new byte[elementSize];
                    Array.Copy(packet, startIndex + i * elementSize, elementArray, 0, elementSize);
                    var numericValue = GetNumericValue(elementArray, elementType, isLittleEndian);
                    valueArray[i] = numericValue;
                }

                var result = Array.CreateInstance(elementType, valueArray.Length);
                Array.Copy(valueArray, result, valueArray.Length);
                return result;
            }
            else
            {
                var valueSize = Marshal.SizeOf(valueType.IsEnum ? valueType.GetEnumUnderlyingType() : valueType);
                var valueArray = new byte[valueSize];
                Array.Copy(packet, startIndex, valueArray, 0, valueSize);

                return GetNumericValue(valueArray, valueType, isLittleEndian);
            }
        }
        private static int SizeOfValue(object value)
        {
            var result = 0;
            var type = value.GetType();
            var elType = type.GetElementType();
            if (value is Array valueArray && elType != null)
            {
                result = valueArray.Length * Marshal.SizeOf(elType);
            }
            else
            {
                result = Marshal.SizeOf(type);
            }

            return result;
        }

        public static byte[] BuildPacket<T>(T packetObj)
        {
            var packetSize = ((PacketSizeAttribute)packetObj.GetType().GetCustomAttributes(false).FirstOrDefault(attr => attr is PacketSizeAttribute))?.Size ?? -1;
            var properties = packetObj.GetType().GetProperties()
                .Select(prop => new
                {
                    Property = prop,
                    PacketInfo = prop.GetCustomAttributes(false).Where(attr => attr is PacketInfoAttribute).Cast<PacketInfoAttribute>().FirstOrDefault()
                })
                .Where(propObj => propObj.PacketInfo != null).ToList();

            foreach (var prop in properties)
            {
                if (!IsSupportedType(prop.Property.PropertyType))
                {
                    throw new PacketConstructorTypeException(prop.Property.Name, packetObj.GetType().Name);
                }
            }

            if (properties.Count == 0)
            {
                return null;
            }

            if (packetSize == -1)
            {
                var lastDataProp = properties.OrderByDescending(propObj => propObj.PacketInfo.StartIndex).First();
                packetSize = lastDataProp.PacketInfo.StartIndex + SizeOfValue(lastDataProp.Property.GetValue(packetObj));
            }

            var result = new byte[packetSize];
            foreach (var property in properties)
            {
                var propBytesArray = GetBytes(property.Property.GetValue(packetObj), property.PacketInfo.IsLittleEndian);
                if (property.Property.GetType().IsEnum)
                {
                    var offset = property.PacketInfo.StartIndex;
                    for (var i = 0; i < propBytesArray.Length; i++)
                    {
                        result[offset + i] |= propBytesArray[i];
                    }
                }
                else
                {
                    Array.Copy(propBytesArray, 0, result, property.PacketInfo.StartIndex, propBytesArray.Length);
                }
            }

            return result;
        }
        public static T BuildObject<T>(byte[] packet)
            where T : new()
        {
            var result = new T();
            var properties = result.GetType().GetProperties()
                .Select(prop => new
                {
                    Property = prop,
                    PacketInfo = prop.GetCustomAttributes(false).Where(attr => attr is PacketInfoAttribute).Cast<PacketInfoAttribute>().FirstOrDefault()
                })
                .Where(propObj => propObj.PacketInfo != null).ToList();

            foreach (var prop in properties)
            {
                if (!IsSupportedType(prop.Property.PropertyType))
                {
                    throw new PacketConstructorTypeException(prop.Property.Name, result.GetType().Name);
                }

                if (prop.Property.PropertyType.IsArray && prop.PacketInfo.MaxSize <= 0)
                {
                    throw new PacketConstructorArrayException(prop.Property.Name, result.GetType().Name);
                }

                var propValue = GetValue(packet, prop.Property.PropertyType, prop.PacketInfo.StartIndex, prop.PacketInfo.MaxSize, prop.PacketInfo.IsLittleEndian);
                prop.Property.SetValue(result, propValue);
            }

            return result;
        }
    }
}
