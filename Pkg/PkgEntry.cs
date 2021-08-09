using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QpangUtils.Pkg
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct PkgEntry
    {
        //public uint unk01;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x30)]
        public string Filename;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x50)]
        public byte[] gap;
        public uint StartLocation;
        public uint EntrySize;
    };

}
