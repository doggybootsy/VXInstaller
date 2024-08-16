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
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Devices.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Automation;
using Windows.UI.ApplicationSettings;
using System.Threading.Tasks;

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
                MicaBackdrop backdrop = new();
                backdrop.Kind = MicaKind.Base;
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
        public static MainWindow CurrentWindow = null;

        public MainWindow()
        {
            CurrentWindow = this;

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

            AddReleaseButtons();
            SetUpNavigationalButtons();
            SetUpActionButtons();
            HandleUserSetting();

            AppTitle = "VX Installer";
        }

        public string AppTitle
        {
            get
            {
                return Title;
            }
            set
            {
                AppTitleTextBlock.Text = value;
                Title = value;
            }
        }

        private DiscordReleaseButton[] Releases;
        private void AddReleaseButtons()
        {
            DiscordReleaseButton stable = new(Release.STABLE);
            DiscordReleaseButton ptb = new(Release.PTB);
            DiscordReleaseButton canary = new(Release.CANARY);

            Releases = [ stable, ptb, canary ];

            for (int i = 0; i < Releases.Length; i++)
            {
                DiscordReleaseButton release = Releases[i];

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
                    foreach (var release in Releases)
                    {
                        if ((bool)release.Button.IsChecked)
                        {
                            return true;
                        }
                    }
                    return false;
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
            ComboBox UserAccount = (ComboBox)SettingPage.FindName("UserAccount");

            if (UserAccount is null) return;

            string SystemDrive = Environment.GetEnvironmentVariable("SystemDrive")!;
            string USERNAME = Environment.GetEnvironmentVariable("USERNAME")!;

            string[] AllUsers = Directory.GetDirectories(System.IO.Path.Combine(SystemDrive, "Users"));

            foreach (string User in AllUsers)
            {
                string AppData = System.IO.Path.Combine(User, "AppData");

                if (!Directory.Exists(AppData)) continue;

                // string UserName = User.Replace(System.IO.Path.GetDirectoryName(User), "").Substring(1);
                string UserName = System.IO.Path.GetFileName(User);

                UserAccount.Items.Add(UserName);
                if (USERNAME == UserName) UserAccount.SelectedItem = UserName;
            };

            UserAccount.SelectionChanged += UserAccount_SelectionChanged;
        }

        private void UserAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (DiscordReleaseButton button in Releases)
            {
                button.Button.IsEnabled = GetDiscordRelease(button.Release) is not null;
                button.Button.IsChecked = false;
            }
        }

        // Due to the setting that allows changing of the user
        // This will get the respective user
        private string GetApplicationData(bool LocalAppData = false)
        {
            ComboBox UserAccount = (ComboBox)SettingPage.FindName("UserAccount");
            // Not possible but
            if (UserAccount is null || UserAccount.SelectedItem is null) return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string SystemDrive = Environment.GetEnvironmentVariable("SystemDrive")!;

            string AppDataType = LocalAppData ? "Local" : "Roaming";

            

            return System.IO.Path.Combine(SystemDrive, "Users", UserAccount.SelectedItem.ToString(), "AppData", AppDataType);
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

            AppWindowTitleBar Titlebar = GetAppWindowForCurrentWindow().TitleBar;

            // Extend content into the title bar
            Titlebar.ExtendsContentIntoTitleBar = true;
            Titlebar.ButtonBackgroundColor = Colors.Transparent;
            Titlebar.ButtonInactiveBackgroundColor = Colors.Transparent;

            SetTitleBar(AppTitleBar);

            AppTitleBarRow.Height = new GridLength(Titlebar.Height);
        }
        public string WinAppSdkRuntimeDetails => $"WinUI 3";

        // Discord
        private static readonly Regex AppRegex = new(@"^app-\d+\.\d+.\d+$");

        public enum Release
        {
            STABLE,
            PTB,
            CANARY
        }
        public static ReleaseStruct GetDiscordRelease(Release release)
        {

            string LocalAppData = CurrentWindow.GetApplicationData(true);
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

            string DiscordBasePath = System.IO.Path.Combine(LocalAppData, DiscordFolderName);

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

        public class DiscordReleaseButton
        {
            public DiscordReleaseButton(Release release)
            {
                ReleaseStruct path = GetDiscordRelease(release);

                Release = release;

                Button = new()
                {
                    IsEnabled = path is not null,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };

                Grid grid = new();

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Fixed width for the Image
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });  // Small space between Image and TextBlock
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // TextBlock stretches in this column

                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                int ImageSize = 60;

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
    }
}
