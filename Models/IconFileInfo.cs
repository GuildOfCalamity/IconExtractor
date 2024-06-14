using System;

namespace IconExtractor.Models;

public sealed class IconFileInfo
{
    public byte[] IconData { get; }

    public int Index { get; }

    public IconFileInfo(byte[] iconData, int index)
    {
        IconData = iconData;
        Index = index;
    }
}
