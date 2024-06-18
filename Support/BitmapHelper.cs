using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using System.Threading;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Diagnostics;

namespace IconExtractor.Support;

/// <summary>
/// Provides the practical object source type for the Image.Source and ImageBrush.ImageSource properties. 
/// You can define a BitmapImage by using a Uniform Resource Identifier (URI) that references an image 
/// source file, or by calling SetSourceAsync and supplying a stream.
/// https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.bitmapimage?view=winrt-22621
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
internal static class BitmapHelper
{
    public static async Task<Microsoft.UI.Xaml.Media.Imaging.BitmapImage?> ToBitmapAsync(this byte[]? data, int decodeSize = -1)
    {
        if (data is null)
            return null;

        try 
        {
            using var ms = new MemoryStream(data);
            var image = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            if (decodeSize > 0)
            {
                image.DecodePixelWidth = decodeSize;
                image.DecodePixelHeight = decodeSize;
            }
            image.DecodePixelType = Microsoft.UI.Xaml.Media.Imaging.DecodePixelType.Logical;
            await image.SetSourceAsync(ms.AsRandomAccessStream());
            return image;
        }
        catch (Exception ex) { 
            Debug.WriteLine($"[ERROR] ToBitmapAsync: {ex.Message}");
            return null; 
        }
    }

    public static Stream ToStream(this Microsoft.UI.Xaml.Media.ImageSource imageSource)
    {
        if (imageSource is null)
            throw new ArgumentNullException($"'{nameof(imageSource)}' cannot be null");

        switch (imageSource)
        {
            case Microsoft.UI.Xaml.Media.Imaging.BitmapImage bitmapImage:
                {
                    if (bitmapImage.UriSource is null)
                        throw new ArgumentNullException($"'{nameof(bitmapImage.UriSource)}' cannot be null");

                    var uri = bitmapImage.UriSource;
                    return uri.ToStream();
                }
            default:
                throw new NotImplementedException($"ImageSource type: {imageSource?.GetType()} is not supported");
        }
    }

    public static async Task<Stream> ToStreamAsync(this Microsoft.UI.Xaml.Media.ImageSource imageSource, CancellationToken cancellationToken = default)
    {
        if (imageSource is null)
            throw new ArgumentNullException($"'{nameof(imageSource)}' cannot be null");

        switch (imageSource)
        {
            case Microsoft.UI.Xaml.Media.Imaging.BitmapImage bitmapImage:
                {
                    if (bitmapImage.UriSource is null)
                        throw new ArgumentNullException($"'{nameof(bitmapImage.UriSource)}' cannot be null");

                    var uri = bitmapImage.UriSource;
                    return await uri.ToStreamAsync(cancellationToken).ConfigureAwait(true);
                }

            default:
                throw new NotImplementedException($"ImageSource type: {imageSource?.GetType()} is not supported");
        }
    }

    #region [New Technique]
    /// <summary>
    /// This was not trivial and proved to be a challenge.
    /// The main issue is the UriSource. Because we're extracting the asset from a DLL, the UriSource is null which immediately limits our options.
    /// I'm sure someone will correct my misadventure, but this works — and you can't argue with results.
    /// </summary>
    /// <param name="hostGrid"><see cref="Microsoft.UI.Xaml.Controls.Grid"/> to serve as the liaison.</param>
    /// <param name="imageSource"><see cref="Microsoft.UI.Xaml.Media.ImageSource"/> to save.</param>
    /// <param name="filePath">The full path to write the image.</param>
    /// <param name="width">16 to 256</param>
    /// <param name="height">16 to 256</param>
    /// <remarks>
    /// If the width or height is not correct the render target cannot be saved.
    /// The following types derive from <see cref="Microsoft.UI.Xaml.Media.ImageSource"/>:
    ///  - <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapSource"/>
    ///  - <see cref="Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"/>
    ///  - <see cref="Microsoft.UI.Xaml.Media.Imaging.SoftwareBitmapSource"/>
    ///  - <see cref="Microsoft.UI.Xaml.Media.Imaging.SurfaceImageSource"/>
    ///  - <see cref="Microsoft.UI.Xaml.Media.Imaging.SvgImageSource"/>
    /// </remarks>
    public static async Task SaveImageSourceToFileAsync(Microsoft.UI.Xaml.Controls.Grid hostGrid, Microsoft.UI.Xaml.Media.ImageSource imageSource, string filePath, int width = 32, int height = 32)
    {
        // Create an Image control to hold the ImageSource
        Microsoft.UI.Xaml.Controls.Image imageControl = new Microsoft.UI.Xaml.Controls.Image 
        { 
            Source = imageSource,
            Width = width,
            Height = height,
        };

        // NOTE: This is super clunky, but for some reason the Image resource is
        // never fully created if it's not appended to a rendered host control.
        // As a workaround we'll add the Image control to the host Grid. ┐( ˘_˘ )┌
        hostGrid.Children.Add(imageControl);

        // Wait for the image to be loaded and rendered
        await Task.Delay(50);

        // Render the Image control to a RenderTargetBitmap
        Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new();
        await renderTargetBitmap.RenderAsync(imageControl);

        // Convert RenderTargetBitmap to SoftwareBitmap
        IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        byte[] pixels = pixelBuffer.ToArray();

        // Remove the Image control from the host Grid
        hostGrid.Children.Remove(imageControl);

        try
        {
            if (pixels.Length == 0 || renderTargetBitmap.PixelWidth == 0 || renderTargetBitmap.PixelHeight == 0)
            {
                Debug.WriteLine($"[ERROR] The width and height are not a match for this asset. Try a different value other than {width},{height}.");
            }
            else
            {
                // NOTE: A SoftwareBitmap displayed in a XAML app must be in BGRA pixel format with pre-multiplied alpha values.
                Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(
                    Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, 
                    renderTargetBitmap.PixelWidth, 
                    renderTargetBitmap.PixelHeight, 
                    Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pixelBuffer);
                // Save SoftwareBitmap to file
                await softwareBitmap.SaveSoftwareBitmapToFileAsync(filePath, Windows.Graphics.Imaging.BitmapInterpolationMode.NearestNeighbor);
                softwareBitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] SaveImageSourceToFileAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Uses a <see cref="Windows.Graphics.Imaging.BitmapEncoder"/> to save the output.
    /// </summary>
    /// <param name="softwareBitmap"><see cref="Windows.Graphics.Imaging.SoftwareBitmap"/></param>
    /// <param name="filePath">output file path to save</param>
    /// <param name="interpolation">In general, moving from NearestNeighbor to Fant, interpolation quality increases while performance decreases.</param>
    /// <remarks>
    /// Assumes <see cref="Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId"/>.
    /// [Windows.Graphics.Imaging.BitmapInterpolationMode]
    /// 3 Fant...........: A Fant resampling algorithm. Destination pixel values are computed as a weighted average of the all the pixels that map to the new pixel in a box shaped kernel.
    /// 2 Cubic..........: A bicubic interpolation algorithm. Destination pixel values are computed as a weighted average of the nearest sixteen pixels in a 4x4 grid.
    /// 1 Linear.........: A bilinear interpolation algorithm. The output pixel values are computed as a weighted average of the nearest four pixels in a 2x2 grid.
    /// 0 NearestNeighbor: A nearest neighbor interpolation algorithm. Also known as nearest pixel or point interpolation. The output pixel is assigned the value of the pixel that the point falls within. No other pixels are considered.
    /// </remarks>
    public static async Task SaveSoftwareBitmapToFileAsync(this Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap, string filePath, Windows.Graphics.Imaging.BitmapInterpolationMode interpolation = Windows.Graphics.Imaging.BitmapInterpolationMode.Fant)
    {
        if (File.Exists(filePath))
        {
            Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                Windows.Graphics.Imaging.BitmapEncoder encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                encoder.BitmapTransform.InterpolationMode = interpolation;
                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] SaveSoftwareBitmapToFileAsync({ex.HResult}): {ex.Message}");
                }
            }
        }
        else
        {
            // Get the folder and file name from the file path.
            string? folderPath = System.IO.Path.GetDirectoryName(filePath);
            string? fileName = System.IO.Path.GetFileName(filePath);
            // Create the folder if it does not exist.
            Windows.Storage.StorageFolder storageFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                Windows.Graphics.Imaging.BitmapEncoder encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                encoder.BitmapTransform.InterpolationMode = interpolation;
                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] SaveSoftwareBitmapToFileAsync({ex.HResult}): {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/imaging
    /// </summary>
    static async void SaveSoftwareBitmapToFileExample(SoftwareBitmap softwareBitmap, StorageFile outputFile)
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

    /// <summary>
    /// The <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/> must contain a UriSource.
    /// </summary>
    /// <remarks>
    /// This has not been tested.
    /// </remarks>
    public static async Task TryBinaryWriter(this Microsoft.UI.Xaml.Media.Imaging.BitmapImage bitmapImage, string filePath)
    {
        if (bitmapImage.UriSource == null)
            return;

        Windows.Storage.Streams.RandomAccessStreamReference stream = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(bitmapImage.UriSource);
        Windows.Storage.Streams.IRandomAccessStreamWithContentType? streamContent = await stream.OpenReadAsync();
        byte[] buffer = new byte[streamContent.Size];
        await streamContent.ReadAsync(buffer.AsBuffer(), (uint)streamContent.Size, Windows.Storage.Streams.InputStreamOptions.None);
        using (BinaryWriter bw = new BinaryWriter(File.Open($"{filePath}.ico", FileMode.Create)))
        {
            foreach (byte b in buffer) { bw.Write(b); }
            bw.Flush();
        }
    }

    public static async Task SaveBitmapImageToFileAsyncAlt(Microsoft.UI.Xaml.Controls.Grid hostGrid, Microsoft.UI.Xaml.Media.Imaging.BitmapImage bitmapImage, string filePath, int width = 32, int height = 32)
    {
        // Create an Image control to hold the BitmapImage
        Microsoft.UI.Xaml.Controls.Image imageControl = new Microsoft.UI.Xaml.Controls.Image 
        { 
            Source = bitmapImage,
            Width = width,
            Height = height,
        };

        // NOTE: This is super clunky, but for some reason the Image resource is
        // never fully created if it's not appended to a rendered host control.
        // As a workaround we'll add the Image control to the host Grid. ┐( ˘_˘ )┌
        hostGrid.Children.Add(imageControl);

        // Wait for the image to be loaded
        await Task.Delay(50);

        // Render the Image control to a RenderTargetBitmap
        Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
        // This next line can throw an exception:
        await renderTargetBitmap.RenderAsync(imageControl);

        // Convert RenderTargetBitmap to SoftwareBitmap
        IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        byte[] pixels = pixelBuffer.ToArray();

        // NOTE: A SoftwareBitmap displayed in a XAML app must be in BGRA pixel format with pre-multiplied alpha values.
        Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, 
            renderTargetBitmap.PixelWidth, 
            renderTargetBitmap.PixelHeight, 
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);

        // Remove the Image control from the host Grid
        hostGrid.Children.Remove(imageControl);

        try
        {
            if (pixels.Length == 0 || renderTargetBitmap.PixelWidth == 0 || renderTargetBitmap.PixelHeight == 0)
            {
                Debug.WriteLine($"[ERROR] The width and height are not a match for this asset. Try a different value other than {width},{height}.");
            }
            else
            {
                softwareBitmap.CopyFromBuffer(pixelBuffer);
                // Save SoftwareBitmap to file
                await softwareBitmap.SaveSoftwareBitmapToFileAsync(filePath, Windows.Graphics.Imaging.BitmapInterpolationMode.Fant);
                softwareBitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] SaveBitmapImageToFileAsyncAlt: {ex.Message}");
        }
    }
    #endregion

    /// <summary>
    /// Generic loader for software bitmaps.
    /// </summary>
    /// <param name="filePath">Full path to asset.</param>
    /// <returns><see cref="Windows.Graphics.Imaging.SoftwareBitmap"/></returns>
    public static async Task<Windows.Graphics.Imaging.SoftwareBitmap>? LoadSoftwareBitmap(string filePath)
    {
        Windows.Graphics.Imaging.SoftwareBitmap? softwareBitmap;
        Windows.Storage.StorageFile inputFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
        using (Windows.Storage.Streams.IRandomAccessStream ras = await inputFile.OpenAsync(FileAccessMode.Read))
        {
            // Create the decoder from the stream
            Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ras);
            // Get the SoftwareBitmap representation of the file
            softwareBitmap = await decoder.GetSoftwareBitmapAsync();
        }
        return softwareBitmap;
    }

    /// <summary>
    /// The "other" BitmapImage, but we don't talk about him at the dinner table.
    /// </summary>
    public static bool SaveBitmapImage(this System.Windows.Media.Imaging.BitmapImage image, string filePath)
    {
        try
        {
            System.Windows.Media.Imaging.BitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] SaveBitmapImage: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Assumes PNG output via <see cref="Windows.Graphics.Imaging.BitmapEncoder"/>.
    /// </summary>
    /// <param name="bitmapImage"><see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/></param>
    /// <remarks>
    /// This assumes the <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/> contains 
    /// a UriSource, which will be used in conjunction with the ToStream() helper.
    /// </remarks>
    /// <returns><see cref="Windows.Graphics.Imaging.SoftwareBitmap"/></returns>
    public static async Task<Windows.Graphics.Imaging.SoftwareBitmap> GetSoftwareBitmapFromBitmapImageAsync(Microsoft.UI.Xaml.Media.Imaging.BitmapImage bitmapImage)
    {
        // Retrieve pixel data from the BitmapImage
        using (Windows.Storage.Streams.InMemoryRandomAccessStream stream = new())
        {
            Windows.Graphics.Imaging.BitmapEncoder encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
            Stream pixelStream = bitmapImage.ToStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);
            // NOTE: A SoftwareBitmap displayed in a XAML app must be in BGRA pixel format with pre-multiplied alpha values.
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();
            // Decode the image to a SoftwareBitmap
            Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            return await decoder.GetSoftwareBitmapAsync();
        }
    }

    #region [Internal Methods]
    internal static Stream ToStream(this Uri uri)
    {
        if (uri is null)
            throw new ArgumentNullException($"'{nameof(uri)}' cannot be null");

        var prefix = uri.Scheme switch
        {
            "ms-appx" or "ms-appx-web" => AppContext.BaseDirectory,
            _ => string.Empty,
        };
        // additional schemes, like ms-appdata could be added here
        // see: https://learn.microsoft.com/en-us/windows/uwp/app-resources/uri-schemes
        var absolutePath = $"{prefix}{uri.LocalPath}";

        return File.OpenRead(absolutePath);
    }

    internal static async Task<Stream> ToStreamAsync(this Uri uri, CancellationToken cancellationToken = default)
    {
        if (App.IsPackaged)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            return await file.OpenStreamForReadAsync().ConfigureAwait(true);
        }

        return uri.ToStream();
    }
    #endregion

    #region [If using System.Drawing lib]
    //public static Bitmap ToBitmap(this ImageSource imageSource)
    //{
    //    using var stream = imageSource.ToStream();
    //    return stream.ToBitmap();
    //}
    //
    //public static Icon ToIcon(this ImageSource imageSource)
    //{
    //    using var stream = imageSource.ToStream();
    //    return stream.ToSmallIcon();
    //}
    //
    //public static async Task<Bitmap> ToBitmapAsync(this ImageSource imageSource, CancellationToken cancellationToken = default)
    //{
    //    using var stream = await imageSource.ToStreamAsync(cancellationToken).ConfigureAwait(true);
    //    return stream.ToBitmap();
    //}
    //
    //public static async Task<Icon> ToIconAsync(this ImageSource imageSource, CancellationToken cancellationToken = default)
    //{
    //    using var stream = await imageSource.ToStreamAsync(cancellationToken).ConfigureAwait(true);
    //    return stream.ToSmallIcon();
    //}
    #endregion
}
