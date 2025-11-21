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

        public async Task MakeTeaAsync()
        {
            Logger.InfoFor<TeaMaker>("MakeTeaAsync - START");

            // Step 1: Start boiling water asynchronously
            Task<string> boilingWaterTask = BoilWaterAsync();

            // Sample: offload CPU-bound snack prep work to Task.Run
            Task snackPreparationTask = Task.Run(() =>
            {
                Logger.InfoFor<TeaMaker>("Task.Run -> Preparing snacks (CPU-bound work)...");
                Thread.Sleep(20000);
                Logger.InfoFor<TeaMaker>("Task.Run -> Snacks ready!");
            });

            // Step 2: Put tea in cup (synchronous operation)
            PutTeaInCup();

            // Step 3: Wait for water to boil and pour it
            Logger.InfoFor<TeaMaker>("MakeTeaAsync - before await boilingWaterTask;");
            string water = await boilingWaterTask;
            PourWaterIntoCup(water);

            // Step 4: Ensure snacks are ready
            await snackPreparationTask;

            // Step 5: Final - Serve the cup
            ServeCup();
        }

        async Task<string> BoilWaterAsync()
        {
            Logger.InfoFor<TeaMaker>("BoilWaterAsync START - Checking kettle status...");

            // Simulate boiling water with an async I/O operation, making HTTP call to check "smart kettle" status
            bool kettleOnline = await kettleService.CheckKettleStatusAsync();

            if (kettleOnline)
            {
                Logger.InfoFor<TeaMaker>("BoilWaterAsync - Kettle responded");
            }
            else
            {
                Logger.WarnFor<TeaMaker>("BoilWaterAsync - Kettle offline, using timer fallback");
                await Task.Delay(BoilingTimeMs);
            }

            Logger.InfoFor<TeaMaker>("BoilWaterAsync END");
            return "Boiled Water";
        }

        void PutTeaInCup()
        {
            Logger.InfoFor<TeaMaker>("PutTeaInCup  -> Tea bag placed in cup");
        }

        void PourWaterIntoCup(string water)
        {
            Logger.InfoFor<TeaMaker>($"PourWaterIntoCup  -> Pouring {water} into cup");
        }

        void ServeCup()
        {
            Logger.InfoFor<TeaMaker>("ServeCup  -> Cup is ready to serve!");
        }
    }
}
