using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;

namespace QpangUtils
{
    unsafe class Program
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint desiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, ulong lpBaseAddress, byte[] lpBuffer, int nSize, out UIntPtr lpNumberOfBytesWritten);


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


        static void Main(string[] args)
        {
            string filename = @"uidata.pkg";
            string pathOld = @"C:\Program Files (x86)\RealFogs\QPang\Ui\";
            string pathNew = @"C:\Program Files (x86)\RealFogs\QPang\Ui\";
            //string pathOld = @"L:\Projects\qpang_server\Modding\pkg\old\";
            //string pathNew = @"L:\Projects\qpang_server\Modding\pkg\new\" + filename.Replace(".pkg","");

            // Unpack all files from the collection
            Dictionary<PkgEntry, byte[]> result = new Dictionary<PkgEntry, byte[]>();

            Console.WriteLine("Unpacking: " + pathOld + filename);
            result = PkgUnpack(File.ReadAllBytes(pathOld + filename), out uint header);

            // Save all files to disk
            foreach(var d in result)
            {
                string filenamed = pathNew + "\\" + d.Key.Filename;

                // create dir if needed
                string[] names = filenamed.Split('\\');
                string name = names[names.Length - 1];
                string dirname = filenamed.Replace(name, "");
                if (!Directory.Exists(dirname))
                    Directory.CreateDirectory(dirname);

                File.WriteAllBytes(filenamed, d.Value);
                Console.WriteLine("Created: " + filenamed);
            }

            Console.WriteLine("Press Enter to start repacking");
            Console.ReadLine();

            byte[] buffResult = PkgPack(result, pathNew, header);
            File.WriteAllBytes(pathNew + "\\" + filename, buffResult);
            Console.WriteLine("Result: " + pathNew + "\\" + filename);

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        // NOTE: for all PKG
        //static void Main(string[] args)
        //{
        //    string pathOld = @"L:\Projects\qpang_server\Modding\pkg\old";
        //    string pathOldUnpacked = @"L:\Projects\qpang_server\Modding\pkg\oldUnpacked";
        //    string pathNew = @"L:\Projects\qpang_server\Modding\pkg\new";
        //    string pathNewUnpacked = @"L:\Projects\qpang_server\Modding\pkg\newUnpacked";

        //    List<string> filenames = new List<string>();

        //    // Collect all files from 'new' dir
        //    AddDirFiles(ref filenames, pathOld);
        //    void AddDirFiles(ref List<string> list, string path)
        //    {
        //        foreach (var f in Directory.GetFiles(path))
        //            list.Add(f);

        //        foreach (var d in Directory.GetDirectories(path))
        //            AddDirFiles(ref list, d);
        //    }

        //    // Unpack all files from the 'new' dir
        //    Dictionary<string, Dictionary<PkgEntry, byte[]>> result = new Dictionary<string, Dictionary<PkgEntry, byte[]>>();
        //    foreach (var f in filenames)
        //    {
        //        Console.WriteLine("Unpacking: " + f);
        //        var data = PkgUnpack(File.ReadAllBytes(f));
        //        result.Add(f, data);
        //    }

        //    // Save all files to 'newUnpacked' dir
        //    Directory.CreateDirectory(pathNewUnpacked);
        //    foreach (var d in result)
        //    {
        //        //if(!Directory.Exists(d.Key))
        //        //    Directory.CreateDirectory(d.Key);
        //        Console.WriteLine("Created: " + d.Key);

        //        string[] pkgNames = d.Key.Split("\\");
        //        string pkgName = pkgNames[pkgNames.Length - 1];
        //        foreach (var di in d.Value)
        //        {
        //            //string[] names = d.Key.Split('\\');
        //            //string name = names[names.Length - 1];
        //            //string filename = d.Key.Replace(name, "") + di.Key.Filename;

        //            string filename = pathNewUnpacked + "\\" + "\\" + di.Key.Filename;

        //            // create dir if needed
        //            string[] names = filename.Split('\\');
        //            string name = names[names.Length - 1];
        //            string dirname = filename.Replace(name, "");
        //            if (!Directory.Exists(dirname))
        //                Directory.CreateDirectory(dirname);

        //            File.WriteAllBytes(filename, di.Value);
        //            Console.WriteLine("Created: " + filename);
        //        }

        //    }


        //    foreach (var f in filenames)
        //    {
        //        Console.WriteLine(f.Replace(pathOld, pathNew));
        //    }

        //    Console.WriteLine("Done!");
        //    Console.ReadLine();
        //}

        static void PackUnpack(byte[] buffer)
        {
            //long dirs = 0;
            //long files = 0;
            //for(int i = 0; i < f)
        }

        static Dictionary<PkgEntry, byte[]> PkgUnpack(byte[] buffer, out uint header)
        {
            Dictionary<PkgEntry, byte[]> result = new Dictionary<PkgEntry, byte[]>();
            uint something = BitConverter.ToUInt32(buffer, 0);

            List<PkgEntry> pkgs = new List<PkgEntry>();

            header = BitConverter.ToUInt32(buffer, 0); // unk header?

            int i = 0;
            uint byteCount = 0;
            while(true)
            {
                byte[] entry = new byte[0x88];
                Array.Copy(buffer, 0x04+(entry.Length * i), entry, 0, entry.Length); // offset by 4 (file header?)

                byte[] unpackedEntry = PkgDecode(entry);

                //PkgEntry pkg = GetObject<PkgEntry>(PkgDecode(entry));

                PkgEntry pkg = new PkgEntry()
                {
                    Filename = Encoding.ASCII.GetString(unpackedEntry).Split('\x00')[0],
                    StartLocation = BitConverter.ToUInt32(unpackedEntry, 0x80),
                    EntrySize = BitConverter.ToUInt32(unpackedEntry, 0x84),
                };

                byteCount += pkg.EntrySize + 0x88;

                if (byteCount > buffer.Length - 4)
                    break;

                if (pkg.Filename == "")
                    break;

                pkgs.Add(pkg);
                i++;
            }

            foreach(var pkg in pkgs)
            {
                byte[] contentBuf = new byte[pkg.EntrySize];
                Array.Copy(buffer, 4 + pkg.StartLocation, contentBuf, 0, contentBuf.Length); // offset by 4 (entry content header?)
                byte[] contentBuff = PkgDecode(contentBuf);
                result.Add(pkg, contentBuff);
            }

            return result;
        }

        static byte[] PkgPack(Dictionary<PkgEntry, byte[]> data, string rootpath, uint header)
        {
            List<byte> result = new List<byte>();
            List<byte[]> contents = new List<byte[]>();
            List<PkgEntry> entries = new List<PkgEntry>();
            List<PkgEntry> updatedEntries = new List<PkgEntry>();

            // write file header
            result.AddRange(BitConverter.GetBytes(header));

            // pack contents
            foreach(var d in data)
            {
                string filename = rootpath + "\\" + d.Key.Filename;
                Console.WriteLine("Packing: " + filename);

                byte[] content = PkgEncode(File.ReadAllBytes(filename));
                contents.Add(content);
                entries.Add(d.Key);
            }

            // update entrties
            int contentsOffset = 0;
            for (int i = 0; i < contents.Count; i++)
            {
                int entriesOffset = 4 + entries.Count * 0x88; // entries
                PkgEntry pkg = new PkgEntry()
                {
                    Filename = entries[i].Filename,
                    StartLocation = (uint)(entriesOffset + contentsOffset),
                    EntrySize = (uint)contents[i].Length
                };
                updatedEntries.Add(pkg);
                contentsOffset += contents[i].Length + 4;
            }

            // write updatedEntries to buffer
            for(int i = 0; i < updatedEntries.Count; i++)
            {
                byte[] entry = GetBytes(updatedEntries[i]);
                byte[] packedEntry = PkgEncode(entry);
                result.AddRange(packedEntry);
            }

            // write contents to buffer
            for (int i = 0; i < contents.Count; i++)
            {
                result.AddRange(BitConverter.GetBytes((uint)contents[i].Length));
                result.AddRange(contents[i]);
            }

            return result.ToArray();
        }


        static byte[] PkgDecode(byte[] bin)
        {
            int size = bin.Length;
            byte[] bout = new byte[size];
            //byte[] bin; // edi
            int index; // ecx
            int v5; // edi
            int index_2; // eax
            int v7; // esi
            int in2;

            if (size / 4 > 0)
            {
                v5 = (size / 4) * 4; // Takes chunks of 4 bbytes
                for(int i = 0; i < v5; i+=4)
                {
                    // Read and NOT
                    uint start = ~BitConverter.ToUInt32(bin, i);

                    // Swap
                    uint right = (start >> 19);
                    right &= 0x00001FFF; // ditch overflow
                    uint left = (start << 13);
                    //left &= 0xFFFFE000;
                    var bytes = BitConverter.GetBytes(left | right);

                    // Store DWORD
                    for (int j = 0; j < 4; j++)
                        bout[i + j] = bytes[j];
                }
            }
            // NOTE: fixed the last few bytes after 4th
            for (int i = ((size / 4)*4); i < size; i++)
                bout[i] = bin[i]; //*(byte*)index_2 = *((byte*)bin + index_2 - out);

            return bout;
        }

        static byte[] PkgEncode(byte[] bin)
        {
            int size = bin.Length;
            byte[] bout = new byte[size];
            //byte[] bin; // edi
            int index; // ecx
            int v5; // edi
            int index_2; // eax
            int v7; // esi
            int in2;

            if (size / 4 > 0)
            {
                v5 = (size / 4) * 4; // Takes chunks of 4 bbytes
                for (int i = 0; i < v5; i += 4)
                {
                    // Read
                    uint start = BitConverter.ToUInt32(bin, i);

                    // Swap
                    uint right = start & 0x00001FFF; // grab low size
                    right <<= 19; // move to the high side
                    uint left = start & 0xFFFFE000; // grab high size
                    left >>= 13; // move to the low size
                    uint result = right | left; // OR togheter
                    result = ~result; // bitflip with a not
                    var bytes = BitConverter.GetBytes(result);

                    // Store DWORD
                    for (int j = 0; j < 4; j++)
                        bout[i + j] = bytes[j];
                }
            }
            // NOTE: fixed the last few bytes after 4th
            for (int i = ((size / 4) * 4); i < size; i++)
                bout[i] = bin[i]; //*(byte*)index_2 = *((byte*)bin + index_2 - out);

            return bout;
        }

        //  Marshalling
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
