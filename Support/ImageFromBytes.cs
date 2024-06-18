using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IconExtractor.Support;

/// <summary>
/// <example><code>
/// xmlns:helpers="using:IconExtractor.Support"
/// &lt;Image Width="24" Height="24" helpers:ImageFromBytes.SourceBytes="{x:Bind DataModelIconFileInfo.ByteData, Mode=OneWay}" /&gt;
/// </code></example>
/// </summary>
public sealed class ImageFromBytes : DependencyObject
{
    public static byte[] GetSourceBytes(DependencyObject obj)
    {
        return (byte[])obj.GetValue(SourceBytesProperty);
    }

    public static void SetSourceBytes(DependencyObject obj, byte[] value)
    {
        obj.SetValue(SourceBytesProperty, value);
    }

    public static readonly DependencyProperty SourceBytesProperty =
        DependencyProperty.RegisterAttached(
            "SourceBytes",
            typeof(byte[]),
            typeof(ImageFromBytes),
            new PropertyMetadata(null, OnSourceBytesChangedAsync));

    static async void OnSourceBytesChangedAsync(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Image image)
        {
            image.Source = await ((byte[])e.NewValue).ToBitmapAsync();
        }
    }
}
