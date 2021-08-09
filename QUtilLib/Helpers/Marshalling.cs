using System;
using System.Runtime.InteropServices;

namespace QUtilLib.Helpers
{
    public static class Util
    {
        public static T GetObject<T>(byte[] data)
        {
            object val = null;
            var type = typeof(T);
            var size = Marshal.SizeOf(type);
            var buf = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, buf, data.Length);
            val = Marshal.PtrToStructure(buf, type);
            Marshal.FreeHGlobal(buf);
            return (T)val;
        }

        public static byte[] GetBytes(object obj)
        {
            var type = obj.GetType();
            var size = Marshal.SizeOf(type);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }
}
