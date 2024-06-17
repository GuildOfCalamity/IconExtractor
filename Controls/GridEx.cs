using System;
using System.Threading.Tasks;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IconExtractor.Controls;

public class GridEx : Grid
{
    public GridEx()
    {
        this.Margin = new Thickness(0);
        this.Padding = new Thickness(0);
        this.PointerEntered += OnPointerEntered;
        this.PointerExited += OnPointerExited;
        //this.Tapped += GridExTapped;
    }


    #region [Events]
    void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => this.ProtectedCursor = null;
    async void GridExTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Wait);
        await Task.Delay(TimeSpan.FromSeconds(1));
        this.ProtectedCursor = null;
    }
    #endregion
}
