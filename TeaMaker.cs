using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitTask
{
    class TeaMaker
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const int BoilingTimeMs = 3000;
        private static string KettleApiUrl => $"https://httpbin.org/delay/{BoilingTimeMs / 1000}";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Tea Making Process ===\n");
            await MakeTeaAsync();
            Console.WriteLine("\n=== Tea is ready! ===");
        }

        static async Task MakeTeaAsync()
        {
            // Step 1: Start boiling water asynchronously
            Task<string> boilingWaterTask = BoilWaterAsync();
            
            // Step 2: Put tea in cup (synchronous operation)
            PutTeaInCup();
            
            // Step 3: Wait for water to boil and pour it
            string water = await boilingWaterTask;
            PourWaterIntoCup(water);
            
            // Step 4: Serve the cup
            ServeCup();
        }

        static async Task<string> BoilWaterAsync()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] BoilWaterAsync START - Checking kettle status...");
            
            // Simulate boiling water with an async I/O operation, making HTTP call to check "smart kettle" status
            try
            {
                var response = await httpClient.GetStringAsync(KettleApiUrl);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] BoilWaterAsync - Kettle responded");
            }
            catch (Exception)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] BoilWaterAsync - Kettle offline, using timer fallback");
                await Task.Delay(BoilingTimeMs);
            }
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] BoilWaterAsync END");
            return "Boiled Water";
        }

        static void PutTeaInCup()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] PutTeaInCup  -> Tea bag placed in cup");
        }

        static void PourWaterIntoCup(string water)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] PourWaterIntoCup  -> Pouring {water} into cup");
        }

        static void ServeCup()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] ServeCup  -> Cup is ready to serve!");
        }
    }
}
