using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

using IconExtractor.Models;
using IconExtractor.Support;
using IconExtractor.Controls;

using TargetFrameworkAttribute = System.Runtime.Versioning.TargetFrameworkAttribute;
using InformationalAttribute = System.Reflection.AssemblyInformationalVersionAttribute;
using ConfigurationAttribute = System.Reflection.AssemblyConfigurationAttribute;
using FileVersionAttribute = System.Reflection.AssemblyFileVersionAttribute;
using ProductAttribute = System.Reflection.AssemblyProductAttribute;
using CompanyAttribute = System.Reflection.AssemblyCompanyAttribute;


namespace IconExtractor;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    #region [Props]
    bool _loaded = false;
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<IconIndexItem> IconItems { get; set; } = new();

    public List<string> dlls = new List<string>
    {
        "aclui.dll",
        "accessibilitycpl.dll",
        "ActionCenter.dll",
        "ActionCenterCPL.dll",
        "AdmTmpl.dll",
        "appmgr.dll",
        "audiosrv.dll",
        "AuditNativeSnapIn.dll",
        "AuthFWGP.dll",
        "autoplay.dll",
        "basecsp.dll",
        "azroleui.dll",
        "bootux.dll",
        "bthci.dll",
        "btpanui.dll",
        "BthpanContextHandler.dll",
        "cabview.dll",
        "CastingShellExt.dll",
        "CertEnrollUI.dll",
        "cewmdm.dll",
        "certmgr.dll",
        "cmdial32.dll",
        "cmlua.dll",
        "cmstplua.dll",
        "colorui.dll",
        "comres.dll",
        "console.dll",
        "ContentDeliveryManager.Utilities.dll",
        "cryptuiwizard.dll",
        "DAMM.dll",
        "deskadp.dll",
        "deskmon.dll",
        "DeviceCenter.dll",
        "DevicePairingFolder.dll",
        "dfshim.dll",
        "devmgr.dll",
        "diagperf.dll",
        "DiagCpl.dll",
        "Display.dll",
        "dmdskres.dll",
        "dot3gpui.dll",
        "dot3mm.dll",
        "dskquoui.dll",
        "dsprop.dll",
        "dsquery.dll",
        "DXP.dll",
        "DxpTaskSync.dll",
        "eapsimextdesktop.dll",
        "EditionUpgradeManagerObj.dll",
        "EhStorShell.dll",
        "EhStorPwdMgr.dll",
        "els.dll",
        "ExplorerFrame.dll",
        "fde.dll",
        "fdprint.dll",
        "fhcpl.dll",
        "filemgmt.dll",
        "FirewallControlPanel.dll",
        "fontext.dll",
        "fveui.dll",
        "fvewiz.dll",
        "fvecpl.dll",
        "FXSCOMPOSERES.dll",
        "gcdef.dll",
        "gpprefcl.dll",
        "gpedit.dll",
        "hgcpl.dll",
        "hnetcfg.dll",
        "hotplug.dll",
        "icm32.dll",
        "icsigd.dll",
        "iernonce.dll",
        "ieframe.dll",
        "imagesp1.dll",
        "imageres.dll",
        "input.dll",
        "INETRES.dll",
        "ipsecsnp.dll",
        "ipsmsnap.dll",
        "itss.dll",
        "iscsicpl.dll",
        "keymgr.dll",
        "localsec.dll",
        "mapi32.dll",
        "mapistub.dll",
        "mciavi32.dll",
        "mferror.dll",
        "miguiresource.dll",
        "mmcshext.dll",
        "mmcbase.dll",
        "moricons.dll",
        "mqsnap.dll",
        "mqutil.dll",
        "msacm32.dll",
        "msctf.dll",
        "mscandui.dll",
        "msctfui.dll",
        "msi.dll",
        "msident.dll",
        "msidntld.dll",
        "msihnd.dll",
        "msieftp.dll",
        "msports.dll",
        "mssvp.dll",
        "mstsc.exe",
        "msutb.dll",
        "mstask.dll",
        "msxml3.dll",
        "mycomput.dll",
        "mydocs.dll",
        "ncpa.cpl",
        "ndfapi.dll",
        "netplwiz.dll",
        "netcenter.dll",
        "netshell.dll",
        "networkexplorer.dll",
        "newdev.dll",
        "ntlanui2.dll",
        "ntshrui.dll",
        "nvcuda.dll",
        "ole32.dll",
        "objsel.dll",
        "occache.dll",
        "oleprn.dll",
        "packager.dll",
        "pifmgr.dll",
        "photowiz.dll",
        "pmcsnap.dll",
        "pnpclean.dll",
        "PortableDeviceStatus.dll",
        "ppcsnap.dll",
        "powercpl.dll",
        "printui.dll",
        "prnntfy.dll",
        "prnfldr.dll",
        "quartz.dll",
        "RADCUI.dll",
        "rasgcw.dll",
        "RASMM.dll",
        "rasdlg.dll",
        "rdbui.dll",
        "rastlsext.dll",
        "rastls.dll",
        "remotepg.dll",
        "sberes.dll",
        "scavengeui.dll",
        "SCardDlg.dll",
        "scksp.dll",
        "scrobj.dll",
        "sdhcinst.dll",
        "scrptadm.dll",
        "SearchFolder.dll",
        "sdcpl.dll",
        "SecurityHealthAgent.dll",
        "SecurityHealthSSO.dll",
        "setupapi.dll",
        "SensorsCpl.dll",
        "shlwapi.dll",
        "shell32.dll",
        "setupcln.dll",
        "shwebsvc.dll",
        "softkbd.dll",
        "SndVolSSO.dll",
        "SpaceControl.dll",
        "sppcommdlg.dll",
        "sppcomapi.dll",
        "srm.dll",
        "srchadmin.dll",
        "srrstr.dll",
        "SrpUxNativeSnapIn.dll",
        "sti.dll",
        "stobject.dll",
        "sud.dll",
        "sysclass.dll",
        "SysFxUI.dll",
        "Tabbtn.dll",
        "tcpipcfg.dll",
        "taskbarcpl.dll",
        "tapiui.dll",
        "themecpl.dll",
        "tpmcompc.dll",
        "TSWorkspace.dll",
        "twext.dll",
        "UIRibbonRes.dll",
        "urlmon.dll",
        "user32.dll",
        "url.dll",
        "usbui.dll",
        "UserAccountControlSettings.dll",
        "usercpl.dll",
        "VAN.dll",
        "Vault.dll",
        "vfwwdm32.dll",
        "wdc.dll",
        "webcheck.dll",
        "werui.dll",
        "werconcpl.dll",
        "wiaaut.dll",
        "WFSR.dll",
        "wiadefui.dll",
        "wiashext.dll",
        "Windows.Storage.Search.dll",
        "Windows.UI.CredDialogController.dll",
        "winmm.dll",
        "wininetlui.dll",
        "winsrv.dll",
        "wlanpref.dll",
        "wlangpui.dll",
        "WMPhoto.dll",
        "WorkfoldersControl.dll",
        "wmploc.DLL",
        "WorkFoldersRes.dll",
        "wsecedit.dll",
        "zipfldr.dll",
        #region [Original Reference List]
        //"imageres.dll",
        //"shell32.dll",
        //"ddores.dll",
        //"wmploc.dll",
        //"pifmgr.dll",
        //"accessibilitycpl.dll",
        //"moricons.dll",
        //"mmcndmgr.dll",
        //"mmres.dll",
        //"netcenter.dll",
        //"netshell.dll",
        //"networkexplorer.dll",
        //"pnidui.dll",
        //"sensorscpl.dll",
        //"setupapi.dll",
        //"wpdshext.dll",
        //"compstui.dll",
        //"ieframe.dll",
        //"dmdskres.dll",
        //"dsuiext.dll",
        //"mstscax.dll",
        //"wiashext.dll",
        //"comres.dll",
        //"actioncentercpl.dll",
        //"aclui.dll",
        //"autoplay.dll",
        //"comctl32.dll",
        //"filemgmt.dll",
        //"ncpa.cpl",
        //"url.dll",
        //"xwizards.dll",
        //"imagesp1.dll",
        //"mstsc.exe",
        //"explorer.exe",
        #endregion
    };

    private string _target = "imageres.dll";
    public string TargetDLL
    {
        get => _target;
        set
        {
            _target = value;
            NotifyPropertyChanged(nameof(TargetDLL));
        }
    }

    int _selectedDLLIndex = 0;
    public int SelectedDLLIndex
    {
        get => _selectedDLLIndex;
        set
        {
            _selectedDLLIndex = value;
            NotifyPropertyChanged(nameof(SelectedDLLIndex));
        }
    }

    private string _targetWidth = "32";
    public string TargetWidth
    {
        get => _targetWidth;
        set
        {
            _targetWidth = value;
            NotifyPropertyChanged(nameof(TargetWidth));
        }
    }

    private string _targetHeight = "32";
    public string TargetHeight
    {
        get => _targetHeight;
        set
        {
            _targetHeight = value;
            NotifyPropertyChanged(nameof(TargetHeight));
        }
    }

    private string _status = "";
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            NotifyPropertyChanged(nameof(Status));
        }
    }

    private bool _saveToDisk = false;
    public bool SaveToDisk
    {
        get => _saveToDisk;
        set
        {
            _saveToDisk = value;
            NotifyPropertyChanged(nameof(SaveToDisk));
        }
    }

    private bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set 
        {
            _isBusy = value;
            NotifyPropertyChanged(nameof(IsBusy));
            _isNotBusy = !_isBusy;
            NotifyPropertyChanged(nameof(IsNotBusy));
        }
    }

    private bool _isNotBusy = true;
    public bool IsNotBusy
    {
        get => _isNotBusy;
        set
        {
            _isNotBusy = value;
            NotifyPropertyChanged(nameof(IsNotBusy));
            _isBusy = !_isNotBusy;
            NotifyPropertyChanged(nameof(IsBusy));
        }
    }

    Microsoft.UI.Xaml.Media.ImageSource? _imgSource;
    public ImageSource? ImgSource 
    { 
        get => _imgSource; 
        set
        {
            _imgSource = value;
            NotifyPropertyChanged(nameof(ImgSource));
        }
    }

    IconFileInfo? _ShieldIconFileInfo;
    public IconFileInfo? ShieldIconFileInfo 
    {
        get
        {
            if (_ShieldIconFileInfo is null)
            {
                var imageResList = Extensions.ExtractSelectedIconsFromDLL(
                    imageresPath,
                new List<int>() { Constants.ImageRes.ShieldIcon },
                24);
                _ShieldIconFileInfo = imageResList.First();
            }
            return _ShieldIconFileInfo;
        }
        set
        {
            _ShieldIconFileInfo = value;
            NotifyPropertyChanged(nameof(ShieldIconFileInfo));
        }
    }

    IconFileInfo? _LandscapeIconFileInfo;
    public IconFileInfo? LandscapeIconFileInfo
    {
        get
        {
            if (_LandscapeIconFileInfo is null)
            {
                var imageResList = Extensions.ExtractSelectedIconsFromDLL(
                    imageresPath,
                new List<int>() { Constants.ImageRes.Desktop },
                24);
                _LandscapeIconFileInfo = imageResList.First();
            }
            return _LandscapeIconFileInfo;
        }
        set
        {
            _LandscapeIconFileInfo = value;
            NotifyPropertyChanged(nameof(LandscapeIconFileInfo));
        }
    }

    IconFileInfo? _MonitorIconFileInfo;
    public IconFileInfo? MonitorIconFileInfo
    {
        get
        {
            if (_MonitorIconFileInfo is null)
            {
                var imageResList = Extensions.ExtractSelectedIconsFromDLL(
                    imageresPath,
                new List<int>() { Constants.ImageRes.CPUMonitor },
                24);
                _MonitorIconFileInfo = imageResList.First();
            }
            return _MonitorIconFileInfo;
        }
        set
        {
            _MonitorIconFileInfo = value;
            NotifyPropertyChanged(nameof(MonitorIconFileInfo));
        }
    }
    
    IconFileInfo? _SearchIconFileInfo;
    public IconFileInfo? SearchIconFileInfo
    {
        get
        {
            if (_SearchIconFileInfo is null)
            {
                var imageResList = Extensions.ExtractSelectedIconsFromDLL(
                    imageresPath,
                new List<int>() { Constants.ImageRes.Search },
                24);
                _SearchIconFileInfo = imageResList.First();
            }
            return _SearchIconFileInfo;
        }
        set
        {
            _SearchIconFileInfo = value;
            NotifyPropertyChanged(nameof(SearchIconFileInfo));
        }
    }
    public string imageresPath { get; private set; } = System.IO.Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
    public string shell32Path { get; private set; } = System.IO.Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "shell32.dll");

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (string.IsNullOrEmpty(propertyName))
            return;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public IconIndexItem SelectedIcon
    {
        get { return (IconIndexItem)GetValue(SelectedIconProperty); }
        set { SetValue(SelectedIconProperty, value); }
    }
    public static readonly DependencyProperty SelectedIconProperty = DependencyProperty.Register(
        nameof(SelectedIcon),
        typeof(IconIndexItem),
        typeof(MainPage),
        new PropertyMetadata(null));
    #endregion

    public ICommand TraverseCommand { get; }
    public ICommand TestCommand { get; }
    public ICommand AboutCommand { get; }

    public MainPage()
    {
        this.InitializeComponent();
        IconsRepeater.Loaded += ItemsGridViewOnLoaded;

        TraverseCommand = new RelayCommand<object>(async (obj) =>
        {
            //testPath = @"C:\Windows\SystemResources\shell32.dll.mun";
            var testPath = System.IO.Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", TargetDLL);
            if (!File.Exists(testPath))
            {
                Status = $"⚠️ DLL was not found";
                return;
            }

            StoryboardPath.Resume();
            IsBusy = true;

            IconItems.Clear();

            var request = Enumerable.Range(1, 3000).ToList();
            Status = $"🔔 Checking {request.Count} indices…";
            IList<IconFileInfo>? fullImageResList = null;
            var extraction = Task.Run(() =>
            {
                fullImageResList = Extensions.ExtractSelectedIconsFromDLL(testPath, request, 64);
            }).GetAwaiter();

            extraction.OnCompleted(() => 
            {
                if (fullImageResList != null)
                {
                    try
                    {
                        if (fullImageResList.Any())
                        {
                            int count = 0;
                            foreach (var img in fullImageResList)
                            {
                                if (!App.IsClosing && img is not null)
                                {
                                    count++;
                                    imgCycle?.DispatcherQueue.TryEnqueue(async () =>
                                    {
                                        var bmp = await img.IconData.ToBitmapAsync();
                                        if (bmp is not null)
                                        {
                                            ImgSource = (Microsoft.UI.Xaml.Media.ImageSource)bmp;
                                            Status = $"Found index #{img.Index}";
                                            IconItems.Add(new IconIndexItem { IconIndex = img.Index, IconImage = ImgSource });
                                            if (SaveToDisk)
                                            {
                                                try
                                                {   // NOTE: When extracting icon assets from a DLL, the UriSource does not exist.
                                                    // In an effort to make this feature a reality, I've created an "alternative" approach.
                                                    if (int.TryParse(TargetWidth, out int tw) && tw > 0 && int.TryParse(TargetHeight, out int th) && th > 0)
                                                        await BitmapHelper.SaveImageSourceToFileAsync(hostGrid, ImgSource, Path.Combine(AppContext.BaseDirectory, $"IconIndex{img.Index}.png"), tw, th);
                                                    else
                                                        await BitmapHelper.SaveImageSourceToFileAsync(hostGrid, ImgSource, Path.Combine(AppContext.BaseDirectory, $"IconIndex{img.Index}.png"), 32, 32);
                                                }
                                                catch (Exception ex) { Status = $"⚠️ Save: {ex.Message}"; }
                                            }
                                        }
                                    });
                                }
                            }
                            ShowMessage($"Process complete ⇒ {count} total icons", InfoBarSeverity.Informational);
                        }
                        else
                        {
                            Status = $"⚠️ DLL contained no usable icons";
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] {ex.Message}");
                        Status = $"[ERROR] {ex.Message}";
                    }
                    finally { IsBusy = false; }
                }
                else
                {
                    Status = $"⚠️ No results to show";
                }

                StoryboardPath.Pause();

                _ = DispatcherQueue.TryEnqueue(async () =>
                {
                    // Give the UI time to update before saving screen shot. 1ms is adequate, but
                    // I want plenty of time to pass so the temporary host grid image is no more.
                    await Task.Delay(500);
                    await UpdateScreenshot(App.MainRoot ?? hostPage, null);
                    if (SaveToDisk)
                    {
                        await App.ShowDialogBox("Assets", $"Icons have been saved to ⇒ {Environment.NewLine}{Environment.NewLine}{AppContext.BaseDirectory}", "OK", "", null, null, new Uri($"ms-appx:///Assets/Info.png"));
                    }
                });

            });
        });

        // Desktop wallpaper refresh.
        TestCommand = new RelayCommand<object>((obj) => 
        {
            // This was the initial scan that I performed to determine which DLLs contained icon assets.
            #region [Testing each DLL in System32]
            //var searchDir = System.IO.Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32");
            //if (Directory.Exists(searchDir))
            //{
            //    DirectoryInfo? searchDI = new DirectoryInfo(searchDir);
            //    FileInfo[]? files = searchDI?.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
            //    if (files != null)
            //    {
            //        StoryboardPath.Resume();
            //        IsBusy = true;
            //
            //        FileInfo? best = files.OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            //        foreach (var file in files)
            //        {
            //            var name = file.FullName;
            //            if (name.Contains("_"))
            //                continue;
            //
            //            var request = Enumerable.Range(1, 500).ToList();
            //            Status = $"🔔 Analyzing: {name} ({file.LastWriteTime})";
            //            IList<IconFileInfo>? fullImageResList = null;
            //            var extraction = Task.Run(() =>
            //            {
            //                fullImageResList = Extensions.ExtractSelectedIconsFromDLL(name, request, 64);
            //            }).GetAwaiter();
            //            extraction.OnCompleted(() =>
            //            {
            //                if (fullImageResList != null)
            //                {
            //                    try
            //                    {
            //                        if (fullImageResList.Any())
            //                        {
            //                            foreach (var img in fullImageResList)
            //                            {
            //                                if (img is not null)
            //                                {
            //                                    App.DebugLog($"Contains icons ⇒ {Path.GetFileName(name)}");
            //                                    break;
            //                                }
            //                            }
            //                        }
            //                        else
            //                        {
            //                            Status = $"⚠️ {name} contained no usable icons";
            //                        }
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Debug.WriteLine($"[ERROR] {ex.Message}");
            //                        Status = $"[ERROR] {ex.Message}";
            //                    }
            //                }
            //                else
            //                {
            //                    Status = $"⚠️ No results to show";
            //                }
            //            });
            //        }
            //
            //        IsBusy = false;
            //        StoryboardPath.Pause();
            //    }
            //}
            #endregion

            var imgPath = Path.Combine(AppContext.BaseDirectory, $"{App.GetCurrentNamespace()}Screenshot.png");
            if (File.Exists(imgPath))
            {
                // Changes wallpaper to latest screenshot.
                _ = App.ShowDialogBox(
                    "Wallpaper Change",
                    $"Are you sure you want to change your desktop wallpaper?{Environment.NewLine}{Environment.NewLine}{imgPath.Truncate(51)}",
                    "Yes",
                    "Cancel",
                    () => { _ = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imgPath, SPIF_UPDATEINIFILE); Status = "🔔 Wallpaper change accepted by user"; },
                    () => { Status = "🔔 Wallpaper change canceled by user"; },
                    new Uri($"ms-appx:///Assets/Notice.png"));
            }
            uint result = 99;
            _ = SystemParametersInfo(SPI_GETFASTTASKSWITCH, 0, ref result, SPIF_UPDATEINIFILE);
            Status = $"{(result == 0 ? "Alt-Tab task switching is disabled" : "Alt-Tab task switching is enabled")}";
        });

        // Dump assemblies.
        AboutCommand = new RelayCommand<object>(async (obj) =>
        {
            try
            {
                var data = Extensions.GatherLoadedModules(true);
                if (string.IsNullOrEmpty(data)) { return; }
                tbAssemblies.Text = data;
                contentDialog.XamlRoot = App.MainRoot?.XamlRoot;
                await contentDialog.ShowAsync();
            }
            catch (Exception ex) { Status = $"{ex.Message}"; }
        });
    }

    /// <summary>
    /// Apply a page's visual state to an <see cref="Microsoft.UI.Xaml.Controls.Image"/> source. 
    /// If the target is null then the image is saved to disk.
    /// </summary>
    /// <param name="root">host <see cref="Microsoft.UI.Xaml.UIElement"/>. Can be a grid, a page, etc.</param>
    /// <param name="target">optional <see cref="Microsoft.UI.Xaml.Controls.Image"/> target</param>
    /// <remarks>
    /// Using a RenderTargetBitmap, you can accomplish scenarios such as applying image effects to a visual that 
    /// originally came from a XAML UI composition, generating thumbnail images of child pages for a navigation 
    /// system, or enabling the user to save parts of the UI as an image source and then share that image with 
    /// other applications. 
    /// Because RenderTargetBitmap is a subclass of <see cref="Microsoft.UI.Xaml.Media.ImageSource"/>, 
    /// it can be used as the image source for <see cref="Microsoft.UI.Xaml.Controls.Image"/> elements or an 
    /// <see cref="Microsoft.UI.Xaml.Media.ImageBrush"/> brush. 
    /// Calling RenderAsync() provides a useful image source but the full buffer representation of rendering 
    /// content is not copied out of video memory until the app calls GetPixelsAsync().
    /// It is faster to call RenderAsync() only, without calling GetPixelsAsync, and use the RenderTargetBitmap as an 
    /// <see cref="Microsoft.UI.Xaml.Controls.Image"/> or <see cref="Microsoft.UI.Xaml.Media.ImageBrush"/> 
    /// source if the app only intends to display the rendered content and does not need the pixel data. 
    /// [Stipulations]
    ///  - Content that's in the tree but with its Visibility set to Collapsed won't be captured.
    ///  - Content that's not directly connected to the XAML visual tree and the content of the main window won't be captured. This includes Popup content, which is considered to be like a sub-window.
    ///  - Content that can't be captured will appear as blank in the captured image, but other content in the same visual tree can still be captured and will render (the presence of content that can't be captured won't invalidate the entire capture of that XAML composition).
    ///  - Content that's in the XAML visual tree but offscreen can be captured, so long as it's not Visibility = Collapsed.
    /// https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.rendertargetbitmap?view=winrt-22621
    /// </remarks>
    async Task UpdateScreenshot(Microsoft.UI.Xaml.UIElement root, Microsoft.UI.Xaml.Controls.Image? target)
    {
        Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new();
        await renderTargetBitmap.RenderAsync(root, App.m_width, App.m_height);
        if (target is not null)
        {
            // A render target bitmap is a viable ImageSource.
            imgCycle.Source = renderTargetBitmap;
        }
        else
        {
            // Convert RenderTargetBitmap to SoftwareBitmap
            IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();
            if (pixels.Length == 0 || renderTargetBitmap.PixelWidth == 0 || renderTargetBitmap.PixelHeight == 0)
            {
                Debug.WriteLine($"[ERROR] The width and height are not valid, cannot save.");
            }
            else
            {
                Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pixelBuffer);
                await softwareBitmap.SaveSoftwareBitmapToFileAsync(Path.Combine(AppContext.BaseDirectory, $"{App.GetCurrentNamespace()}Screenshot.png"), Windows.Graphics.Imaging.BitmapInterpolationMode.NearestNeighbor);
                softwareBitmap.Dispose();
            }
        }
    }

    async Task<RandomAccessStreamReference> GetRandomAccessStreamFromUIElement(UIElement? element)
    {
        Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new();
        InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
        // Render to an image at the current system scale and retrieve pixel contents
        await renderTargetBitmap.RenderAsync(element ?? hostPage);
        var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        // Encode image to an in-memory stream.
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            (uint)renderTargetBitmap.PixelWidth,
            (uint)renderTargetBitmap.PixelHeight,
            96d,
            96d,
            pixelBuffer.ToArray());
        await encoder.FlushAsync();
        // Set content to the encoded image in memory.
        return RandomAccessStreamReference.CreateFromStream(stream);
    }

    void ItemsGridViewOnLoaded(object sender, RoutedEventArgs e)
    {
        // Delegate loading of icons, so we have smooth navigating to
        // this page and do not unnecessarily block the UI thread.
        // On startup there won't be anything in the collection, but
        // in the event that you decide to load a large number of
        // items from disk, this will facilitate that process.
        Task.Run(delegate ()
        {
            _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                IconsRepeater.ItemsSource = IconItems;
            });
        });

        _loaded = true;
        TargetDLL = dlls[0];
        Status = "✔️ Ready";
        StoryboardPath.Begin();
        StoryboardPath.Pause();

        ShowMessage(ReflectAssemblyFramework(typeof(MainPage)), InfoBarSeverity.Informational);

        #region [Manipulatable Container Test]
        //Image img = new Image 
        //{
        //    Opacity = 0.8d,
        //    Width = 40,
        //    Height = 40,
        //    Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png", UriKind.RelativeOrAbsolute)), 
        //};
        //Button btn = new Button
        //{
        //    Width = 54,
        //    Height = 54,
        //    Padding = new Thickness(0),
        //    VerticalAlignment = VerticalAlignment.Bottom,
        //    HorizontalAlignment = HorizontalAlignment.Right,
        //    Content = img,
        //};
        //btn.Click += (_, _) => { Status = "you clicked me"; };
        //ToolTipService.SetToolTip(btn, "drag me around");
        //AddManipulatableElement(btn);
        #endregion
    }

    /// <summary>
    /// Makes an element manipulatable and adds it to the host grid.
    /// </summary>
    /// <param name="element"><see cref="UIElement"/></param>
    void AddManipulatableElement(UIElement element)
    {
        ManipulatableContainer container = new ManipulatableContainer();
        container.Content = element;
        hostGrid.Children.Add(container);
    }

    /// <summary>
    /// A BitmapImage can be sourced from these image file formats:
    /// - Joint Photographic Experts Group (JPEG)
    /// - Portable Network Graphics (PNG)
    /// - Bitmap (BMP)
    /// - Graphics Interchange Format (GIF)
    /// - Tagged Image File Format (TIFF)
    /// - JPEG XR
    /// - Icon (ICO)
    /// </summary>
    /// <remarks>
    /// If the image source is a stream, that stream is expected to contain an image file in one of these formats.
    /// The BitmapImage class represents an abstraction so that an image source can be set asynchronously but still 
    /// be referenced in XAML markup as a property value, or in code as an object that doesn't use awaitable syntax. 
    /// When you create a BitmapImage object in code, it initially has no valid source. You should then set its source 
    /// using one of these techniques:
    /// Use the BitmapImage(Uri) constructor rather than the default constructor. Although it's a constructor you can 
    /// think of this as having an implicit asynchronous behavior: the BitmapImage won't be ready for use until it 
    /// raises an ImageOpened event that indicates a successful async source set operation.
    /// Set the UriSource property. As with using the Uri constructor, this action is implicitly asynchronous, and the 
    /// BitmapImage won't be ready for use until it raises an ImageOpened event.
    /// Use SetSourceAsync. This method is explicitly asynchronous. The properties where you might use a BitmapImage, 
    /// such as Image.Source, are designed for this asynchronous behavior, and won't throw exceptions if they are set 
    /// using a BitmapImage that doesn't have a complete source yet. Rather than handling exceptions, you should handle 
    /// ImageOpened or ImageFailed events either on the BitmapImage directly or on the control that uses the source 
    /// (if those events are available on the control class).
    /// ImageFailed and ImageOpened are mutually exclusive. One event or the other will always be raised whenever a 
    /// BitmapImage object has its source value set or reset.
    /// The API for Image, BitmapImage and BitmapSource doesn't include any dedicated methods for encoding and decoding 
    /// of media formats. All of the encode and decode operations are built-in, and at most will surface aspects of 
    /// encode or decode as part of event data for load events. 
    /// If you want to do any special work with image encode or decode, which you might use if your app is doing image 
    /// conversions or manipulation, you should use the API that are available in the Windows.Graphics.Imaging namespace.
    /// </remarks>
    void TestImage_Loaded(object sender, RoutedEventArgs e)
    {
        Image? img = sender as Image;
        if (img is null)
            return;

        Microsoft.UI.Xaml.Media.Imaging.BitmapImage bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
        // TODO: Try -1 for DecodePixelWidth
        img.Width = bitmapImage.DecodePixelWidth = 80;
        // Natural px width of image source. You don't need to set DecodePixelHeight because
        // the system maintains aspect ratio, and calculates the other dimension, as long as
        // one dimension measurement is provided.
        bitmapImage.UriSource = new Uri(img.BaseUri, "Assets/StoreLogo.png");
        img.Source = bitmapImage;
    }

    void IconsOnTemplatePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var oldIndex = IconItems.IndexOf(SelectedIcon);
        var previousItem = IconsRepeater.TryGetElement(oldIndex);
        if (previousItem != null) { MoveToSelectionState(previousItem, false); }
        var itemIndex = IconsRepeater.GetElementIndex(sender as UIElement);
        SelectedIcon = IconItems[itemIndex != -1 ? itemIndex : 0];
        MoveToSelectionState(sender as UIElement, true);
    }

    /// <summary>
    /// Activate the proper VisualStateGroup for the control.
    /// </summary>
    static void MoveToSelectionState(UIElement previousItem, bool isSelected)
    {
        try { VisualStateManager.GoToState(previousItem as Control, isSelected ? "Selected" : "Default", false); }
        catch (NullReferenceException ex) { App.DebugLog($"[{previousItem.NameOf()}] {ex.Message}"); }
    }

    /// <summary>
    /// General helper method.
    /// </summary>
    IconFileInfo? LoadIconResource(int iconIndex)
    {
        string imageres = System.IO.Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
        var imageResList = Extensions.ExtractSelectedIconsFromDLL(
            imageres,
            new List<int>() { iconIndex },
            24);
        return imageResList.FirstOrDefault();
    }

    void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loaded)
        {
            try
            {
                var selection = e.AddedItems[0] as string;
                if (!string.IsNullOrEmpty(selection))
                {
                    TargetDLL = selection;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] SelectionChanged: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// <see cref="TextBox"/> event.
    /// </summary>
    void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var tb = sender as TextBox;
        tb?.SelectAll();
    }

    /// <summary>
    /// Thread-safe helper for <see cref="Microsoft.UI.Xaml.Controls.InfoBar"/>.
    /// </summary>
    /// <param name="message">text to show</param>
    /// <param name="severity"><see cref="Microsoft.UI.Xaml.Controls.InfoBarSeverity"/></param>
    public void ShowMessage(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        if (App.IsClosing)
            return;

        infoBar.DispatcherQueue?.TryEnqueue(() =>
        {
            infoBar.IsOpen = true;
            infoBar.Severity = severity;
            infoBar.Message = $"{message}";
        });
    }

    /// <summary>
    /// Reflective AssemblyInfo attributes
    /// </summary>
    public string ReflectAssemblyFramework(Type type)
    {
        try
        {
            System.Reflection.Assembly assembly = type.Assembly;
            if (assembly != null)
            {
                var fileVerAttr = (FileVersionAttribute)assembly.GetCustomAttributes(typeof(FileVersionAttribute), false)[0];
                var confAttr = (ConfigurationAttribute)assembly.GetCustomAttributes(typeof(ConfigurationAttribute), false)[0];
                var frameAttr = (TargetFrameworkAttribute)assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), false)[0];
                var compAttr = (CompanyAttribute)assembly.GetCustomAttributes(typeof(CompanyAttribute), false)[0];
                var nameAttr = (ProductAttribute)assembly.GetCustomAttributes(typeof(ProductAttribute), false)[0];
                var verAttr = (InformationalAttribute)assembly.GetCustomAttributes(typeof(InformationalAttribute), false)[0];
                return string.Format("{0} {1} {2} {3} – User '{4}' on {5}", nameAttr.Product, verAttr.InformationalVersion, string.IsNullOrEmpty(confAttr.Configuration) ? "–" : confAttr.Configuration, string.IsNullOrEmpty(frameAttr.FrameworkDisplayName) ? frameAttr.FrameworkName : frameAttr.FrameworkDisplayName, Environment.UserName, Environment.OSVersion);
            }
        }
        catch (Exception) { }
        return string.Empty;
    }

    #region [Win32 API]
    /// <summary>
    /// SystemParametersInfo reads or sets information about numerous settings in Windows.
    /// These include Windows's accessibility features as well as various settings for other things.
    /// The exact behavior of the function depends on the flag passed as uAction.
    /// All sizes and dimensions used by this function are measured in pixels.
    /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa
    /// </summary>
    /// <param name="uiAction">The exact behavior of the function depends on the SPI flag passed as uAction.</param>
    /// <param name="uiParam">The purpose of this parameter varies with uAction. </param>
    /// <param name="pvParam">The purpose of this parameter varies with uAction. In VB, if this is to be set as a string or to 0, the ByVal keyword must preceed it.</param>
    /// <param name="fWinIni">Zero or more of the following flags specifying the change notification to take place. Generally, this can be set to 0 if the function merely queries information, but should be set to something if the function sets information.
    ///   SPIF_SENDWININICHANGE = 0x02 (broadcast the change made by the function to all running programs)
    ///   SPIF_UPDATEINIFILE    = 0x01 (save the change made by the function to the user profile)
    /// </param>
    /// <returns></returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.I4)]
    static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.I4)]
    static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, ref UInt32 pvParam, UInt32 fWinIni);

    // Set the current desktop wallpaper bitmap. uiParam must be 0. pvParam is a String holding the filename of the bitmap file to use as the wallpaper. 
    static UInt32 SPI_SETDESKWALLPAPER = 20;
    // Determine if the warning beeper is on or off. uiParam must be 0. pvParam is a Long-type variable which receives 0 if the warning beeper is off, or a non-zero value if it is on. 
    static UInt32 SPI_GETBEEP = 1;
    // Determine if fast Alt-Tab task switching is enabled. uiParam must be 0. pvParam is a Long-type variable which receives 0 if fast task switching is not enabled, or a non-zero value if it is. 
    static UInt32 SPI_GETFASTTASKSWITCH = 35;
    // Determine whether font smoothing is enabled or not. uiParam must be 0. pvParam is a Long-type variable which receives 0 if font smoothing is not enabled, or a non-zero value if it is. 
    static UInt32 SPI_GETFONTSMOOTHING = 74;
    static UInt32 SPIF_UPDATEINIFILE = 0x1;
    static UInt32 SPIF_SENDWININICHANGE = 0x2;
    #endregion
}

public static class Functions
{
    public static string IdFormatter(int id) => $"Idx #{id}";
}