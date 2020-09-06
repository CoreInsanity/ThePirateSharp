using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThePirateBay;

namespace TPB_Console_client
{
    class Program
    {
        static void Main(string[] args)
        {
            var tpb = new Tpb();
            Console.Title = "TPB Crawler";
            while (true)
            {
                Console.Clear();
                Console.Write("Search query: ");
                var query = new Query(Console.ReadLine());
                query.Category = TorrentCategory.AllGames;
                
                var searchResults = tpb.Search(query);
                Console.Clear();
                searchResults = searchResults.OrderByDescending(r => r.IsVip).ThenByDescending(r => r.IsTrusted).ThenByDescending(r => r.Uploaded).ToList();
                int index = 1;
                foreach (var res in searchResults)
                {
                    if (res.IsVip) Console.ForegroundColor = ConsoleColor.Green;
                    if (res.IsTrusted) Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"[{index}] [{res.Uploaded.ToString("dd-MM-yyyy")}] [{Math.Round((double)res.SizeBytes / 1073741824.0, 2)} GB] {res.Name}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    System.Threading.Thread.Sleep(20);
                    index++;
                }
                Console.ReadKey();
            }
        }
    }
}
