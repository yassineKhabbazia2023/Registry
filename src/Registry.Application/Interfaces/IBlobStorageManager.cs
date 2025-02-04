namespace Application.Interfaces
{
    public interface IBlobStorageManager
    {
        Task<bool> SaveFileAsync(string endpoint, string fileContent);
    }
}
