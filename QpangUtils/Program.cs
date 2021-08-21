using QUtilLib.Pkg;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace QPangUtil
{
    unsafe class Program
    {
        static void Banner()
        {
            Console.WriteLine("                                                     \r\n" +
                              "   ,-----.        ,--. ,--.  ,--.  ,--.,--.          \r\n" +
                              "  '  .-.  ',-----.|  | |  |,-'  '-.`--'|  | ,---.    \r\n" +
                              "  |  | |  |'-----'|  | |  |'-.  .-',--.|  |(  .-'    \r\n" +
                              "  '  '-'  '-.     '  '-'  '  |  |  |  ||  |.-'  `)   \r\n" +
                              "   `-----'--'      `-----'   `--'  `--'`--'`----'    \r\n" +
                              "                              ~ AnimeShooter.com     \r\n");
        }

        static void Help()
        {
            Console.WriteLine("Usage: [TYPE] [OPTION] [FILENAME]\n\r" +
                              "\n\r" +
                              "Type:\n\r" +
                              "  PKG \t\t\t.pkg file format\n\r" +
                              // TODO:
                              //"  PACK \t\t\t.pack file format" +
                              //"  MESH \t\t\t.mesh file format" +
                              //"  CONF \t\t\t.conf file format" +
                              //"  DAT \t\t\t.dat file format" +
                              "\n\r" +
                              "\n\r" +
                              "Option:\n\r" +
                              "  -h\t\t\tprint this help\n\r" +
                              "  -o\t\t\toutput directory\n\r" +
                              "  -u\t\t\tunpack\n\r" +
                              "  -p\t\t\tpack\n\r" +
                              "\n\r");
        }

        static void Main(string[] args)
        {
            Banner();

            //ConfToJson(@"L:\Projects\qpang_server\Modding\skill.conf.txt");

            if (args.Length < 3)
            {
                Help();
                return;
            }

            string type = args[0];

            string filename = "";
            string output = Directory.GetCurrentDirectory();

            bool unpack = true;
            int i = 1;
            while(i < args.Length)
            {
                if(i == args.Length-1)
                    filename = args[i];
                else
                    if (args[i].Equals("-h"))
                    {
                        Help(); 
                        return;
                    }   
                    else if(args[i].Equals("-o"))
                    {
                        if (args.Length < i + 1)
                            return;
                        output = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("-u"))
                        unpack = true;
                    else if(args[i].Equals("-p"))
                        unpack = false;
                i++;
            }

            if (type.Equals("PKG"))
                if (unpack)
                    PkaUnpack(filename, output);
                else
                    PkaPack(filename, output);
            //else if (type.Equals("PACK")) // TODO: add to Lib
            //    return;
            else
                Help(); // invalid type
   
            return;
        }

        static void PkaUnpack(string filename, string output)
        {
            string[] split = filename.Split('\\');
            string file = split[split.Length - 1];

            // Unpack all files from the collection
            Dictionary<PkgEntry, byte[]> result = new Dictionary<PkgEntry, byte[]>();

            Console.WriteLine("[-] Unpacking: " + filename);
            result = Pkg.PkgUnpack(File.ReadAllBytes(filename), out uint header); // header is alwats 0x0004ABCD ?

            // Save all files to disk
            foreach (var d in result)
            {
                //string filenamed = output + "\\" + file + "\\" + d.Key.Filename;
                string filenamed = output + "\\" + d.Key.Filename;

                // create dir if needed
                string[] names = filenamed.Split('\\');
                string name = names[names.Length - 1];
                string dirname = filenamed.Replace(name, "");
                if (!Directory.Exists(dirname))
                    Directory.CreateDirectory(dirname);

                File.WriteAllBytes(filenamed, d.Value);
                Console.WriteLine("[+] Created: " + filenamed);
            }
        }

        static void PkaPack(string path, string output)
        {
            string[] split = path.Split('\\');
            string file = split[split.Length - 1];

            // TODO: fix file order or crash!
            List<string> files = new List<string>();
            AddDirFiles(ref files, path);
            void AddDirFiles(ref List<string> list, string path)
            {
                foreach (var f in Directory.GetFiles(path))
                    list.Add(f);

                foreach (var d in Directory.GetDirectories(path))
                    AddDirFiles(ref list, d);
            }

            Dictionary<string, byte[]> content = new Dictionary<string, byte[]>();
            foreach (var f in files)
            {
                content.Add(f.Replace(path + "\\", ""), File.ReadAllBytes(f));
                Console.WriteLine("[-] Packing: " + f);
            }
                

            byte[] buffResult = Pkg.PkgPack(content, output, 0x0004ABCD);
            File.WriteAllBytes(output, buffResult);
            Console.WriteLine("[+] Created: " + output);
        }

        static void ConfToJson(string path) // NOTE: quick tool for converting to JSON
        {
            var lines = File.ReadLines(path);

            List<SkillCard> objects = new List<SkillCard>();
            string objName = "";
            SkillCard current = null;
            Type type;
            bool isReading = false;
            foreach (var line in lines)
            {
                if (line == "SkillCard" && objName == "")
                {
                    objName = line;
                    type = typeof(SkillCard);
                    current = new SkillCard(); ;// (SkillCard)Activator.CreateInstance(type); // eh?
                    isReading = true;
                }
                else if(line == "}" && objName != "")
                {
                    objName = "";
                    objects.Add(current);
                    isReading = false;
                }

                string[] colums = line.Split('\t');
                if(isReading && colums.Length > 3)
                switch(colums[1])
                {
                    case "CodeNm":
                            current.CodeName = colums[3];
                        break;
                    case "ENG_Nm":
                            current.Name = colums[4];
                        break;
                    case "ENG_Desc":
                            current.Description = colums[3];
                        break;
                    case "ItemId":
                            current.ItemId = Convert.ToUInt32(colums[3].Replace(" ", string.Empty));
                        break;
                    case "Rtp":
                            string tp = colums[4].Replace(" ", string.Empty);
                            current.Type = Convert.ToByte(tp);
                        break;
                    case "Skc":
                            string pts = colums[4].Replace(" ", string.Empty);
                            current.Points = Convert.ToByte(pts);
                        break;
                    case "Tgt":
                            string tt = colums[4].Replace(" ", string.Empty);
                            current.TargetType = Convert.ToByte(tt);
                        break;
                    case "Term":
                            string dr = colums[3].Replace(" ", string.Empty);
                            current.Duration = Convert.ToByte(dr);
                        break;
                    case "Texture":
                            string tn = colums[3].Replace(" ", string.Empty);
                            tn = tn.Replace("card_skill_", "");
                            current.Texture = tn + ".png";
                        break;
                }
                    
            }

            string jsons = JsonConvert.SerializeObject(objects);
            File.WriteAllText(path + ".json", jsons);

            Console.WriteLine();


        }
    }
}
