using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using xNet.Net;
using xNet.Threading;

namespace RainbowGun
{
    public class Proxy
    {
        private string filename;
        private string proxies_type;
        private List<string> proxies = new List<string>();
        private List<string> good_proxies = new List<string>();
        private bool debug;
        private int connection_timeout;

        public Proxy(string fnm, string proxies_type = "http", bool dbg = false,int cnt = 15000)
        {
            filename = fnm;
            debug = dbg;
            connection_timeout = cnt;

            if (proxies_type == "http" || proxies_type == "socks4" || proxies_type == "socks4a" ||
                proxies_type == "socks5")
            {
                this.proxies_type = proxies_type;
            }
        }

        public List<string> GetGoodProxies()
        {
            if (ReadProxies())
            {
                CheckAllProxies();
            }
            return good_proxies;
        }

        public bool ReadProxies()
        {
            try
            {
                if (File.Exists(filename))
                {
                    using (StreamReader sr = new StreamReader(filename, Encoding.Default))
                    {
                        int counter = 0;
                        while (!sr.EndOfStream)
                        {
                            string tmp = sr.ReadLine();
                            if (Regex.IsMatch(tmp,
                                @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b:\d{2,5}"))
                            {
                                proxies.Add(tmp);
                                if (debug) Console.WriteLine("Good format: {0}",tmp);
                                counter++;
                            }
                            else
                            {
                                if (debug) Console.WriteLine("Bad format: {0}", tmp);
                            }
                        }
                        if (debug)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\nReaded {0} proxies!\n",counter);
                            Console.ResetColor();
                        }
                        sr.Close();
                        if (proxies.Count != 0) return true;                      
                    }

                }
                else
                {
                    Console.WriteLine("File doesn't exist");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        public int CheckAllProxies()
        {
            good_proxies = new List<string>();

            MultiThreading mt = new MultiThreading(proxies.Count);
            mt.RunForEach(proxies,CheckProxy);

            while (mt.Working)
            {
                Thread.Sleep(1);
            }
            return good_proxies.Count;
        }

        private void CheckProxy(string proxy_string)
        {
            try
            {
                ProxyClient proxyClient;
                switch (proxies_type)
                {
                    case "http":
                        proxyClient = HttpProxyClient.Parse(proxy_string);
                        break;
                    case "socks4":
                        proxyClient = Socks4ProxyClient.Parse(proxy_string);
                        break;
                    case "socks4a":
                        proxyClient = Socks4aProxyClient.Parse(proxy_string);
                        break;
                    case "socks5":
                        proxyClient = Socks5ProxyClient.Parse(proxy_string);
                        break;
                    default:
                        proxyClient = HttpProxyClient.Parse(proxy_string);
                        break;
                }
                proxyClient.ConnectTimeout = connection_timeout;
                TcpClient tcpClient = proxyClient.CreateConnection("google.com", 80);
                if (tcpClient.Connected)
                {
                    good_proxies.Add(proxy_string);
                    if (debug) Console.WriteLine("Good proxy: {0}", proxy_string);
                }
                else
                {
                    if (debug) Console.WriteLine("Bad proxy: {0}", proxy_string); 
                }
            }
            catch (ProxyException pex)
            {
                if (debug) Console.WriteLine(pex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<string> ReturnAllProxies()
        {
            return proxies;
        }

        public List<string> ReturnGoodProxies()
        {
            return good_proxies;
        }
    }
}
