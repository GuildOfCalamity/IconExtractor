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

internal static class BitmapHelper
{
    public static async Task<Microsoft.UI.Xaml.Media.Imaging.BitmapImage?> ToBitmapAsync(this byte[]? data, int decodeSize = -1)
    {
        if (data is null)
            return null;

        try {
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
    /// I'm sure someone will correct my misadventure, but this works — and you can't argue with results.
    /// </summary>
    /// <param name="hostGrid"><see cref="Microsoft.UI.Xaml.Controls.Grid"/> to serve as the liaison.</param>
    /// <param name="imageSource"><see cref="Microsoft.UI.Xaml.Media.ImageSource"/> to save.</param>
    /// <param name="filePath">The full path to write the image.</param>
    /// <param name="width">16 to 256</param>
    /// <param name="height">16 to 256</param>
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
        // As a workaround we'll add the Image control to the host Grid.
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

        Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
        softwareBitmap.CopyFromBuffer(pixelBuffer);

        // Save SoftwareBitmap to file
        await SaveSoftwareBitmapToFileAsync(softwareBitmap, filePath);
    }

    /// <summary>
    /// Uses a <see cref="Windows.Graphics.Imaging.BitmapEncoder"/> to save the output.
    /// </summary>
    /// <remarks>
    /// Assumes <see cref="Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId"/>.
    /// </remarks>
    static async Task SaveSoftwareBitmapToFileAsync(Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap, string filePath)
    {
        if (File.Exists(filePath))
        {
            Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                Windows.Graphics.Imaging.BitmapEncoder encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();
            }
        }
        else
        {
            // Get the folder and file name from the file path
            string? folderPath = System.IO.Path.GetDirectoryName(filePath);
            string? fileName = System.IO.Path.GetFileName(filePath);
            // Create the folder if it does not exist
            Windows.Storage.StorageFolder storageFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                Windows.Graphics.Imaging.BitmapEncoder encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
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
        // As a workaround we'll add the Image control to the host Grid.
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

        // Configure the software bitmap
        Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, 
            renderTargetBitmap.PixelWidth, 
            renderTargetBitmap.PixelHeight, 
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);

        // Remove the Image control from the host Grid
        hostGrid.Children.Remove(imageControl);

        softwareBitmap.CopyFromBuffer(pixelBuffer);

        // Save SoftwareBitmap to file
        await SaveSoftwareBitmapToFileAsync(softwareBitmap, filePath);
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
