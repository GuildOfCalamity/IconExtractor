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

    IconFileInfo _ShieldIconFileInfo;
    public IconFileInfo ShieldIconFileInfo 
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

    IconFileInfo _MonitorIconFileInfo;
    public IconFileInfo MonitorIconFileInfo
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

            IsBusy = true;

            IconItems.Clear();

            var request = Enumerable.Range(1, 3000).ToList();
            Status = $"✔️ Checking {request.Count} indices...";
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
                                                    // In an effort to make this feature a reality, I've created a "alternative" approach.
                                                    await BitmapHelper.SaveImageSourceToFileAsync(hostGrid, ImgSource, Path.Combine(AppContext.BaseDirectory, $"IconIndex{img.Index}.png"));
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
            });
        });
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/imaging
    /// </summary>
    async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
    {
        using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
        {
            // Create an encoder with the desired format
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

            // Set the software bitmap
            encoder.SetSoftwareBitmap(softwareBitmap);

            // Set additional encoding parameters, if needed
            //encoder.BitmapTransform.ScaledWidth = 320;
            //encoder.BitmapTransform.ScaledHeight = 240;
            //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
            //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            //encoder.IsThumbnailGenerated = true;

            try
            {
                await encoder.FlushAsync();
            }
            catch (Exception ex)
            {
                const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                switch (ex.HResult)
                {
                    case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                        // If the encoder does not support writing a thumbnail, then try again
                        // but disable thumbnail generation.
                        encoder.IsThumbnailGenerated = false;
                        break;
                    default:
                        throw;
                }
            }

            if (encoder.IsThumbnailGenerated == false)
            {
                await encoder.FlushAsync();
            }
        }
    }

    void ItemsGridViewOnLoaded(object sender, RoutedEventArgs e)
    {
        // Delegate loading of icons, so we have smooth navigating to this page and do not unnecessarily block the UI thread.
        Task.Run(delegate ()
        {
            _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                IconsRepeater.ItemsSource = IconItems;
            });
        });

        _loaded = true;
        TargetDLL = dlls[0];
        Status = "✔️ System ready";
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
}

public static class Functions
{
    public static string IdFormatter(int id) => $"Idx #{id}";
}