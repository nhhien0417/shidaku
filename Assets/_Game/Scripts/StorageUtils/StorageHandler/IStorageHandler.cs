using System.Collections.Generic;
using System.Threading.Tasks;

namespace StorageUtils.StorageHandler
{
    public interface IStorageHandler<T>
    {
        public Task<string> SaveData(T data, string id);
        public Task<T> GetData(string id);
        public Task<List<T>> GetAllData();
        public void ShowStorageLocation();
    }
    
    public abstract class ErrorMessage
    {
        public const string OutOfMemory = "Out of memory";
        public const string UnknownError = "Unknown error";
    }
}
