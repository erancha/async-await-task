using System.Threading.Tasks;

namespace AsyncAwaitTask
{
    public interface IKettleService
    {
        Task<bool> CheckKettleStatusAsync();
    }
}
