using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IconExtractor.Support;

public static class Constants
{
    /// <summary>
    /// See imageres.dll for more icon indexes to add
    /// </summary>
    public static class ImageRes
    {
        public const int QuickAccess = 1024;
        public const int Desktop = 183;
        public const int Downloads = 184;
        public const int CPUMonitor = 150;
        public const int Documents = 112;
        public const int Pictures = 113;
        public const int Music = 108;
        public const int Videos = 189;
        public const int GenericDiskDrive = 35;
        public const int WindowsDrive = 36;
        public const int ThisPC = 109;
        public const int Network = 25;
        public const int RecycleBin = 55;
        public const int CloudDrives = 1040;
        public const int OneDrive = 1043;
        public const int Libraries = 1023;
        public const int Folder = 3;
        public const int ShieldIcon = 78;
        public const int Landscape = 72;
        public const int Search = 8;
    }

    /// <summary>
    /// See shell32.dll for more icon indexes to add
    /// </summary>
    public static class Shell32
    {
        public const int QuickAccess = 51380;
    }

    // Default icon sizes that are available for files and folders
    public static class ShellIconSizes
    {
        public const int Small = 16;
        public const int Normal = 24;
        public const int Large = 32;
        public const int ExtraLarge = 48;
        public const int Jumbo = 256;
    }

    public static class UserEnvironmentPaths
    {
        public static readonly string DesktopPath = Windows.Storage.UserDataPaths.GetDefault().Desktop;
        public static readonly string DownloadsPath = Windows.Storage.UserDataPaths.GetDefault().Downloads;
        public static readonly string LocalAppDataPath = Windows.Storage.UserDataPaths.GetDefault().LocalAppData;
        // Currently is the command to open the folder from cmd ("cmd /c start Shell:RecycleBinFolder")
        public const string RecycleBinPath = @"Shell:RecycleBinFolder";
        public const string NetworkFolderPath = @"Shell:NetworkPlacesFolder";
        public const string MyComputerPath = @"Shell:MyComputerFolder";
        public static readonly string TempPath = Environment.GetEnvironmentVariable("TEMP") ?? "";
        public static readonly string HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static readonly string SystemRootPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        public static readonly string RecentItemsPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
        public static Dictionary<string, string> ShellPlaces = new()
        {
            { "::{645FF040-5081-101B-9F08-00AA002F954E}", RecycleBinPath },
            { "::{5E5F29CE-E0A8-49D3-AF32-7A7BDC173478}", "Home" /*MyComputerPath*/ },
            { "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", MyComputerPath },
            { "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", NetworkFolderPath },
            { "::{208D2C60-3AEA-1069-A2D7-08002B30309D}", NetworkFolderPath },
            { RecycleBinPath.ToUpperInvariant(), RecycleBinPath },
            { MyComputerPath.ToUpperInvariant(), MyComputerPath },
            { NetworkFolderPath.ToUpperInvariant(), NetworkFolderPath },
        };
    }
}