using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media.Imaging;
using Newtonsoft.Json.Linq;
using static VXInstaller.Discord;

namespace VXInstaller
{
    public class DiscordReleaseButton
    {
        public DiscordReleaseButton(Release release)
        {
            var path = GetDiscordPath(release);
 
            Release = release;

            Button = new()
            {
                IsEnabled = path is not null,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Tag = path?.Resources
            };

            ReleaseStuff = path;
            
            Grid grid = new();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Fixed width for the Image
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });  // Small space between Image and TextBlock
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // TextBlock stretches in this column

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var ImageSize = 60;

            // Create the Image
            Image = new()
            {
                Source = new BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.png")),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = ImageSize,
                Height = ImageSize
            };
            Grid.SetColumn(Image, 0);
            Grid.SetRow(Image, 0);

            // Create the TextBlock
            TextBlock = new()
            {
                Text = ReleaseText,
                Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(TextBlock, 2);
            Grid.SetRow(TextBlock, 0);

            // Add the Image and TextBlock to the Grid
            grid.Children.Add(Image);
            grid.Children.Add(TextBlock);

            Button.Content = grid;
        }

        public readonly Release Release;
        public readonly ToggleButton Button;
        public readonly Image Image;
        public readonly TextBlock TextBlock;
        public ReleaseStruct ReleaseStuff;

        private string ReleaseText
        {
            get
            {
                if (Release is Release.PTB) return "Public Test Branch";
                if (Release is Release.CANARY) return "Canary";
                return "Stable";
            }
        }
    }
    public class BackdropProvider
    {
        private static bool IsMicaSupported()
        {
            return MicaController.IsSupported();
        }

        private static bool IsAcrylicSupported()
        {
            return DesktopAcrylicController.IsSupported();
        }
        public static bool IsSupported()
        {
            return IsMicaSupported() || IsAcrylicSupported();
        }
        public static SystemBackdrop GetBackdrop()
        {
            if (IsMicaSupported())
            {
                MicaBackdrop backdrop = new()
                {
                    Kind = MicaKind.Base
                };
                return backdrop;
            }

            if (!IsAcrylicSupported()) return null;
            {
                DesktopAcrylicBackdrop backdrop = new();

                return backdrop;
            }

        }
    }
    public class Discord
    {
        public Discord() { }

        private static Regex AppRegex = new Regex(@"^app-\d+\.\d+.\d+$");
        private static Regex DesktopCoreRegex = new Regex(@"^discord_desktop_core-\d+$");

        public enum Release
        {
            STABLE,
            PTB,
            CANARY
        }
        public static ReleaseStruct GetDiscordPath(Release release)
        {
            var LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Should not be possible but you never know
            if (string.IsNullOrEmpty(LocalAppData))
            {
                return null;
            }

            string DiscordFolderName;
            switch (release)
            {
                case Release.STABLE:
                    DiscordFolderName = "Discord";
                    break;
                case Release.PTB:
                    DiscordFolderName = "DiscordPTB";
                    break;
                case Release.CANARY:
                    DiscordFolderName = "DiscordCanary";
                    break;

                default:
                    return null;
            }

            var DiscordBasePath = System.IO.Path.Combine(LocalAppData, DiscordFolderName);

            try
            {
                if (!Directory.Exists(DiscordBasePath)) return null;
            }
            catch
            {
                return null;
            }

            string AppFolder = null;

            var Directories = Directory.GetDirectories(DiscordBasePath);
            foreach (var DirectoryPath in Directories)
            {
                var DirectoryName = DirectoryPath.Replace(DiscordBasePath, "").Substring(1);

                if (AppRegex.Match(DirectoryName).Success)
                {
                    AppFolder = DirectoryPath;
                }
            }

            if (string.IsNullOrEmpty(AppFolder))
            {
                return null;
            }

            ReleaseStruct releaseStruct = new()
            {
                Resources = System.IO.Path.Combine(AppFolder, "resources"),
                Release = release
            };

            return releaseStruct;
        }
        public class ReleaseStruct
        {
            public string Resources { get; set; }
            public Release Release { get; set; }
        }
        public class ReleasesStruct
        {
            public ReleaseStruct Stable { get; set; }
            public ReleaseStruct PTB { get; set; }
            public ReleaseStruct Canary { get; set; }
        }

        public static ReleasesStruct GetAllDiscordPaths()
        {
            ReleasesStruct structure = new()
            {
                Stable = GetDiscordPath(Release.STABLE),
                PTB = GetDiscordPath(Release.PTB),
                Canary = GetDiscordPath(Release.CANARY)
            };

            return structure;
        }
    }

    public sealed partial class MainWindow : Window
    {
        
        private DiscordReleaseButton SelectedReleaseButton;
        
        public MainWindow()
        {
            this.InitializeComponent();

            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // Set the window size to 800x600
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 600, Height = 400 });

            // Disable resize and remove maximize and minimize buttons
            var appWindowPresenter = this.AppWindow.Presenter as OverlappedPresenter;
            appWindowPresenter.IsResizable = false;
            appWindowPresenter.IsMaximizable = false;

            CustomizeTitlebar(hWnd);

            if (BackdropProvider.IsSupported())
            {
                SystemBackdrop = BackdropProvider.GetBackdrop();
            }

            this.Activated += OnActivated;

            AddReleaseButtons();
            SetUpNavigationalButtons();
            SetUpActionButtons();
        }

        private DiscordReleaseButton[] Releases;
        private void AddReleaseButtons()
        {
            DiscordReleaseButton stable = new(Release.STABLE);
            DiscordReleaseButton ptb = new(Release.PTB);
            DiscordReleaseButton canary = new(Release.CANARY);

            Releases = new[] { stable, ptb, canary };

            for (var i = 0; i < Releases.Length; i++)
            {
                var release = Releases[i];

                ReleasePage.Children.Add(release.Button);

                Grid.SetRow(release.Button, i * 2);
                Grid.SetColumn(release.Button, 0);

                release.Button.Click += (o, e) =>
                {
                    CheckNavigationalButtonsState();
                };
            }
        }
        
        private enum Page
        {
            RELEASES, ACTION, INFO
        }
        private Page CurrentPage = Page.RELEASES;
        private void GoToNextPage()
        {
            switch (CurrentPage)
            {
                case Page.RELEASES:
                    VisualStateManager.GoToState(PageContainerControl, "ActionPageState", true);
                    CurrentPage = Page.ACTION;
                    break;
                case Page.ACTION:
                    VisualStateManager.GoToState(PageContainerControl, "InfoPageState", true);
                    CurrentPage = Page.INFO;
                    break;
            }
        }
        private void GoBackPage()
        {
            switch (CurrentPage)
            {
                case Page.ACTION:
                    VisualStateManager.GoToState(PageContainerControl, "ReleasePageState", true);
                    CurrentPage = Page.RELEASES;
                    break;
            }
        }
        private bool CanGoNextPage()
        {
            switch (CurrentPage)
            {
                case Page.RELEASES:
                    return Releases.Any(release => release.Button.IsChecked != null && release.Button.IsChecked.GetValueOrDefault());
                case Page.ACTION:
                    return true;
            }

            return false;
        }
        private bool CanGoBackPage()
        {
            return CurrentPage is Page.ACTION;
        }
        private void CheckNavigationalButtonsState()
        {
            NextButton.IsEnabled = CanGoNextPage();
            BackButton.IsEnabled = CanGoBackPage();
        }
        private void SetUpNavigationalButtons()
        {
            CheckNavigationalButtonsState();

            NextButton.Click += (o, e) =>
            {
                GoToNextPage();
                CheckNavigationalButtonsState();
            };
            BackButton.Click += (o, e) =>
            {
                GoBackPage();
                CheckNavigationalButtonsState();
            };
        }

        private void SetUpActionButtons()
        {
            InstallButton.Checked += InstallButtonStateChanged;
            InstallButton.Unchecked += InstallButtonStateChanged;
            UninstallButton.Checked += UninstallButtonStateChanged;
            UninstallButton.Unchecked += UninstallButtonStateChanged;
        }

        private void InstallButtonStateChanged(object sender, RoutedEventArgs e)
        {
            UninstallButton.IsChecked = !InstallButton.IsChecked;
        }
        private void UninstallButtonStateChanged(object sender, RoutedEventArgs e)
        {
            InstallButton.IsChecked = !UninstallButton.IsChecked;
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(SettingsContainerControl, "Settings", true);
        }

        private void OnActivated(object sender, WindowActivatedEventArgs args)
        {
            var ApplicationTitle = "VX Installer";

            AppTitleTextBlock.Text = ApplicationTitle;

            Title = ApplicationTitle;
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;

        private static void RemoveMaximizeButton(IntPtr hWnd)
        {
            IntPtr style;

            // Use the appropriate function depending on the platform
            if (IntPtr.Size == 8) // 64-bit
            {
                style = GetWindowLongPtr64(hWnd, GWL_STYLE);
                style = new IntPtr(style.ToInt64() & ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX));
                SetWindowLongPtr64(hWnd, GWL_STYLE, style);
            }
            else // 32-bit
            {
                style = new IntPtr(GetWindowLong32(hWnd, GWL_STYLE) & ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX));
                SetWindowLong32(hWnd, GWL_STYLE, style.ToInt32());
            }
        }
        private void CustomizeTitlebar(IntPtr hWnd)
        {
            RemoveMaximizeButton(hWnd);

            if (!AppWindowTitleBar.IsCustomizationSupported()) return;

            var Titlebar = GetAppWindowForCurrentWindow().TitleBar;

            // Extend content into the title bar
            Titlebar.ExtendsContentIntoTitleBar = true;
            Titlebar.ButtonBackgroundColor = Colors.Transparent;
            Titlebar.ButtonInactiveBackgroundColor = Colors.Transparent;

            SetTitleBar(AppTitleBar);

            AppTitleBarRow.Height = new GridLength(Titlebar.Height);
        }
         private async void InstallVX(string releasesPath)
         {
             RenameAppAsar(releasesPath);
             await DownloadLatestAppAsar(releasesPath);
         }

         private static bool RenameAppAsar(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                throw new ArgumentException("Invalid directory path.");
            }

            var appAsarPath = Path.Combine(directoryPath, "app.asar");
            var originalAppAsarPath = Path.Combine(directoryPath, "original.app.asar");

            try
            {
                if (File.Exists(appAsarPath))
                {
                    File.Move(appAsarPath, originalAppAsarPath);
                    return true;
                }
                else
                {
                    Console.WriteLine("File 'app.asar' not found. Is Discord installed correctly?");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error renaming file: {ex.Message}");
                return false;
            }
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
