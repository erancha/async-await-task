using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitTask
{
    internal sealed class WebScraper
    {
        private static readonly Regex WordRegex = new(@"\b[\w']+\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ScriptStyleRegex = new(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);

        private readonly HttpClient httpClient;
        private readonly IReadOnlyList<string> urls;

        public WebScraper(HttpClient httpClient, IEnumerable<string> urls)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (urls is null)
            {
                throw new ArgumentNullException(nameof(urls));
            }

            this.urls = urls.Where(url => !string.IsNullOrWhiteSpace(url)).ToList();

            if (this.urls.Count == 0)
            {
                throw new ArgumentException("At least one URL must be provided.", nameof(urls));
            }
        }

        public async Task<IReadOnlyList<(string Word, int Count)>> ScrapeAndAggregateAsync(CancellationToken cancellationToken = default)
        {
            Logger.InfoFor<WebScraper>("ScrapeAndAggregateAsync - START");

            var scrapeTasks = urls.Select(url => ScrapeUrlAsync(url, cancellationToken)).ToArray();
            var results = await Task.WhenAll(scrapeTasks);

            Logger.InfoFor<WebScraper>("ScrapeAndAggregateAsync - All URLs scraped");

            // Sequential aggregation: fine for typical workloads where I/O dominates. For 1000+ URLs or millions of words, consider parallel approaches (see end of file).
            var aggregate = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var result in results)
            {
                foreach (var kvp in result.WordCounts)
                {
                    aggregate[kvp.Key] = aggregate.TryGetValue(kvp.Key, out var existing)
                        ? existing + kvp.Value
                        : kvp.Value;
                }
            }

            var ordered = aggregate
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();

            Logger.InfoFor<WebScraper>("ScrapeAndAggregateAsync - Aggregation complete");

            return ordered;
        }

        private async Task<WordCountResult> ScrapeUrlAsync(string url, CancellationToken cancellationToken)
        {
            Logger.InfoFor<WebScraper>($"ScrapeUrlAsync - START ({url})");

            try
            {
                var content = await httpClient.GetStringAsync(url, cancellationToken);
                var wordCounts = CountWords(content);

                Logger.InfoFor<WebScraper>($"ScrapeUrlAsync - END ({url}) - total words: {wordCounts.Sum(kvp => kvp.Value)}, unique words: {wordCounts.Count}");
                return new WordCountResult(url, wordCounts);
            }
            catch (Exception ex)
            {
                Logger.ErrorFor<WebScraper>($"ScrapeUrlAsync - ERROR ({url})", ex);
                return new WordCountResult(url, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
            }
        }

        private static Dictionary<string, int> CountWords(string content)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var visibleText = ScriptStyleRegex.Replace(content, " ");
            visibleText = HtmlTagRegex.Replace(visibleText, " ");
            visibleText = System.Net.WebUtility.HtmlDecode(visibleText);

            foreach (Match match in WordRegex.Matches(visibleText))
            {
                var word = match.Value.ToLowerInvariant();
                counts[word] = counts.TryGetValue(word, out var current)
                    ? current + 1
                    : 1;
            }

            return counts;
        }

        private sealed record WordCountResult(string Url, IReadOnlyDictionary<string, int> WordCounts);
    }
}

/*
 * AGGREGATION STRATEGY: Sequential vs Parallel
 * =============================================
 * 
 * Current Implementation (Sequential):
 * ------------------------------------
 * The aggregation at line 45-55 uses a sequential foreach loop with a regular Dictionary.
 * This is appropriate for typical workloads because:
 * - HTTP I/O operations (already parallelized via Task.WhenAll) dominate execution time
 * - Dictionary operations are extremely fast (O(1) average case)
 * - Parallel overhead would likely exceed benefits for <1000 URLs
 * 
 * When to Consider Parallel Aggregation:
 * --------------------------------------
 * 1. Very large datasets: 1000+ URLs or millions of unique words per URL
 * 2. Aggregation becomes the bottleneck: when merging dictionaries takes >10% of total time
 * 3. CPU-bound processing: complex operations per word (stemming, normalization, etc.)
 * 
 * Parallel Approaches:
 * -------------------
 * 
 * Option 1: ConcurrentDictionary (Simplest)
 *   var aggregate = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
 *   Parallel.ForEach(results, result =>
 *   {
 *       foreach (var kvp in result.WordCounts)
 *       {
 *           aggregate.AddOrUpdate(kvp.Key, kvp.Value, (key, oldValue) => oldValue + kvp.Value);
 *       }
 *   });
 *   Pros: Simple, thread-safe, good for moderate parallelism
 *   Cons: Some contention on hot keys; overhead for small datasets
 * 
 * Option 2: Partition and Merge (Better for Large Datasets)
 *   var partitions = Partitioner.Create(results, loadBalance: true);
 *   var partitionResults = partitions.AsParallel()
 *       .Select(partition => 
 *       {
 *           var localDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
 *           foreach (var result in partition)
 *           {
 *               foreach (var kvp in result.WordCounts)
 *               {
 *                   localDict[kvp.Key] = localDict.TryGetValue(kvp.Key, out var existing) 
 *                       ? existing + kvp.Value 
 *                       : kvp.Value;
 *               }
 *           }
 *           return localDict;
 *       })
 *       .ToList();
 *   // Final merge (sequential, but smaller)
 *   var aggregate = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
 *   foreach (var partitionDict in partitionResults)
 *   {
 *       foreach (var kvp in partitionDict)
 *       {
 *           aggregate[kvp.Key] = aggregate.TryGetValue(kvp.Key, out var existing) 
 *               ? existing + kvp.Value 
 *               : kvp.Value;
 *       }
 *   }
 *   Pros: Reduces contention, better cache locality, scales well
 *   Cons: More complex, requires final merge step
 * 
 * Option 3: PLINQ with GroupBy (Functional Approach)
 *   var aggregate = results
 *       .AsParallel()
 *       .SelectMany(r => r.WordCounts)
 *       .GroupBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
 *       .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value));
 *   Pros: Concise, leverages PLINQ optimizations
 *   Cons: Creates intermediate collections; may be slower for very large datasets
 * 
 * Performance Recommendation:
 * --------------------------
 * For typical web scraping (10-100 URLs, typical web pages), sequential aggregation is optimal.
 * The bottleneck is network I/O, which is already parallelized. Only consider parallel aggregation
 * when profiling shows it's actually a bottleneck (>10% of total time) or when dealing with
 * extremely large datasets (1000+ URLs or millions of unique words).
 */

