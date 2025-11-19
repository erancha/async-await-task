using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AsyncAwaitTask
{
   internal sealed class Program
   {
      private const int TopWordsToDisplay = 10;

      private static readonly IReadOnlyList<string> UrlsToScrape =
            // Enumerable.Repeat("https://github.com/erancha/async-await-task", 10)
            // .Concat
            (new[]
            {
               "https://www.example.com",
               "https://www.iana.org/domains/reserved",
               "https://httpbin.org/html"
            })
            .ToList();

      private static async Task Main(string[] args)
      {
         using var httpClient = new HttpClient();

         var kettleService = new KettleService(httpClient);
         var teaMaker = new TeaMaker(kettleService);

         // Console.WriteLine("=== Tea Making Process ===\n");
         // await teaMaker.MakeTeaAsync();
         // Console.WriteLine("\n=== Tea is ready! ===");

         Console.WriteLine("\n=== Web Scraping ===\n");
         var webScraper = new WebScraper(httpClient, UrlsToScrape);
         var stopwatch = Stopwatch.StartNew();
         var aggregatedResults = await webScraper.ScrapeAndAggregateAsync();
         stopwatch.Stop();

         Console.WriteLine($"Aggregated word counts (top {TopWordsToDisplay}, desc):");
         foreach (var (word, count) in aggregatedResults.Take(TopWordsToDisplay))
         {
            Console.WriteLine($"{word}: {count}");
         }

         Console.WriteLine($"\nTotal elapsed time: {stopwatch.Elapsed.TotalSeconds:F2}s");
      }
   }
}

