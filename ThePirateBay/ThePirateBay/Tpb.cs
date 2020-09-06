using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Globalization;
using CefSharp.OffScreen;
using System.Threading;
using CefSharp;

namespace ThePirateBay
{
    public class Tpb
    {
        private ChromiumWebBrowser ChromiumClient { get; set; }
        private SemaphoreSlim Signal { get; set; } = new SemaphoreSlim(0, 20);

        public Tpb()
        {
            ChromiumClient = new ChromiumWebBrowser();
            for (int i = 0; i < 30; i++)
            {
                if (ChromiumClient.IsBrowserInitialized) break;
                Thread.Sleep(1000);
            }
            if (!ChromiumClient.IsBrowserInitialized) throw new Exception("Chromium client is not initialized");
        }

        public List<Torrent> Search(Query query)
        {
            List<Torrent> result = new List<Torrent>();
            HtmlDocument doc = new HtmlDocument();
            string htmlBody = "";

            Signal.Dispose();
            Signal = new SemaphoreSlim(0, 20);
            ChromiumClient.FrameLoadEnd += new EventHandler<FrameLoadEndEventArgs>(delegate{Signal.Release();Console.WriteLine("Releasing signal"); });
            ChromiumClient.Load(query.TranslateToUrl());
            Signal.Wait();
            htmlBody = ChromiumClient.GetSourceAsync().Result;

            doc.LoadHtml(htmlBody);

            var torrentSection = doc.DocumentNode.Descendants().Where(x => x.Name == "section").First(y => y.Attributes.Any(a => a.Value == "col-center")).Descendants().First(d => d.Attributes.Any(a => a.Value == "torrents"));

            foreach (var torrent in torrentSection.Descendants().Where(d => d.Attributes.Where(a => a.Name == "id").Any(v => v.Value == "st")))
            {
                var torrentModel = new Torrent();
                torrentModel.Name = torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-title"))).InnerText.Replace("&nbsp;", "");
                torrentModel.Uploaded = DateTime.Parse(torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-uploaded"))).InnerText.Replace("&nbsp;", ""));
                torrentModel.Size = torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-size"))).InnerText.Replace("&nbsp;", "");
                torrentModel.Seeds = Convert.ToInt32(torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-seed"))).InnerText.Replace("&nbsp;", ""));
                torrentModel.Leechers = Convert.ToInt32(torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-leech"))).InnerText.Replace("&nbsp;", ""));
                torrentModel.Uled = torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-user"))).InnerText.Replace("&nbsp;", "");
                torrentModel.Magnet = torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-icons"))).Descendants().First(d => d.Name == "a").Attributes.First(a => a.Name == "href").Value;
                torrentModel.IsTrusted = torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-icons"))).Descendants().Any(d => d.Attributes.Any(a => a.Value == "Trusted"));
                torrentModel.IsVip = torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-icons"))).Descendants().Any(d => d.Attributes.Any(a => a.Value == "VIP"));
                torrentModel.CategoryParent = Convert.ToInt32(torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-type"))).Descendants().Where(d => d.Name == "a").ToList()[0].Attributes.First(a => a.Name == "href").Value.Split(':')[1]);
                torrentModel.Category = Convert.ToInt32(torrent.Descendants().First(d => d.Attributes.Where(a => a.Name == "class").Any(v => v.Value.Contains("item-type"))).Descendants().Where(d => d.Name == "a").ToList()[1].Attributes.First(a => a.Name == "href").Value.Split(':')[1]);


                if (torrentModel.Size.Contains("KiB"))
                {
                    torrentModel.SizeBytes = decimal.Parse(torrentModel.Size.Replace("KiB", ""), NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US")) * 1024M;
                }
                else if (torrentModel.Size.Contains("MiB"))
                {
                    torrentModel.SizeBytes = decimal.Parse(torrentModel.Size.Replace("MiB", ""), NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US")) * 1024M * 1024M;
                }
                else if (torrentModel.Size.Contains("GiB"))
                {
                    torrentModel.SizeBytes = decimal.Parse(torrentModel.Size.Replace("GiB", ""), NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US")) * 1024M * 1024M * 1024M;
                }
                else if (torrentModel.Size.Contains("TiB"))
                {
                    torrentModel.SizeBytes = decimal.Parse(torrentModel.Size.Replace("TiB", ""), NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US")) * 1024M * 1024M * 1024M * 1024M;
                }

                result.Add(torrentModel);
            }

            Thread.Sleep(2000);
            return result;
        }
    }
}
