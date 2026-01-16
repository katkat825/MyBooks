using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace MyBooks.FileService.Services;

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

    public async Task<string> RefreshAccessTokenAsync(string refreshToken)
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

    // user-facing
    public async Task<Stream?> GetFileStreamAsync(string fileId, string refreshToken)
    {
        var accessToken = await RefreshAccessTokenAsync(refreshToken);
        return await GetFileStreamAsSystemAsync(fileId, accessToken);
    }

    // system-facing: reuse cached access token
    public async Task<Stream?> GetFileStreamAsSystemAsync(string fileId, string accessToken)
    {
        var service = CreateService(accessToken);
        var open = service.Files.Get(fileId);
        open.Fields = "id";
        open.SupportsAllDrives = true;
        await open.ExecuteAsync();
        
        var download = service.Files.Get(fileId);
        download.SupportsAllDrives = true;

        var stream = new MemoryStream();
        await download.DownloadAsync(stream);
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

    public async Task<string> GetOrCreateFolderAsync(string folderName, string parentId, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            throw new ArgumentException("folderName is required", nameof(folderName));

        if (string.IsNullOrWhiteSpace(parentId))
            parentId = "root";

        var accessToken = await RefreshAccessTokenAsync(refreshToken);
        var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName = "MyBooks FileService"
        });

        // 1) Find existing folder(s) under parent
        var listRequest = service.Files.List();
        listRequest.Q = $"'{parentId}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
        listRequest.Fields = "files(id, name)";
        listRequest.PageSize = 100;

        var listResult = await listRequest.ExecuteAsync();

        // prefer exact (case-insensitive) match if multiple
        var existing = listResult.Files
            .FirstOrDefault(f => string.Equals(f.Name, folderName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
            return existing.Id;

        // 2) Create folder
        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { parentId }
        };

        var createRequest = service.Files.Create(folderMetadata);
        createRequest.Fields = "id";
        var created = await createRequest.ExecuteAsync();

        if (!string.IsNullOrEmpty(created?.Id))
            return created.Id;

        // 3) Very rare: if we didn’t get an ID (or a race), try listing again and return the first match
        listResult = await listRequest.ExecuteAsync();
        existing = listResult.Files
            .FirstOrDefault(f => string.Equals(f.Name, folderName, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException("Failed to create or locate the requested Google Drive folder.");

        return existing.Id;
    }

    public async Task DeleteFileAsync(string fileId, string refreshToken)
    {
        var accessToken = await RefreshAccessTokenAsync(refreshToken);
        var service = CreateService(accessToken);
        await service.Files.Delete(fileId).ExecuteAsync();
    }

    public async Task<IList<Google.Apis.Drive.v3.Data.File>> ListFoldersAsync(string parentId, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(parentId))
            parentId = "root";

        var accessToken = await RefreshAccessTokenAsync(refreshToken);
        var service = CreateService(accessToken);

        var request = service.Files.List();
        request.Q = $"'{parentId}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
        request.Fields = "files(id, name)";
        request.PageSize = 100;

        var result = await request.ExecuteAsync();

        return result.Files;
    }

    public async Task<IList<Google.Apis.Drive.v3.Data.File>> ListFilesAsync(string parentId, string refreshToken)
    {
        var accessToken = await RefreshAccessTokenAsync(refreshToken);
        return await ListFilesAsSystemAsync(parentId, accessToken);
    }

    public async Task<IList<Google.Apis.Drive.v3.Data.File>> ListFilesAsSystemAsync(
        string parentId,
        string accessToken)
    {
        if (string.IsNullOrWhiteSpace(parentId))
            parentId = "root";

        var service = CreateService(accessToken);

        var request = service.Files.List();

        // Only return supported book types (PDF + EPUB) and folders
         request.Q = $"'{parentId}' in parents " +
                $"and trashed = false " +
                $"and (mimeType = 'application/pdf' " +
                $"or mimeType = 'application/epub+zip' " +
                $"or mimeType = 'application/vnd.google-apps.folder')";
        request.Fields = "files(id, name, size, mimeType)";
        request.PageSize = 100;

        var result = await request.ExecuteAsync();
        return result.Files;
    }
    
    public async Task<Google.Apis.Drive.v3.Data.File?> GetFileAsync(string fileId, string refreshToken)
    {
        var accessToken = await RefreshAccessTokenAsync(refreshToken);
        var service = CreateService(accessToken);

        var request = service.Files.Get(fileId);
        request.Fields = "id, name, mimeType, size";
        return await request.ExecuteAsync();
    }
}