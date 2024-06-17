using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using IconExtractor.Models;
using IconExtractor.Support;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Storage;
using System.Windows.Media.Media3D;
using static Vanara.PInvoke.User32;
using IconExtractor.Controls;

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
        "imageres.dll",
        "shell32.dll",
        "ddores.dll",
        "wmploc.dll",
        "pifmgr.dll",
        "accessibilitycpl.dll",
        "moricons.dll",
        "mmcndmgr.dll",
        "mmres.dll",
        "netcenter.dll",
        "netshell.dll",
        "networkexplorer.dll",
        "pnidui.dll",
        "sensorscpl.dll",
        "setupapi.dll",
        "wpdshext.dll",
        "compstui.dll",
        "ieframe.dll",
        "dmdskres.dll",
        "dsuiext.dll",
        "mstscax.dll",
        "wiashext.dll",
        "comres.dll",
        "actioncentercpl.dll",
        "aclui.dll",
        "autoplay.dll",
        "comctl32.dll",
        "filemgmt.dll",
        "ncpa.cpl",
        "url.dll",
        "xwizards.dll",
        "imagesp1.dll",
        "mstsc.exe",
        "explorer.exe"
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
    public string imageresPath { get; private set; }= System.IO.Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
    public string shell32Path { get; private set; }= System.IO.Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "shell32.dll");

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
            Status = $"🔔 Checking {request.Count} indices...";
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
                            foreach (var img in fullImageResList)
                            {
                                if (!App.IsClosing && img is not null)
                                {
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
                    // Give the UI time to update before saving screen shot.
                    // Even 1 ms seems adequate, but I'll use 1 frame worth (approx 33 ms).
                    await Task.Delay(30);
                    await UpdateScreenshot(App.MainRoot ?? hostPage, null);
                });

            });
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
                await BitmapHelper.SaveSoftwareBitmapToFileAsync(softwareBitmap, Path.Combine(AppContext.BaseDirectory, $"{App.GetCurrentNamespace()}Screenshot.png"));
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

    void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var tb = sender as TextBox;
        tb?.SelectAll();
    }
}

public static class Functions
{
    public static string IdFormatter(int id) => $"Idx #{id}";
}