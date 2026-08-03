namespace Application.Interfaces
{
    public interface IBlobStorageManager
    {
        /// <summary>
        /// Stores the content and returns the created blob name.
        /// </summary>
        Task<string> SaveFileAsync(string endpoint, string fileContent);
    }
}
