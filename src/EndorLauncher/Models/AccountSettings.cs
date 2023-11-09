using System.ComponentModel;
using System.Text.Json.Serialization;
using EndorLauncher.Internal;

namespace EndorLauncher;

public class AccountSettings : ObservableObject
{
    private string? _username;

    public string? Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string? _password;

    [JsonIgnore]
    public string? Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            OnPropertyChanged(nameof(EncryptedPassword));
        }
    }

    [JsonPropertyName("password")] 
    public string? EncryptedPassword 
    {
        get => string.IsNullOrEmpty(_password) ? null : GuessItsSlightlyBetterThanPlain.Encrypt(_password);
        set => Password = string.IsNullOrEmpty(value) ? null : GuessItsSlightlyBetterThanPlain.Decrypt(value);
    }
}