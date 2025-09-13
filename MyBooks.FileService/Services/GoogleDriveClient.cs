using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace MyBooks.FileService.Services
{
    public class GoogleDriveClient
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public GoogleDriveClient(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        private DriveService CreateService(string accessToken)
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MyBooks FileService"
            });
        }

        private async Task<string> RefreshAccessTokenAsync(string refreshToken)
        {
            var payload = new Dictionary<string, string>
            {
                {"client_id", _config["GoogleOAuth:ClientId"]},
                {"client_secret", _config["GoogleOAuth:ClientSecret"]},
                {"refresh_token", refreshToken},
                {"grant_type", "refresh_token"}
            };

            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(payload));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(json)!;
            return (string)obj.access_token;
        }

        public async Task<Stream?> GetFileStreamAsync(string fileId, string refreshToken)
        {
            var accessToken = await RefreshAccessTokenAsync(refreshToken);
            var service = CreateService(accessToken);

            var request = service.Files.Get(fileId);
            var stream = new MemoryStream();
            await request.DownloadAsync(stream);
            stream.Position = 0;
            return stream;
        }

        public async Task<string> UploadFileAsync(string fileName, Stream content, string mimeType, string folderId, string refreshToken)
        {
            var accessToken = await RefreshAccessTokenAsync(refreshToken);
            var service = CreateService(accessToken);

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = new List<string> { folderId }
            };

            var request = service.Files.Create(fileMetadata, content, mimeType);
            request.Fields = "id";
            var result = await request.UploadAsync();

            if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
                throw new Exception($"Upload failed: {result.Exception?.Message}");

            return request.ResponseBody.Id;
        }

        public async Task DeleteFileAsync(string fileId, string refreshToken)
        {
            var accessToken = await RefreshAccessTokenAsync(refreshToken);
            var service = CreateService(accessToken);
            await service.Files.Delete(fileId).ExecuteAsync();
        }
    }
}
