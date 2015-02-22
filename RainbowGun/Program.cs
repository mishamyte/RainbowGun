using System;
using System.IO;
using System.Reflection;

namespace RainbowGun
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += AppDomain_AssemblyResolve; //load xNet from resources

            if (EULA.CheckEULA())
            {
                if (args.Length >= 3)
                {
                    try
                    {
                        RainbowGun rg = null;
                        switch (args.Length)
                        {
                            case 3:
                                rg = new RainbowGun(args[0], args[1], int.Parse(args[2]));
                                break;
                            case 4:
                                rg = new RainbowGun(args[0], args[1], int.Parse(args[2]));
                                rg.SetProxiesTimeout(int.Parse(args[3]));
                                break;
                            case 5:
                                rg = new RainbowGun(args[0], args[1], int.Parse(args[2]), args[4]);
                                rg.SetProxiesTimeout(int.Parse(args[3]));
                                break;
                            case 6:
                                rg = new RainbowGun(args[0], args[1], int.Parse(args[2]), args[4], int.Parse(args[5]));
                                rg.SetProxiesTimeout(int.Parse(args[3]));
                                break;
                            case 7:
                                rg = new RainbowGun(args[0], args[1], int.Parse(args[2]), args[4], int.Parse(args[5]),
                                    bool.Parse(args[6]));
                                rg.SetProxiesTimeout(int.Parse(args[3]));
                                break;
                            case 8:
                                rg = new RainbowGun(args[0], args[1], int.Parse(args[2]), args[4], int.Parse(args[5]),
                                    bool.Parse(args[6]), bool.Parse(args[7]));
                                rg.SetProxiesTimeout(int.Parse(args[3]));
                                break;
                            default:
                                Help();
                                break;
                        }
                        if (rg != null)
                        {
                            rg.SetDebug(true);
                            rg.Attack();
                        }
                        else
                        {
                            Console.WriteLine("Error! Read Help!");
                            Console.WriteLine();
                            Help();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine();
                        Help();
                    }
                }
                else Help();

                Console.ReadKey();
            }
        }

        static void Help()
        {
            Console.WriteLine("Использование:");
            Console.WriteLine("\tRainbowGun <адрес цели> <имя файла прокси> <количество потоков>");
            Console.WriteLine("\tRainbowGun <адрес цели> <имя файла прокси> <количество потоков> <таймаут для прокси>");
            Console.WriteLine("\tRainbowGun <адрес цели> <имя файла прокси> <количество потоков> <таймаут для прокси> <тип прокси>");
            Console.WriteLine("\tRainbowGun <адрес цели> <имя файла прокси> <количество потоков> <таймаут для прокси> <тип прокси> <задержка>");
            Console.WriteLine("\tRainbowGun <адрес цели> <имя файла прокси> <количество потоков> <таймаут для прокси> <тип прокси> <задержка> <добавлять рандомную строку>");
            Console.WriteLine("\tRainbowGun <адрес цели> <имя файла прокси> <количество потоков> <таймаут для прокси> <тип прокси> <задержка> <добавлять рандомную строку> <удалять мертвые прокси во время атаки>");
        }

        private static Assembly AppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("xNet"))
            {
                using (var resource = new MemoryStream(Resources.xNet))
                {
                    using (var reader = new BinaryReader(resource))
                    {
                        var buffer = reader.ReadBytes(1024*1024);
                        return Assembly.Load(buffer);
                    }
                }
            }

            return null;
        }

    }
}
