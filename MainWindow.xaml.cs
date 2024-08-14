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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VXInstaller
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        AppWindow _appwindow;
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

        AppWindowTitleBar _titlebar;
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
            get { return ApplicationView.GetForCurrentView().Title; }
            set { ApplicationView.GetForCurrentView().Title = value; }
        }

        public MainWindow()
        {
            this.InitializeComponent();

            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            // Set the window size to 800x600
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 800, Height = 600 });

            // Disable resize
            var appWindowPresenter = this.AppWindow.Presenter as OverlappedPresenter;
            appWindowPresenter.IsResizable = false;
            appWindowPresenter.IsMaximizable = false;

            // Extend content into the title bar
            AppWindowTitleBar.ExtendsContentIntoTitleBar = true;

            AppWindowTitleBar.ButtonBackgroundColor = Colors.Transparent;

            SetTitleBar(AppTitleBar);
        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            myButton.Content = "Clicked";
        }
    }
}
