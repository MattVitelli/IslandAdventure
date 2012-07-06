using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Gaia.Core
{
    class ByteConverter
    {
        public static byte[] StructureToByteArray(object obj)
        {
            int Length = Marshal.SizeOf(obj);
            byte[] bytearray = new byte[Length];
            IntPtr ptr = Marshal.AllocHGlobal(Length);
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, bytearray, 0, Length);
            Marshal.FreeHGlobal(ptr);
            return bytearray;
        }

        public static void ByteArrayToStructure(byte[] bytearray, ref object obj)
        {
            int Length = Marshal.SizeOf(obj);
            IntPtr ptr = Marshal.AllocHGlobal(Length);
            Marshal.Copy(bytearray, 0, ptr, Length);
            obj = Marshal.PtrToStructure(ptr, obj.GetType());
            Marshal.FreeHGlobal(ptr);
        }
    }
}
