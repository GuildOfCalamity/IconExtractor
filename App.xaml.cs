using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IconExtractor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static int m_width { get; set; } = 940;
        public static int m_height { get; set; } = 700;

        private Window? m_window;
        public static IntPtr WindowHandle { get; set; }
        public static FrameworkElement? MainRoot { get; set; }
        public static bool IsClosing { get; set; } = false;

        // https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/#advantages-and-disadvantages-of-packaging-your-app
#if IS_UNPACKAGED // We're using a custom PropertyGroup Condition we defined in the csproj to help us with the decision.
        public static bool IsPackaged { get => false; }
#else
        public static bool IsPackaged { get => true; }
#endif

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

            App.Current.DebugSettings.FailFastOnErrors = false;

            #region [Exception handlers]
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomainFirstChanceException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            UnhandledException += ApplicationUnhandledException;
            #endregion

            this.InitializeComponent();

            // https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.focusvisualkind?view=windows-app-sdk-1.3
            this.FocusVisualKind = FocusVisualKind.Reveal;

            if (Debugger.IsAttached)
            {
                this.DebugSettings.BindingFailed += DebugOnBindingFailed;
                this.DebugSettings.XamlResourceReferenceFailed += DebugOnXamlResourceReferenceFailed;
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var appInst = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent();
            if (appInst != null)
            {
                Debug.WriteLine($"[INFO] Application ProcessId: {appInst.ProcessId}");
                var appActArgs = appInst.GetActivatedEventArgs();
                Debug.WriteLine($"[INFO] Activation args type: {appActArgs.Data.GetType().Name}");
                if (appActArgs.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs actEA)
                {
                    Debug.WriteLine($"[INFO] Activation kind: {actEA.Kind}");
                }
            }


            m_window = new MainWindow();


            var appWin = GetAppWindow(m_window);
            if (appWin != null)
            {
                // Gets or sets a value that indicates whether this window will appear in various system representations, such as ALT+TAB and taskbar.
                appWin.IsShownInSwitchers = true;

                // We don't have the Closing event exposed by default, so we'll use the AppWindow to compensate.
                appWin.Closing += (s, e) =>
                {
                    App.IsClosing = true;
                    Debug.WriteLine($"[INFO] Application closing detected at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
                };

                // Destroying is always called, but Closing is only called when the application is shutdown normally.
                appWin.Destroying += (s, e) =>
                {
                    Debug.WriteLine($"[INFO] Application destroying detected at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
                };

                // Set the application icon.
                if (IsPackaged)
                    appWin.SetIcon(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, $"Assets/StoreLogo.ico"));
                else
                    appWin.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, $"Assets/StoreLogo.ico"));

                appWin.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
            }


            m_window.Activate();


            // Save the FrameworkElement for any future content dialogs.
            MainRoot = m_window.Content as FrameworkElement;

            appWin?.Resize(new Windows.Graphics.SizeInt32(m_width, m_height));
            CenterWindow(m_window);
        }

        #region [Window Helpers]
        /// <summary>
        /// This code example demonstrates how to retrieve an AppWindow from a WinUI3 window.
        /// The AppWindow class is available for any top-level HWND in your app.
        /// AppWindow is available only to desktop apps (both packaged and unpackaged), it's not available to UWP apps.
        /// https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/windowing/windowing-overview
        /// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.create?view=windows-app-sdk-1.3
        /// </summary>
        public Microsoft.UI.Windowing.AppWindow? GetAppWindow(object window)
        {
            // Retrieve the window handle (HWND) of the current (XAML) WinUI3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // For other classes to use (mostly P/Invoke).
            App.WindowHandle = hWnd;

            // Retrieve the WindowId that corresponds to hWnd.
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            // Lastly, retrieve the AppWindow for the current (XAML) WinUI3 window.
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            return appWindow;
        }

        /// <summary>
        /// Centers a <see cref="Microsoft.UI.Xaml.Window"/> based on the <see cref="Microsoft.UI.Windowing.DisplayArea"/>.
        /// </summary>
        /// <remarks>This must be run on the UI thread.</remarks>
        public static void CenterWindow(Window window)
        {
            if (window == null) { return; }

            try
            {
                System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                if (Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId) is Microsoft.UI.Windowing.AppWindow appWindow &&
                    Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest) is Microsoft.UI.Windowing.DisplayArea displayArea)
                {
                    Windows.Graphics.PointInt32 CenteredPosition = appWindow.Position;
                    CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
                    CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
                    appWindow.Move(CenteredPosition);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// The <see cref="Microsoft.UI.Windowing.DisplayArea"/> exposes properties such as:
        /// OuterBounds     (Rect32)
        /// WorkArea.Width  (int)
        /// WorkArea.Height (int)
        /// IsPrimary       (bool)
        /// DisplayId.Value (ulong)
        /// </summary>
        /// <param name="window"></param>
        /// <returns><see cref="DisplayArea"/></returns>
        public Microsoft.UI.Windowing.DisplayArea? GetDisplayArea(Window window)
        {
            try
            {
                System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                var da = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                return da;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region [Domain Events]
        void ApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
            Exception? ex = e.Exception;
            Debug.WriteLine($"[UnhandledException]: {ex?.Message}");
            Debug.WriteLine($"Unhandled exception of type {ex?.GetType()}: {ex}");
            DebugLog($"Unhandled Exception StackTrace: {Environment.StackTrace}");
            DebugLog($"{ex?.DumpFrames()}");
            e.Handled = true;
        }

        void CurrentDomainOnProcessExit(object? sender, EventArgs e)
        {
            if (!IsClosing)
                IsClosing = true;

            if (sender is null)
                return;

            if (sender is AppDomain ad)
            {
                Debug.WriteLine($"[OnProcessExit]", $"{nameof(App)}");
                Debug.WriteLine($"DomainID: {ad.Id}", $"{nameof(App)}");
                Debug.WriteLine($"FriendlyName: {ad.FriendlyName}", $"{nameof(App)}");
                Debug.WriteLine($"BaseDirectory: {ad.BaseDirectory}", $"{nameof(App)}");
            }
        }

        void CurrentDomainFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Debug.WriteLine($"[ERROR] First chance exception from {sender?.GetType()}: {e.Exception.Message}");
            DebugLog($"First chance exception from {sender?.GetType()}: {e.Exception.Message}");
            if (e.Exception.InnerException != null)
                DebugLog($"  ⇨ InnerException: {e.Exception.InnerException.Message}");
            DebugLog($"First chance exception StackTrace: {Environment.StackTrace}");
            DebugLog($"{e.Exception.DumpFrames()}");
        }

        void CurrentDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
        {
            Exception? ex = e.ExceptionObject as Exception;
            Debug.WriteLine($"[ERROR] Thread exception of type {ex?.GetType()}: {ex}");
            DebugLog($"Thread exception of type {ex?.GetType()}: {ex}");
            DebugLog($"{ex?.DumpFrames()}");
        }

        void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception is AggregateException aex)
            {
                aex?.Flatten().Handle(ex =>
                {
                    Debug.WriteLine($"[ERROR] Unobserved task exception: {ex?.Message}");
                    DebugLog($"Unobserved task exception: {ex?.Message}");
                    DebugLog($"{ex?.DumpFrames()}");
                    return true;
                });
            }
            e.SetObserved(); // suppress and handle manually
        }

        /// <summary>
        /// Simplified debug logger for app-wide use.
        /// </summary>
        /// <param name="message">the text to append to the file</param>
        public static void DebugLog(string message)
        {
            try
            {
                if (App.IsPackaged)
                    System.IO.File.AppendAllText(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Debug.log"), $"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")}] {message}{Environment.NewLine}");
                else
                    System.IO.File.AppendAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory, "Debug.log"), $"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")}] {message}{Environment.NewLine}");
            }
            catch (Exception)
            {
                Debug.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")}] {message}");
            }
        }
        #endregion

        #region [Debugger Events]
        void DebugOnXamlResourceReferenceFailed(DebugSettings sender, XamlResourceReferenceFailedEventArgs args)
        {
            Debug.WriteLine($"[WARNING] XamlResourceReferenceFailed: {args.Message}");
        }

        void DebugOnBindingFailed(object sender, BindingFailedEventArgs args)
        {
            Debug.WriteLine($"[WARNING] BindingFailed: {args.Message}");
        }
        #endregion

        #region [Reflection Helpers]
        /// <summary>
        /// Returns the declaring type's namespace.
        /// </summary>
        public static string? GetCurrentNamespace() => System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Namespace;

        /// <summary>
        /// Returns the declaring type's full name.
        /// </summary>
        public static string? GetCurrentFullName() => System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Assembly.FullName;

        /// <summary>
        /// Returns the declaring type's assembly name.
        /// </summary>
        public static string? GetCurrentAssemblyName() => System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        /// <summary>
        /// Returns the AssemblyVersion, not the FileVersion.
        /// </summary>
        public static Version GetCurrentAssemblyVersion() => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
        #endregion
    }
}
