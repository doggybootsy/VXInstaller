using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Windows.UI.ViewManagement;
using System.Text.Json;
using Windows.Graphics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json.Linq;

namespace VXInstaller
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow GetAppWindowForCurrentWindow()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private AppWindow _appwindow;

        public AppWindow m_AppWindow
        {
            get
            {
                if (_appwindow == null)
                {
                    _appwindow = GetAppWindowForCurrentWindow();
                }

                return _appwindow;
            }
        }

        private AppWindowTitleBar _titlebar;

        public AppWindowTitleBar AppWindowTitleBar
        {
            get
            {
                if (_titlebar == null)
                {
                    _titlebar = m_AppWindow.TitleBar;
                }

                return _titlebar;
            }
        }

        public string ApplicationTitle
        {
            get => ApplicationView.GetForCurrentView().Title;
            set => ApplicationView.GetForCurrentView().Title = value;
        }

        public List<string> Disallowed = new() { "Default", "Default User" };

        private Grid ProfileGrid;
        private const string DiscordFolderName = "Discord";

        public MainWindow()
        {
            this.InitializeComponent();

            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // Set the window size to 800x600
            appWindow.Resize(new SizeInt32 { Width = 800, Height = 600 });

            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsMaximizable = false;
                presenter.IsResizable = false;
                presenter.IsMinimizable = true;
            }

            SetTitleBar(AppTitleBar);

            ProfileGrid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            this.Content = ProfileGrid;

            LoadUserProfiles();
        }

        private void LoadUserProfiles()
        {
            ProfileGrid.Children.Clear();
            ProfileGrid.RowDefinitions.Clear();
            ProfileGrid.ColumnDefinitions.Clear();

            ProfileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var userDirectoryPath = @"C:\Users";
            if (Directory.Exists(userDirectoryPath))
            {
                var directories = Directory.GetDirectories(userDirectoryPath);
                Console.WriteLine($"Found {directories.Length} user directories.");

                var row = 0;
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if (!Directory.Exists(Path.Combine(dir, "AppData")) || Disallowed.Exists(x =>
                            x.Equals(dirInfo.Name, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        continue;
                    }

                    var button = new Button
                    {
                        Content = dirInfo.Name,
                        Tag = dirInfo.FullName,
                        Margin = new Thickness(0, 0, 0, 8),
                        Padding = new Thickness(10),
                        Height = 40,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    button.Click += UserProfileButton_Click;

                    ProfileGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    Grid.SetRow(button, row++);
                    Grid.SetColumn(button, 0);
                    ProfileGrid.Children.Add(button);
                }
            }
            else
            {
                Console.WriteLine($"Directory not found: {userDirectoryPath}");
            }
        }


        private void UserProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var userProfilePath = button.Tag.ToString();

            LoadDiscordFolders(userProfilePath);
        }

        private void UserProfileButton_ClickVXInstall(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var discordSelectedPath = button.Tag.ToString();

            InstallVX(discordSelectedPath);
        }

        private void LoadDiscordFolders(string userProfilePath)
        {
            ProfileGrid.Children.Clear();
            ProfileGrid.RowDefinitions.Clear();
            ProfileGrid.ColumnDefinitions.Clear();

            ProfileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var appDataLocalPath = Path.Combine(userProfilePath, "AppData", "Local");
            if (!Directory.Exists(appDataLocalPath)) return;
            var directories = Directory.GetDirectories(appDataLocalPath)
                .Where(d => Path.GetFileName(d).Contains(DiscordFolderName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var row = 0;
            foreach (var dir in directories)
            {
                var folderName = Path.GetFileName(dir);
                var button = new Button
                {
                    Content = folderName,
                    Tag = dir,
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(10),
                    Height = 40,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                button.Click += UserProfileButton_ClickVXInstall;

                ProfileGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                Grid.SetRow(button, row++);
                Grid.SetColumn(button, 0);
                ProfileGrid.Children.Add(button);
            }
        }

        private static List<string> FindDiscordPaths(string profilePath)
        {
            List<string> targetPaths = new List<string>();

            if (!Directory.Exists(profilePath)) return targetPaths;
            var appFolders = Directory.GetDirectories(profilePath)
                .Where(folder => Path.GetFileName(folder).StartsWith("app-"))
                .ToList();

            appFolders.Sort();

            foreach (var appFolder in appFolders)
            {
                var modulesPath = Path.Combine(appFolder, "modules");

                if (!Directory.Exists(modulesPath)) continue;
                var highestVersionFolder = FindHighestVersionFolder(modulesPath, "discord_desktop_core-");

                if (string.IsNullOrEmpty(highestVersionFolder)) continue;
                string targetPath = Path.Combine(modulesPath, highestVersionFolder, "discord_desktop_core");

                if (Directory.Exists(targetPath))
                {
                    targetPaths.Add(targetPath);
                }
            }

            return targetPaths;
        }

        private static string FindHighestVersionFolder(string modulesPath, string prefix)
        {
            var versionFolders = Directory.GetDirectories(modulesPath)
                .Select(Path.GetFileName)
                .Where(name => name.StartsWith(prefix))
                .ToList();

            if (versionFolders.Count == 0)
                return null;

            versionFolders.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));

            return versionFolders.LastOrDefault();
        }

        private async void InstallVX(string discordDir)
        {
            var basePath = Path.Combine(discordDir);
            if (!Directory.Exists(basePath)) return;

            var selectedPath = FindDiscordPaths(basePath).LastOrDefault();
            if (selectedPath == null) return;

            var indexJsPath = Path.Combine(selectedPath, "index.js");
            var content = "require(\"./app.asar\")\nmodule.exports = require('./core.asar');";

            await File.WriteAllTextAsync(indexJsPath, content);

            await DownloadLatestAppAsar(selectedPath);
        }

        private async Task DownloadLatestAppAsar(string discordDir)
        {
            var downloadUrl = await GetLatestReleaseDownloadUrlAsync();
            if (downloadUrl == null)
            {
                Console.WriteLine("Failed to get the download URL.");
                return;
            }

            var downloadPath = Path.Combine(Path.GetTempPath(), "app.asar");

            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(downloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    await using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
            }

            var targetPath = Path.Combine(discordDir, "app.asar");
            File.Move(downloadPath, targetPath);
        }

        private async Task<string> GetLatestReleaseDownloadUrlAsync()
        {
            const string apiUrl = "https://api.github.com/repos/doggybootsy/vx/releases/latest";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            using var response = await httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch release data: {response.StatusCode}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var releaseData = JObject.Parse(jsonResponse);
            var assets = releaseData["assets"] as JArray;

            if (assets == null)
            {
                Console.WriteLine("No assets found in the release data.");
                return null;
            }

            var asset = assets.FirstOrDefault(a =>
                a["name"].ToString().Equals("app.asar", StringComparison.OrdinalIgnoreCase));
            return asset?["browser_download_url"]?.ToString();
        }
    }
}