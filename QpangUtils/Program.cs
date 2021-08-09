using QUtilLib.Pkg;
using System;
using System.Collections.Generic;
using System.IO;

namespace QPangUtil
{
    unsafe class Program
    {
        static void Main(string[] args)
        {
            string filename = @"uidata.pkg";
            string pathOld = @"C:\Program Files (x86)\RealFogs\QPang\Ui\";
            string pathNew = @"C:\Program Files (x86)\RealFogs\QPang\Ui\";

            // Unpack all files from the collection
            Dictionary<PkgEntry, byte[]> result = new Dictionary<PkgEntry, byte[]>();

            Console.WriteLine("Unpacking: " + pathOld + filename);
            result = Pkg.PkgUnpack(File.ReadAllBytes(pathOld + filename), out uint header);

            // Save all files to disk
            foreach (var d in result)
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

            byte[] buffResult = Pkg.PkgPack(result, pathNew, header);
            File.WriteAllBytes(pathNew + "\\" + filename, buffResult);
            Console.WriteLine("Result: " + pathNew + "\\" + filename);

            Console.WriteLine("Done!");
            Console.ReadLine();
        }


    }
}
