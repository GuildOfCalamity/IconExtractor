using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IconExtractor.Models;

public class IconIndexItem
{
    public int IconIndex { get; set; }
    public Microsoft.UI.Xaml.Media.ImageSource? IconImage { get; set; }
}
