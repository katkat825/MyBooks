using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyBooks.Common.Helpers;

public static class IntegrationConfigExtensions
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static T? DeserializeConfig<T>(this string configJson) =>
        JsonSerializer.Deserialize<T>(configJson, _options);

    public static string SerializeConfig<T>(this T config) =>
        JsonSerializer.Serialize(config, _options);
}

public class GoogleDriveConfig
{
    public string RefreshToken { get; set; } = string.Empty;
    public string FolderId { get; set; } = string.Empty;
}

public class OneDriveConfig
{
    public string RefreshToken { get; set; } = string.Empty;
    public string DriveId { get; set; } = string.Empty;
    public string RootPath { get; set; } = "/";
}


public enum StorageProvider
{
    Unknown = 0,
    GoogleDrive = 1,
    OneDrive = 2,
    Dropbox = 3,
    Kindle = 4,
    Nook = 5
}