using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Runtime.Versioning;
using System.Threading;

namespace IconExtractor.Support
{
    internal static class BitmapHelper
    {
        public static async Task<BitmapImage?> ToBitmapAsync(this byte[]? data, int decodeSize = -1)
        {
            if (data is null)
                return null;

            try
            {
                using var ms = new MemoryStream(data);
                var image = new BitmapImage();
                if (decodeSize > 0)
                {
                    image.DecodePixelWidth = decodeSize;
                    image.DecodePixelHeight = decodeSize;
                }
                image.DecodePixelType = DecodePixelType.Logical;
                await image.SetSourceAsync(ms.AsRandomAccessStream());
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Stream ToStream(this ImageSource imageSource)
        {
            switch (imageSource)
            {
                case BitmapImage bitmapImage:
                    {
                        var uri = bitmapImage.UriSource;

                        return uri.ToStream();
                    }

                default:
                    throw new NotImplementedException($"ImageSource type: {imageSource.GetType()} is not supported");
            }
        }

        public static async Task<Stream> ToStreamAsync(this ImageSource imageSource, CancellationToken cancellationToken = default)
        {
            switch (imageSource)
            {
                case BitmapImage bitmapImage:
                    {
                        var uri = bitmapImage.UriSource;

                        return await uri.ToStreamAsync(cancellationToken).ConfigureAwait(true);
                    }

                default:
                    throw new NotImplementedException($"ImageSource type: {imageSource.GetType()} is not supported");
            }
        }

        internal static Stream ToStream(this Uri uri)
        {
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


        #region [If using Drawing lib]
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
}
