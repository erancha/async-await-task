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

         var stopwatch = Stopwatch.StartNew();

         var kettleService = new KettleService(httpClient);
         var teaMaker = new TeaMaker(kettleService);

         Logger.Info("=== Tea Making Process ===", nameof(Program));
         await teaMaker.MakeTeaAsync();
         stopwatch.Stop();
         // ------------------------------------------------------------------------------------------------

         // Logger.Info("=== Web Scraping ===", nameof(Program));
         // var webScraper = new WebScraper(httpClient, UrlsToScrape);
         // var aggregatedResults = await webScraper.ScrapeAndAggregateAsync();
         // stopwatch.Stop();

         // Logger.Info($"Aggregated word counts (top {TopWordsToDisplay}, desc):", nameof(Program));
         // foreach (var (word, count) in aggregatedResults.Take(TopWordsToDisplay))
         // {
         //    Logger.Info($"{word}: {count}", nameof(Program));
         // }
         // ------------------------------------------------------------------------------------------------

         // Logger.Info("=== Kafka Async Pipeline ===", nameof(Program));
         // var kafkaPipeline = new KafkaPipeline();
         // await kafkaPipeline.RunAsync();

         // stopwatch.Stop();

         Logger.Info($"Total elapsed time: {stopwatch.Elapsed.TotalSeconds:F2}s", nameof(Program));
      }
   }
}

