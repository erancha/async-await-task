using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitTask
{
    class TeaMaker
    {
        private readonly IKettleService kettleService;
        private const int BoilingTimeMs = 3000;

        public TeaMaker(IKettleService kettleService)
        {
            this.kettleService = kettleService;
        }

        static async Task Main(string[] args)
        {
            // Setup dependency injection
            var httpClient = new HttpClient();
            var kettleService = new KettleService(httpClient);
            var teaMaker = new TeaMaker(kettleService);

            Console.WriteLine("=== Tea Making Process ===\n");
            await teaMaker.MakeTeaAsync();
            Console.WriteLine("\n=== Tea is ready! ===");
        }

        async Task MakeTeaAsync()
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

        async Task<string> BoilWaterAsync()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] BoilWaterAsync START - Checking kettle status...");
            
            // Simulate boiling water with an async I/O operation, making HTTP call to check "smart kettle" status
            bool kettleOnline = await kettleService.CheckKettleStatusAsync();
            
            if (kettleOnline)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] BoilWaterAsync - Kettle responded");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] BoilWaterAsync - Kettle offline, using timer fallback");
                await Task.Delay(BoilingTimeMs);
            }
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] BoilWaterAsync END");
            return "Boiled Water";
        }

        void PutTeaInCup()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] PutTeaInCup  -> Tea bag placed in cup");
        }

        void PourWaterIntoCup(string water)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] PourWaterIntoCup  -> Pouring {water} into cup");
        }

        void ServeCup()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [thread #{Thread.CurrentThread.ManagedThreadId}] ServeCup  -> Cup is ready to serve!");
        }
    }
}
