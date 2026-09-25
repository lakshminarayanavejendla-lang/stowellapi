namespace StowellCoAPI.Services
{
    public class FolderProxyService
    {
        private readonly HttpClient _http;

        public FolderProxyService(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("PnPWorker");
        }

        public async Task CreateFolderAsync(string folderName)
        {
            await _http.PostAsJsonAsync(
                "api/sharepoint/create-folder",
                new { folderName });
        }
    }
}
