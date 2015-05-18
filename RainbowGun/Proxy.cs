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
        private readonly string _filename;
        private readonly string _proxiesType;
        private readonly List<string> _proxies = new List<string>();
        private List<string> _goodProxies = new List<string>();
        private readonly bool _debug;
        private readonly int _connectionTimeout;

        public Proxy(string fnm, string proxiesType = "http", bool dbg = false,int cnt = 15000)
        {
            _filename = fnm;
            _debug = dbg;
            _connectionTimeout = cnt;

            if (proxiesType == "http" || proxiesType == "socks4" || proxiesType == "socks4a" ||
                proxiesType == "socks5")
            {
                this._proxiesType = proxiesType;
            }
        }

        public List<string> GetGoodProxies()
        {
            if (ReadProxies())
            {
                CheckAllProxies();
            }
            return _goodProxies;
        }

        public bool ReadProxies()
        {
            try
            {
                if (File.Exists(_filename))
                {
                    using (var sr = new StreamReader(_filename, Encoding.Default))
                    {
                        int counter = 0;
                        while (!sr.EndOfStream)
                        {
                            string tmp = sr.ReadLine();
                            if (Regex.IsMatch(tmp,
                                @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b:\d{2,5}"))
                            {
                                _proxies.Add(tmp);
                                if (_debug) Console.WriteLine("Good format: {0}",tmp);
                                counter++;
                            }
                            else
                            {
                                if (_debug) Console.WriteLine("Bad format: {0}", tmp);
                            }
                        }
                        if (_debug)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\nReaded {0} proxies!\n",counter);
                            Console.ResetColor();
                        }
                        sr.Close();
                        if (_proxies.Count != 0) return true;                      
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
            _goodProxies = new List<string>();

            var mt = new MultiThreading(_proxies.Count);
            mt.RunForEach(_proxies,CheckProxy);

            while (mt.Working)
            {
                Thread.Sleep(1);
            }
            return _goodProxies.Count;
        }

        private void CheckProxy(string proxyString)
        {
            try
            {
                ProxyClient proxyClient;
                switch (_proxiesType)
                {
                    case "http":
                        proxyClient = HttpProxyClient.Parse(proxyString);
                        break;
                    case "socks4":
                        proxyClient = Socks4ProxyClient.Parse(proxyString);
                        break;
                    case "socks4a":
                        proxyClient = Socks4aProxyClient.Parse(proxyString);
                        break;
                    case "socks5":
                        proxyClient = Socks5ProxyClient.Parse(proxyString);
                        break;
                    default:
                        proxyClient = HttpProxyClient.Parse(proxyString);
                        break;
                }
                proxyClient.ConnectTimeout = _connectionTimeout;
                var tcpClient = proxyClient.CreateConnection("google.com", 80);
                if (tcpClient.Connected)
                {
                    _goodProxies.Add(proxyString);
                    if (_debug) Console.WriteLine("Good proxy: {0}", proxyString);
                }
                else
                {
                    if (_debug) Console.WriteLine("Bad proxy: {0}", proxyString); 
                }
            }
            catch (ProxyException pex)
            {
                if (_debug) Console.WriteLine(pex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<string> ReturnAllProxies()
        {
            return _proxies;
        }

        public List<string> ReturnGoodProxies()
        {
            return _goodProxies;
        }
    }
}
