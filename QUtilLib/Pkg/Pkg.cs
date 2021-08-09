using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QUtilLib.Helpers;

namespace QUtilLib.Pkg
{
    public static class Pkg
    {
        public static Dictionary<PkgEntry, byte[]> PkgUnpack(byte[] buffer, out uint header)
        {
            Dictionary<PkgEntry, byte[]> result = new Dictionary<PkgEntry, byte[]>();
            List<PkgEntry> pkgs = new List<PkgEntry>();

            header = BitConverter.ToUInt32(buffer, 0); // unk header?

            int i = 0;
            uint byteCount = 0;
            while (true)
            {
                byte[] entry = new byte[0x88];
                Array.Copy(buffer, 0x04 + (entry.Length * i), entry, 0, entry.Length); // offset by 4 (file header?)

                byte[] unpackedEntry = PkgDecode(entry);

                PkgEntry pkg = Util.GetObject<PkgEntry>(PkgDecode(entry));

                //PkgEntry pkg = new PkgEntry()
                //{
                //    Filename = Encoding.ASCII.GetString(unpackedEntry).Split('\x00')[0],
                //    StartLocation = BitConverter.ToUInt32(unpackedEntry, 0x80),
                //    EntrySize = BitConverter.ToUInt32(unpackedEntry, 0x84),
                //};

                byteCount += pkg.EntrySize + 0x88;

                if (byteCount > buffer.Length - 4)
                    break;

                if (pkg.Filename == "")
                    break;

                pkgs.Add(pkg);
                i++;
            }

            foreach (var pkg in pkgs)
            {
                byte[] contentBuf = new byte[pkg.EntrySize];
                Array.Copy(buffer, 4 + pkg.StartLocation, contentBuf, 0, contentBuf.Length); // offset by 4 (entry content header?)
                byte[] contentBuff = PkgDecode(contentBuf);
                result.Add(pkg, contentBuff);
            }

            return result;
        }

        public static byte[] PkgPack(Dictionary<string, byte[]> data, string rootpath, uint header)
        {
            List<byte> result = new List<byte>();
            List<byte[]> contents = new List<byte[]>();
            List<string> entries = new List<string>();
            List<PkgEntry> updatedEntries = new List<PkgEntry>();

            // write file header
            result.AddRange(BitConverter.GetBytes(header));

            // pack contents
            foreach (var d in data)
            {
                byte[] content = PkgEncode(d.Value);
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
                    Filename = entries[i],
                    StartLocation = (uint)(entriesOffset + contentsOffset),
                    EntrySize = (uint)contents[i].Length
                };
                updatedEntries.Add(pkg);
                contentsOffset += contents[i].Length + 4;
            }

            // write updatedEntries to buffer
            for (int i = 0; i < updatedEntries.Count; i++)
            {
                byte[] entry =  Util.GetBytes(updatedEntries[i]);
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

        public static byte[] PkgDecode(byte[] bin)
        {
            int size = bin.Length;
            byte[] bout = new byte[size];
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
            for (int i = ((size / 4) * 4); i < size; i++)
                bout[i] = bin[i]; //*(byte*)index_2 = *((byte*)bin + index_2 - out);

            return bout;
        }

        public static byte[] PkgEncode(byte[] bin)
        {
            int size = bin.Length;
            byte[] bout = new byte[size];
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
            for (int i = ((size / 4) * 4); i < size; i++)
                bout[i] = bin[i]; //*(byte*)index_2 = *((byte*)bin + index_2 - out);

            return bout;
        }
    }
}
