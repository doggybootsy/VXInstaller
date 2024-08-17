using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using WinRT;
using System.Text.RegularExpressions;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Devices.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Automation;
using Windows.UI.ApplicationSettings;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;

namespace Github
{
    public class Asset
    {
        public string url { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string name { get; set; }
        public object label { get; set; }
        public Uploader uploader { get; set; }
        public string content_type { get; set; }
        public string state { get; set; }
        public int size { get; set; }
        public int download_count { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string browser_download_url { get; set; }
    }

    public class Author
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public bool site_admin { get; set; }
    }

    public class Release
    {
        public string url { get; set; }
        public string assets_url { get; set; }
        public string upload_url { get; set; }
        public string html_url { get; set; }
        public int id { get; set; }
        public Author author { get; set; }
        public string node_id { get; set; }
        public string tag_name { get; set; }
        public string target_commitish { get; set; }
        public string name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public DateTime created_at { get; set; }
        public DateTime published_at { get; set; }
        public List<Asset> assets { get; set; }
        public string tarball_url { get; set; }
        public string zipball_url { get; set; }
        public string body { get; set; }
    }

    public class Uploader
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public bool site_admin { get; set; }
    }
}

namespace VXInstaller
{
    public class BackdropProvider
    {
        static public bool IsMicaSupported()
        {
            return MicaController.IsSupported();
        }
        static public bool IsAcrylicSupported()
        {
            return DesktopAcrylicController.IsSupported();
        }
        static public bool IsSupported()
        {
            return IsMicaSupported() || IsAcrylicSupported();
        }
        static public SystemBackdrop GetBackdrop()
        {
            if (IsMicaSupported())
            {
                MicaBackdrop backdrop = new()
                {
                    Kind = MicaKind.Base
                };
                return backdrop;
            }
            if (IsAcrylicSupported())
            {
                DesktopAcrylicBackdrop backdrop = new();

                return backdrop;
            }

            return null;
        }
    }
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

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

            SetUpNavigationalButtons();
            SetUpActionButtons();
            HandleUserSetting();

            Title = "VX Installer";


            StableReleaseButton.IsEnabled = GetDiscordRelease(Release.STABLE) is not null;
            PTBReleaseButton.IsEnabled = GetDiscordRelease(Release.PTB) is not null;
            CanaryReleaseButton.IsEnabled = GetDiscordRelease(Release.CANARY) is not null;

            SetupLogoTheming();
            SetupLoader();
        }
        private void ReleaseButtonClick(object s, RoutedEvent e)
        {
            CheckNavigationalButtonsState();
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
                    UserAccount.IsEnabled = false;
                    break;
                case Page.ACTION:
                    VisualStateManager.GoToState(PageContainerControl, "InfoPageState", true);
                    CurrentPage = Page.INFO;

                    if (InstallButton.IsChecked is true)
                    {
                        if (StableReleaseButton.IsChecked is true) Install(GetDiscordRelease(Release.STABLE));
                        if (PTBReleaseButton.IsChecked is true) Install(GetDiscordRelease(Release.PTB));
                        if (CanaryReleaseButton.IsChecked is true) Install(GetDiscordRelease(Release.CANARY));
                    }
                    else
                    {
                        if (StableReleaseButton.IsChecked is true) Uninstall(GetDiscordRelease(Release.STABLE));
                        if (PTBReleaseButton.IsChecked is true) Uninstall(GetDiscordRelease(Release.PTB));
                        if (CanaryReleaseButton.IsChecked is true) Uninstall(GetDiscordRelease(Release.CANARY));
                    }

                    break;
                case Page.INFO:
                    Close();

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
                    UserAccount.IsEnabled = true;
                    break;
                case Page.INFO:

                    StableReleaseButton.IsChecked = false;
                    PTBReleaseButton.IsChecked = false;
                    CanaryReleaseButton.IsChecked = false;

                    VisualStateManager.GoToState(PageContainerControl, "ReleasePageState", true);

                    BackButton.Content = "Back";
                    NextButton.Content = "Next";

                    CurrentPage = Page.RELEASES;
                    UserAccount.IsEnabled = true;

                    InfoLog.Children.Clear();
                    break;
            }
        }
        private void CheckNextButtonState()
        {
            switch (CurrentPage)
            {
                case Page.RELEASES:
                    NextButton.IsEnabled = StableReleaseButton.IsChecked is true || PTBReleaseButton.IsChecked is true || CanaryReleaseButton.IsChecked is true;
                    break;
                case Page.ACTION:
                    NextButton.IsEnabled = true;
                    break;
                default:
                    NextButton.IsEnabled = false;
                    break;
            }
        }
        private void CheckBackButtonState()
        {
            BackButton.IsEnabled = CurrentPage is Page.ACTION;
        }
        private void CheckNavigationalButtonsState()
        {
            CheckNextButtonState();
            CheckBackButtonState();
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

        private void ClickCheckNavigationalButtonsState(object sender, RoutedEventArgs e)
        {
            CheckNavigationalButtonsState();
        }

        // Actions buttons
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

        // Settings
        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(SettingsContainerControl, "Settings", true);
        }
        private void CloseSettings(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(SettingsContainerControl, "MainApp", true);
        }
        private void HandleUserSetting()
        {
            string SystemDrive = Environment.GetEnvironmentVariable("SystemDrive")!;
            string USERNAME = Environment.GetEnvironmentVariable("USERNAME")!;

            string[] AllUsers = Directory.GetDirectories(Path.Combine(SystemDrive, "Users"));

            foreach (string User in AllUsers)
            {
                string AppData = Path.Combine(User, "AppData");

                if (!Directory.Exists(AppData)) continue;

                // string UserName = User.Replace(Path.GetDirectoryName(User), "").Substring(1);
                string UserName = Path.GetFileName(User);

                UserAccount.Items.Add(UserName);
                if (USERNAME == UserName) UserAccount.SelectedItem = UserName;
            };

            UserAccount.SelectionChanged += UserAccount_SelectionChanged;
        }
        private void UserAccountReset(object sender, RoutedEventArgs e)
        {
            string USERNAME = Environment.GetEnvironmentVariable("USERNAME")!;

            UserAccount.SelectedItem = USERNAME;
        }

        private void UserAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StableReleaseButton.IsEnabled = GetDiscordRelease(Release.STABLE) is not null;
            StableReleaseButton.IsChecked = false;

            PTBReleaseButton.IsEnabled = GetDiscordRelease(Release.PTB) is not null;
            PTBReleaseButton.IsChecked = false;

            CanaryReleaseButton.IsEnabled = GetDiscordRelease(Release.CANARY) is not null;
            CanaryReleaseButton.IsChecked = false;
        }

        // Due to the setting that allows changing of the user
        // This will get the respective user
        private string GetApplicationData(bool LocalAppData = false)
        {
            if (UserAccount.SelectedItem is null) return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string SystemDrive = Environment.GetEnvironmentVariable("SystemDrive")!;

            string AppDataType = LocalAppData ? "Local" : "Roaming";

            return Path.Combine(SystemDrive, "Users", UserAccount.SelectedItem.ToString(), "AppData", AppDataType);
        }
        private void SetupLogoTheming()
        {
            Content.As<FrameworkElement>()!.ActualThemeChanged += (s, e) =>
            {
                SetLogoTheme();
            };
            SetLogoTheme();
        }
        private void SetLogoTheme()
        {
            FrameworkElement content = Content.As<FrameworkElement>()!;
            ElementTheme value = content.ActualTheme;

            string type = "Dark";
            if (value == ElementTheme.Light) type = "Light";

            string source = $"ms-appx:///Assets/VXLogo.{type}.png";
            Uri uri = new(source);

            AboutSection.HeaderIcon = new BitmapIcon()
            {
                UriSource = uri
            };

            ApplicationIcon.Source = new BitmapImage(uri);
            LoaderLogo.Source = new BitmapImage(uri);
        }
        private async void SetupLoader()
        {
            await Task.Delay(1000);

            Loader.Visibility = Visibility.Collapsed;
            SettingsContainerControl.Visibility = Visibility.Visible;
            ApplicationIcon.Visibility = Visibility.Visible;
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        // Customize the Titlebar
        // DLL's to remove the Minimize and Maxime button
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

            AppWindowTitleBar titleBar = GetAppWindowForCurrentWindow().TitleBar;

            // Extend content into the title bar
            ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            SetTitleBar(AppTitleBar);

            AppTitleBarRow.Height = new GridLength(titleBar.Height);

            var content = (Content as FrameworkElement)!;
            content.ActualThemeChanged += (s, e) =>
            {
                ModifyTitlebarTheme();
            };
            ModifyTitlebarTheme();
        }
        private void ModifyTitlebarTheme()
        {
            FrameworkElement content = Content.As<FrameworkElement>()!;
            ElementTheme value = content.ActualTheme;

            AppWindowTitleBar titleBar = AppWindow.TitleBar;
            if (value == ElementTheme.Light)
            {
                titleBar.ForegroundColor = Colors.Black;
                titleBar.BackgroundColor = Colors.White;
                titleBar.InactiveForegroundColor = Colors.Gray;
                titleBar.InactiveBackgroundColor = Colors.White;

                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;

                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 245, 245, 245);
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonPressedBackgroundColor = Colors.White;
            }
            else if (value == ElementTheme.Dark)
            {
                titleBar.ForegroundColor = Colors.White;
                titleBar.BackgroundColor = Windows.UI.Color.FromArgb(255, 31, 31, 31);
                titleBar.InactiveForegroundColor = Colors.Gray;
                titleBar.InactiveBackgroundColor = Windows.UI.Color.FromArgb(255, 31, 31, 31);

                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;

                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 51, 51, 51);
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonPressedBackgroundColor = Colors.Gray;
            }

            // Fixed bug where icon background color was not applied 
            titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            titleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        }

        // Setting stuff
        // TODO: Show Win App SDK Stuff
        public string WinAppSdkRuntimeDetails => $"Windows App SDK";
        private void CreateIssue(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/doggybootsy/VXInstaller/issues/new", UseShellExecute = true });
        }

        // Discord
        [GeneratedRegex(@"^app-\d+\.\d+.\d+$")]
        private static partial Regex AppRegex();

        public enum Release
        {
            STABLE,
            PTB,
            CANARY
        }
        public ReleaseStruct GetDiscordRelease(Release release)
        {

            string LocalAppData = GetApplicationData(true);
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

            string DiscordBasePath = Path.Combine(LocalAppData, DiscordFolderName);

            try
            {
                if (!Directory.Exists(DiscordBasePath)) return null;
            }
            catch
            {
                return null;
            }

            string AppFolder = null;

            string[] Directories = Directory.GetDirectories(DiscordBasePath);
            foreach (string DirectoryPath in Directories)
            {
                string DirectoryName = DirectoryPath.Replace(DiscordBasePath, "").Substring(1);

                if (AppRegex().Match(DirectoryName).Success)
                {
                    AppFolder = DirectoryPath;
                }
            }

            if (string.IsNullOrEmpty(AppFolder))
            {
                return null;
            }

            string exe = "Discord.exe";
            if (release is Release.PTB) exe = "DiscordPTB.exe";
            else if (release is Release.CANARY) exe = "DiscordCanary.exe";

            ReleaseStruct releaseStruct = new()
            {
                Resources = Path.Combine(AppFolder, "resources"),
                ExeLocation = Path.Combine(AppFolder, exe),
                Release = release
            };

            return releaseStruct;
        }
        public class ReleaseStruct
        {
            public string Resources { get; set; }
            public string ExeLocation { get; set; }
            public Release Release { get; set; }
        }

        private async Task<bool> CloseDiscord(ReleaseStruct release)
        {
            bool didClose = false;
            bool hasLogged = false;

            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                try
                {
                    if (process.MainModule == null) continue;

                    if (process.MainModule.FileName.Equals(release.ExeLocation, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!process.HasExited)
                        {
                            if (!hasLogged)
                            {
                                hasLogged = true;
                                AddLog("Closing Discord");
                            }

                            // Attempt to close gracefully first (if applicable)
                            process.CloseMainWindow();
                            // Wait a little
                            await Task.Delay(10);

                            if (!process.HasExited)
                            {
                                process.Kill();
                            }

                            didClose = true;
                        }

                        await process.WaitForExitAsync();
                    }
                }
                
                catch (System.ComponentModel.Win32Exception) { }
                catch (InvalidOperationException) { }
                finally
                {
                    process.Dispose();
                }
            }

            return didClose;
        }
        private void OpenDiscord(ReleaseStruct release)
        {
            AddLog("Opening Discord");

            ProcessStartInfo startInfo = new()
            {
                FileName = release.ExeLocation, // The executable you want to start
                UseShellExecute = true   // Use the operating system shell to start the process
            };

            Process.Start(startInfo);
        }

        private void AddLog(string log)
        {
            TextBlock textBlock = new()
            {
                Text = log
            };

            InfoLog.Children.Add(textBlock);
        }

        // Installing
        public readonly string IndexJsScript = @"// VX Index.js v1.0.0
const fs = require('node:fs');
const path = require('node:path');

const asars = fs.readdirSync(__dirname)
  .filter((file) => file.endsWith('.asar'))
  .map((file) => file.replace('.asar', ''))
  .filter((file) => /^(\d+)\.(\d+)\.(\d+)/.test(file));

const [ latest, ...old ] = asars.sort((a, b) => -a.localeCompare(b));

for (const version of old) {
  try { fs.unlinkSync(path.join(__dirname, `${version}.asar`)); }
  catch (error) {
    console.log('[vx]:', 'Unable to delete version', version);
  }
}

require(`./${latest}.asar`);";

        private string REPO_API_URL = "https://api.github.com/repos/doggybootsy/vx/releases/latest";

        private async Task<bool> DownloadLatestAsar()
        {
            AddLog("Fetching Latest Release");

            using HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("VX Installer");

            using HttpResponseMessage response = await client.GetAsync(REPO_API_URL);

            if (!response.IsSuccessStatusCode)
            {
                AddLog($"Failed to fetch [status code {response.StatusCode}]");
                return false;
            }

            Github.Release release = await response.Content.ReadFromJsonAsync<Github.Release>();

            string version = release.tag_name;
            if (version.StartsWith("v"))
            {
                version = version.Substring(1);
            }

            AddLog($"Latest version is v{version}");

            string ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string path = Path.Combine(ApplicationData, ".vx", "app", $"{version}.asar");

            if (File.Exists(path))
            {
                AddLog("Latest version is installed");
                return true;
            }

            AddLog("Fetching assets");

            Github.Asset asar = null;

            foreach (Github.Asset asset in release.assets)
            {
                if (asset.name.EndsWith(".asar"))
                {
                    asar = asset;
                }
            }

            if (asar is null)
            {
                AddLog("Asset 'app.asar' not found!");
                return false;
            }

            AddLog("Downloading latest asset");
            using Stream stream = await client.GetStreamAsync(asar.browser_download_url);
            using FileStream fs = new(path, FileMode.OpenOrCreate);

            await stream.CopyToAsync(fs);
            AddLog("Downloaded latest asset");

            return true;
        }

        private void EnsureVXPath()
        {
            AddLog("Ensuring VX Directory");

            string ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string vxDirectory = Path.Combine(ApplicationData, ".vx");

            if (!Directory.Exists(vxDirectory)) Directory.CreateDirectory(vxDirectory);

            string vxAppDirectory = Path.Combine(vxDirectory, "app");
            if (!Directory.Exists(vxAppDirectory)) Directory.CreateDirectory(vxAppDirectory);

            string vxAppIndex = Path.Combine(vxAppDirectory, "index.js");
            if (!File.Exists(vxAppIndex)) File.WriteAllText(vxAppIndex, IndexJsScript);
        }

        private async void Install(ReleaseStruct release)
        {
            BackButton.Content = "Restart";
            NextButton.Content = "Exit";

            EnsureVXPath();

            await DownloadLatestAsar();

            bool wasDiscordOpen = await CloseDiscord(release);

            AddLog($"Was Discord open {wasDiscordOpen}");

            string appAsar = Path.Combine(release.Resources, "app.asar");
            string originalAppAsar = Path.Combine(release.Resources, "vx.app.asar");

            // If original exists then we just stop
            if (File.Exists(originalAppAsar))
            {
                AddLog("VX is already injected");

                if (wasDiscordOpen) OpenDiscord(release);
                return;
            }

            Console.WriteLine($"{Process.GetProcessesByName("Discord").Length}");

            AddLog("Injecting VX");

            File.Move(appAsar, originalAppAsar);

            string appDirectory = Path.Combine(release.Resources, "app");

            Directory.CreateDirectory(appDirectory);

            // Allow $VX_REQUIRE to be set so you can have a local version
            // For vx development
            // If its empty / null use %APPDATA%\.vx\app
            string app = Environment.GetEnvironmentVariable("VX_INJECTION_PATH");

            if (string.IsNullOrEmpty(app))
            {
                app = @$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\.vx\app";
            }

            string index = $"require('{app.Replace("\\", "/")}');\nrequire('../vx.app.asar');";


            string package = @"{""main"": ""index.js""}";

            File.WriteAllText(Path.Combine(appDirectory, "index.js"), index);
            File.WriteAllText(Path.Combine(appDirectory, "package.json"), package);

            AddLog("VX is injected");
            if (wasDiscordOpen) OpenDiscord(release);

            BackButton.IsEnabled = true;
            NextButton.IsEnabled = true;
        }
        private async void Uninstall(ReleaseStruct release)
        {
            BackButton.Content = "Restart";
            NextButton.Content = "Exit";

            bool wasDiscordOpen = await CloseDiscord(release);

            string originalAppAsar = Path.Combine(release.Resources, "vx.app.asar");

            // If original exists then we just stop
            if (!File.Exists(originalAppAsar))
            {
                AddLog("VX is already uninjected");
                if (wasDiscordOpen) OpenDiscord(release);
                return;
            }

            File.Move(originalAppAsar, Path.Combine(release.Resources, "app.asar"));

            Directory.Delete(Path.Combine(release.Resources, "app"), true);

            AddLog("VX is uninjected");
            if (wasDiscordOpen) OpenDiscord(release);

            BackButton.IsEnabled = true;
            NextButton.IsEnabled = true;
        }
    }
}
