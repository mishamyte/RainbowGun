using System;
using System.IO;
using System.Security.Cryptography;

namespace RainbowGun
{
    public static class EULA
    {
        public static bool CheckEULA()
        {
            if (!CheckEULAFile())
            {
                return AcceptEULA();
            }
            return GetMD5Hash("EULA.txt").Equals("E0BE3FDC623BE289A19D93284E7E90A0") || AcceptEULA();
        }

        private static bool AcceptEULA()
        {
            ShowEULA();
            Console.WriteLine("Accept - [y], Decline - [n]");
            var acceptString = Console.ReadLine();
            while (acceptString != null && (!acceptString.ToLower().Equals("y") && !acceptString.ToLower().Equals("n")))
            {
                acceptString = Console.ReadLine();
            }
            if (acceptString.ToLower().Equals("n")) return false;
            WriteEULA();
            return true;
        }
        private static bool CheckEULAFile()
        {
            if (File.Exists("EULA.txt"))
            {
                return true;
            }
            return false;
        }

        private static void ShowEULA()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Rainbow Gun License\n");
            Console.ResetColor();
            Console.WriteLine("Copyright (c) 2014-2015 Mykhailo Shevchuk, myte@ukr.net\n");
            Console.WriteLine("1. This software is provided \"as-is,\" without any express or implied warranty. In no event shall the author be held liable for any damages arising from the use of this software.\n");
            Console.WriteLine("2. Authorization must be obtained from the web application owner before using this software.\n");
            Console.WriteLine("3. This software will try to send a lot of requests when working.\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("BY ACCEPTING BELOW, YOU AGREE TO BE BOUND BY THE TERMS AND CONDITIONS OF THIS AGREEMENT.\n");
            Console.ResetColor();
        }

        private static void WriteEULA()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("EULA.txt",false))
                {
                    sw.WriteLine("Rainbow Gun License");
                    sw.WriteLine();
                    sw.WriteLine("Copyright (c) 2014-2015 Mykhailo Shevchuk, myte@ukr.net");
                    sw.WriteLine();
                    sw.WriteLine("1. This software is provided \"as-is,\" without any express or implied warranty. In no event shall the author be held liable for any damages arising from the use of this software.");
                    sw.WriteLine();
                    sw.WriteLine("2. Authorization must be obtained from the web application owner before using this software.");
                    sw.WriteLine();
                    sw.WriteLine("3. This software will try to send a lot of requests when working.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while writing EULA-file");
                Console.WriteLine(ex.Message);
            }
        }

        private static string GetMD5Hash(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                var fileData = new byte[fs.Length];
                fs.Read(fileData, 0, (int)fs.Length);
                byte[] checkSum = md5.ComputeHash(fileData);
                string result = BitConverter.ToString(checkSum).Replace("-", String.Empty);
                return result;
            }
        }
    }
}
