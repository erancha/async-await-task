using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AsyncAwaitTask
{
    public class KettleService : IKettleService
    {
        private readonly HttpClient httpClient;
        private const int BoilingTimeMs = 3000;
        private static string KettleApiUrl => $"https://httpbin.org/delay/{BoilingTimeMs / 1000}";

        public KettleService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<bool> CheckKettleStatusAsync()
        {
            try
            {
                var response = await httpClient.GetStringAsync(KettleApiUrl);
                return true; // Kettle is online
            }
            catch (Exception ex)
            {
                Logger.WarnFor<KettleService>(
                    $"CheckKettleStatusAsync - Kettle offline because request to {KettleApiUrl} failed: {ex.GetType().Name} | {ex.Message}");
                return false; // Kettle is offline
            }
        }
    }
}
