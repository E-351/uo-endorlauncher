using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Avalonia.Controls.Shapes;
using EndorLauncher.Internal;

namespace EndorLauncher.Models;

public enum ServerEnvironment
{
    Live,
    PTR,
}

public class AppSettings : ObservableObject
{
    public const string SettingsFileName = "endorlauncher.json";

    private string _clientPath = @"%LOCALAPPDATA%\Endor Revived\Client\EndorRevived.exe";
    public string ClientPath
    {
        get => _clientPath;
        set => SetProperty(ref _clientPath, value);
    }

    public ObservableCollection<AccountSettings> Accounts { get; set; } = new()
    {
        new() { Username = "", Password = "" },
    };

    public void Save()
    {
        var path = GetFilePath();

        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

        JsonSerializer.Serialize(stream, this, _jsonSerializerOptions);
    }

    private static JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string GetFilePath()
    {
        return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Environment.ProcessPath)!, SettingsFileName);
    }

    public static AppSettings Load()
    {
        var path = GetFilePath();

        if (!System.IO.Path.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            return JsonSerializer.Deserialize<AppSettings>(stream, _jsonSerializerOptions) ?? throw new Exception("Failed to load settings, file is probably corrupted");
        }
        catch (JsonException)
        {
            File.Delete(path);
            return new AppSettings();
        }
    }
}
