using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Platform.Storage;
using EndorLauncher.Internal;
using EndorLauncher.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Path = System.IO.Path;

namespace EndorLauncher.UI;

public enum MainWindowMode
{
    Normal,
    Editable,
}

public class MainWindowViewModel : ObservableObject
{
    private AppSettings _settings = new();
    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }
}

public partial class MainWindow : Window
{
    private readonly string[] DirVariables = { "%LOCALAPPDATA%", "%APPDATA%", "%ProgramData%", "%ProgramFiles%", "%ProgramFiles(x86)%", "%USERPROFILE%" };

    public MainWindow()
    {
        DataContext = new MainWindowViewModel()
        {
            Settings = AppSettings.Load(),
        };

        InitializeComponent();
    }

    public MainWindowViewModel Model => (MainWindowViewModel)DataContext!;

    public ServerEnvironment[] ServerEnvironmentOptions { get; } = Enum.GetValues<ServerEnvironment>().ToArray();

    // control
    public void EditSettings()
    {
        if (Mode != MainWindowMode.Normal)
        {
            return;
        }

        Mode = MainWindowMode.Editable;
    }

    public async void CancelEditSettings()
    {
        if (Mode != MainWindowMode.Editable)
        {
            return;
        }

        try
        {
            Model.Settings = AppSettings.Load();
            Mode = MainWindowMode.Normal;
        }
        catch (Exception)
        {
            await MessageBoxManager
                .GetMessageBoxStandard("Error", "Failed to reload settings.", icon: MsBox.Avalonia.Enums.Icon.Error)
                .ShowAsync();
        }
    }

    public async void ConfirmEditSettings()
    {
        if (Mode != MainWindowMode.Editable)
        {
            return;
        }

        try
        {
            Model.Settings.Save();
            Mode = MainWindowMode.Normal;
        }
        catch (Exception)
        {
            await MessageBoxManager
                .GetMessageBoxStandard("Error", "Failed to save settings.", icon: MsBox.Avalonia.Enums.Icon.Error)
                .ShowAsync();
        }
    }

    // normal mode
    public async void LaunchClient(AccountSettings accountSettings)
    {
        if (Mode != MainWindowMode.Normal)
        {
            return;
        }

        try
        {
            var clientPath = Environment.ExpandEnvironmentVariables(Model.Settings.ClientPath);

            var args = new StringBuilder();

            args.Append($@"-username ""{accountSettings.Username}"" ");
            args.Append($@"-password_enc ""{accountSettings.EncryptedPassword}"" ");
            args.Append("-skiploginscreen ");

            switch (ServerEnvironment)
            {
                case ServerEnvironment.Live:
                    args.Append("-ip server.endor-revived.com -port 5000 ");
                    break;
                case ServerEnvironment.PTR:
                    args.Append("-ip server.esqgame.com -port 5000 ");
                    break;
            }

            // trim
            while (args[^1] == ' ')
            {
                args.Length -= 1;
            }

            Process.Start(new ProcessStartInfo()
            {
                FileName = clientPath,
                Arguments = args.ToString(),
                WorkingDirectory = System.IO.Path.GetDirectoryName(clientPath),
            });
        }
        catch (Exception)
        {
            await MessageBoxManager
                .GetMessageBoxStandard("Error", "Failed to launch client. Check whether path is set correctly.", icon: MsBox.Avalonia.Enums.Icon.Error)
                .ShowAsync();
        }
    }

    // editable mode
    public async void SelectClientDirectory()
    {
        var topLevel = TopLevel.GetTopLevel(this);

        IStorageFolder? suggestedStartLocation = null;
        try
        {
            suggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(new Uri(
                System.IO.Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(Model.Settings.ClientPath))!)
            );
        }
        catch (Exception)
        {
            // this is not important
        }

        var paths = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Select client path",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeFilter = new []
            {
                new FilePickerFileType("Endor executable")
                {
                    Patterns = new[]
                    {
                        "EndorRevived.exe",
                    },
                },
                new FilePickerFileType("All files")
                {
                    Patterns = new[]
                    {
                        "*.*"
                    },
                },
            }
        });

        if (paths.Count <= 0)
        {
            return;
        }

        var path = Path.GetFullPath(paths.First().Path.LocalPath);
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var dirVariable in DirVariables)
        {
            var dir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dirVariable));

            if (path.StartsWith(dir))
            {
                path = $"{dirVariable}{path[dir.Length..]}";
                break;
            }
        }

        Model.Settings.ClientPath = path;
    }

    public void CreateProfile()
    {
        if (Mode != MainWindowMode.Editable)
        {
            return;
        }

        var accountSettings = new AccountSettings();

        Model.Settings.Accounts.Add(accountSettings);
    }

    public void ShiftUp(AccountSettings accountSettings)
    {
        if (Mode != MainWindowMode.Editable)
        {
            return;
        }

        var index = Model.Settings.Accounts.IndexOf(accountSettings);
        if (index == -1)
        {
            return;
        }

        if (index <= 0)
        {
            return;
        }

        (Model.Settings.Accounts[index - 1], Model.Settings.Accounts[index]) = (Model.Settings.Accounts[index], Model.Settings.Accounts[index - 1]);
    }

    public void ShiftDown(AccountSettings accountSettings)
    {
        if (Mode != MainWindowMode.Editable)
        {
            return;
        }

        var index = Model.Settings.Accounts.IndexOf(accountSettings);
        if (index >= Model.Settings.Accounts.Count - 1)
        {
            return;
        }

        (Model.Settings.Accounts[index + 1], Model.Settings.Accounts[index]) = (Model.Settings.Accounts[index], Model.Settings.Accounts[index + 1]);
    }

    public void DeleteProfile(AccountSettings accountSettings)
    {
        if (Mode != MainWindowMode.Editable)
        {
            return;
        }

        if (Model.Settings.Accounts.Count <= 1)
        {
            return;
        }

        Model.Settings.Accounts.Remove(accountSettings);
    }

    #region Property: ServerEnvironment

    public static readonly DirectProperty<MainWindow, ServerEnvironment> ServerEnvironmentProperty = AvaloniaProperty.RegisterDirect<MainWindow, ServerEnvironment>(nameof(ServerEnvironment), o => o.ServerEnvironment);

    private ServerEnvironment _serverEnvironment;
    public ServerEnvironment ServerEnvironment
    {
        get => _serverEnvironment;
        private set => SetAndRaise(ServerEnvironmentProperty, ref _serverEnvironment, value);
    }

    #endregion

    #region Property: Mode

    public static readonly DirectProperty<MainWindow, MainWindowMode> ModeProperty = AvaloniaProperty.RegisterDirect<MainWindow, MainWindowMode>(nameof(Mode), o => o.Mode);

    private MainWindowMode _mode;
    public MainWindowMode Mode
    {
        get => _mode;
        private set => SetAndRaise(ModeProperty, ref _mode, value);
    }

    #endregion
}
