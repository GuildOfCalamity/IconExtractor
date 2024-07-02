using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;

using Vanara.PInvoke;
using IconExtractor.Models;

namespace IconExtractor;

public static class Extensions
{
    private static readonly ConcurrentDictionary<(string File, int Index, int Size), IconFileInfo> _iconCache = new();
    public static IList<IconFileInfo> ExtractSelectedIconsFromDLL(string file, IList<int> indexes, int iconSize = 48)
    {
        var iconsList = new List<IconFileInfo>();

        foreach (int index in indexes)
        {
            if (_iconCache.TryGetValue((file, index, iconSize), out var iconInfo))
            {
                iconsList.Add(iconInfo);
            }
            else
            {
                // This is merely to pass into the function and is unneeded otherwise
                if (Shell32.SHDefExtractIcon(file, -1 * index, 0, out User32.SafeHICON icon, out User32.SafeHICON hIcon2, Convert.ToUInt32(iconSize)) == HRESULT.S_OK)
                {
                    try
                    {
                        using var image = icon.ToBitmap();
                        byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
                        iconInfo = new IconFileInfo(bitmapData, index);
                        _iconCache[(file, index, iconSize)] = iconInfo;
                        iconsList.Add(iconInfo);
                        User32.DestroyIcon(icon);
                        User32.DestroyIcon(hIcon2);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] ExtractSelectedIconsFromDLL: {ex.Message}");
                    }
                }
            }
        }

        return iconsList;
    }

    public static IList<IconFileInfo>? ExtractIconsFromDLL(string file)
    {
        var iconsList = new List<IconFileInfo>();
        using var currentProc = Process.GetCurrentProcess();

        using var icoCnt = Shell32.ExtractIcon(currentProc.Handle, file, -1);
        if (icoCnt is null)
            return null;

        int count = icoCnt.DangerousGetHandle().ToInt32();
        if (count <= 0)
            return null;

        for (int i = 0; i < count; i++)
        {
            if (_iconCache.TryGetValue((file, i, -1), out var iconInfo))
            {
                iconsList.Add(iconInfo);
            }
            else
            {
                try
                {
                    using var icon = Shell32.ExtractIcon(currentProc.Handle, file, i);
                    using var image = icon.ToBitmap();

                    byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
                    iconInfo = new IconFileInfo(bitmapData, i);
                    _iconCache[(file, i, -1)] = iconInfo;
                    iconsList.Add(iconInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] ExtractIconsFromDLL: {ex.Message}");
                }
            }
        }

        return iconsList;
    }

    /// <summary>
    /// This should only be used on instantiated objects, not static objects.
    /// </summary>
    public static string ToStringDump<T>(this T obj)
    {
        const string Seperator = "\r\n";
        const System.Reflection.BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;

        if (obj is null)
            return string.Empty;

        try
        {
            var objProperties =
                from property in obj?.GetType().GetProperties(BindingFlags)
                where property.CanRead
                select string.Format("{0} : {1}", property.Name, property.GetValue(obj, null));

            return string.Join(Seperator, objProperties);
        }
        catch (Exception ex)
        {
            return $"⇒ Probably a non-instanced object: {ex.Message}";
        }
    }

    /// <summary>
    /// var stack = GeneralExtensions.GetStackTrace(new StackTrace());
    /// </summary>
    public static string GetStackTrace(StackTrace st)
    {
        string result = string.Empty;
        for (int i = 0; i < st.FrameCount; i++)
        {
            StackFrame? sf = st.GetFrame(i);
            result += sf?.GetMethod() + " <== ";
        }
        return result;
    }

    public static string Flatten(this Exception? exception)
    {
        var sb = new StringBuilder();
        while (exception != null)
        {
            sb.AppendLine(exception.Message);
            sb.AppendLine(exception.StackTrace);
            exception = exception.InnerException;
        }
        return sb.ToString();
    }

    public static string DumpFrames(this Exception exception)
    {
        var sb = new StringBuilder();
        var st = new StackTrace(exception, true);
        var frames = st.GetFrames();
        foreach (var frame in frames)
        {
            if (frame != null)
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;

                sb.Append($"File: {frame.GetFileName()}")
                  .Append($", Method: {frame.GetMethod()?.Name}")
                  .Append($", LineNumber: {frame.GetFileLineNumber()}")
                  .Append($"{Environment.NewLine}");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Helper for parsing command line arguments.
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns>string array of args excluding the 1st arg</returns>
    public static string[] IgnoreFirstTakeRest(this string[] inputArray)
    {
        if (inputArray.Length > 1)
            return inputArray.Skip(1).ToArray();
        else
            return Array.Empty<string>();
    }

    /// <summary>
    /// Helper for parsing command line arguments.
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns>string array of args excluding the 1st arg</returns>
    public static string[] IgnoreNthTakeRest(this string[] inputArray, int skip = 1)
    {
        if (inputArray.Length > 1)
            return inputArray.Skip(skip).ToArray();
        else
            return Array.Empty<string>();
    }

    /// <summary>
    /// Returns the first element from a tokenized string, e.g.
    /// Input:"{tag}"  Output:"tag"
    /// </summary>
    /// <example>
    /// var clean = ExtractFirst("{tag}", '{', '}');
    /// </example>
    public static string ExtractFirst(this string text, char start, char end)
    {
        string pattern = @"\" + start + "(.*?)" + @"\" + end; //pattern = @"\{(.*?)\}"
        Match match = Regex.Match(text, pattern);
        if (match.Success)
            return match.Groups[1].Value;
        else
            return "";
    }

    /// <summary>
    /// Returns the last element from a tokenized string, e.g.
    /// Input:"{tag}"  Output:"tag"
    /// </summary>
    /// <example>
    /// var clean = ExtractLast("{tag}", '{', '}');
    /// </example>
    public static string ExtractLast(this string text, char start, char end)
    {
        string pattern = @"\" + start + @"(.*?)\" + end; //pattern = @"\{(.*?)\}"
        MatchCollection matches = Regex.Matches(text, pattern);
        if (matches.Count > 0)
        {
            Match lastMatch = matches[matches.Count - 1];
            return lastMatch.Groups[1].Value;
        }
        else
            return "";
    }

    /// <summary>
    /// Returns all the elements from a tokenized string, e.g.
    /// Input:"{tag}"  Output:"tag"
    /// </summary>
    public static string[] ExtractAll(this string text, char start, char end)
    {
        string pattern = @"\" + start + @"(.*?)\" + end; //pattern = @"\{(.*?)\}"
        MatchCollection matches = Regex.Matches(text, pattern);
        string[] results = new string[matches.Count];
        for (int i = 0; i < matches.Count; i++)
            results[i] = matches[i].Groups[1].Value;

        return results;
    }

    /// <summary>
    /// An updated string truncation helper.
    /// </summary>
    /// <remarks>
    /// This can be helpful when the CharacterEllipsis TextTrimming Property is not available.
    /// </remarks>
    public static string Truncate(this string text, int maxLength, string mesial = "…")
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (maxLength > 0 && text.Length > maxLength)
        {
            var limit = maxLength / 2;
            if (limit > 1)
            {
                return String.Format("{0}{1}{2}", text.Substring(0, limit).Trim(), mesial, text.Substring(text.Length - limit).Trim());
            }
            else
            {
                var tmp = text.Length <= maxLength ? text : text.Substring(0, maxLength).Trim();
                return String.Format("{0}{1}", tmp, mesial);
            }
        }
        return text;
    }

    /// <summary>
    /// Fetch all <see cref="ProcessModule"/>s in the current running process.
    /// </summary>
    /// <param name="excludeWinSys">if true any file path starting with %windir% will be excluded from the results</param>
    public static string GatherLoadedModules(bool excludeWinSys)
    {
        var modules = new StringBuilder();
        // Setup some common library paths if exclude option is desired.
        var winSys = Environment.GetFolderPath(Environment.SpecialFolder.Windows) ?? "N/A";
        var winProg = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) ?? "N/A";
        try
        {
            var process = Process.GetCurrentProcess();
            foreach (ProcessModule module in process.Modules)
            {
                var fn = module.FileName ?? "Empty";
                if (excludeWinSys && !fn.StartsWith(winSys, StringComparison.OrdinalIgnoreCase) && !fn.StartsWith(winProg, StringComparison.OrdinalIgnoreCase))
                    modules.AppendLine($"{System.IO.Path.GetFileName(fn)} (v{GetFileVersion(fn)})");
                else if (!excludeWinSys)
                    modules.AppendLine($"{System.IO.Path.GetFileName(fn)} (v{GetFileVersion(fn)})");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GatherLoadedModules: {ex.Message}");
        }
        return modules.ToString();
    }

    /// <summary>
    /// Brute force alpha removal of <see cref="Version"/> text
    /// is not always the best approach, e.g. the following:
    /// "3.0.0-zmain.2211 (DCPP(199ff10ec000000)(cloudtest).160101.0800)"
    /// ...converts to:
    /// "3.0.0.221119910000000.160101.0800"
    /// ...which is not accurate.
    /// </summary>
    /// <param name="fullPath">the entire path to the file</param>
    /// <returns>sanitized <see cref="Version"/></returns>
    public static Version GetFileVersion(string fullPath)
    {
        try
        {
            var ver = FileVersionInfo.GetVersionInfo(fullPath).FileVersion;
            if (string.IsNullOrEmpty(ver)) { return new Version(); }
            if (ver.HasSpace())
            {   // Some assemblies contain versions such as "10.0.22622.1030 (WinBuild.160101.0800)"
                // This will cause the Version constructor to throw an exception, so just take the first piece.
                var chunk = ver.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstPiece = Regex.Replace(chunk[0].Replace(',', '.'), "[^.0-9]", "");
                return new Version(firstPiece);
            }
            string cleanVersion = Regex.Replace(ver, "[^.0-9]", "");
            return new Version(cleanVersion);
        }
        catch (Exception)
        {
            return new Version(); // 0.0
        }
    }

    public static bool HasAlpha(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsLetter(x));
    }
    public static bool HasAlphaRegex(this string str)
    {
        return Regex.IsMatch(str ?? "", @"[+a-zA-Z]+");
    }

    public static bool HasNumeric(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsNumber(x));
    }
    public static bool HasNumericRegex(this string str)
    {
        return Regex.IsMatch(str ?? "", @"[0-9]+"); // [^\D+]
    }

    public static bool HasSpace(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsSeparator(x));
    }
    public static bool HasSpaceRegex(this string str)
    {
        return Regex.IsMatch(str ?? "", @"[\s]+");
    }

    public static bool HasPunctuation(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsPunctuation(x));
    }

    public static bool HasAlphaNumeric(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsNumber(x)) && str.Any(x => char.IsLetter(x));
    }
    public static bool HasAlphaNumericRegex(this string str)
    {
        return Regex.IsMatch(str ?? "", "[a-zA-Z0-9]+");
    }

    public static string RemoveAlphas(this string str)
    {
        return string.Concat(str?.Where(c => char.IsNumber(c) || c == '.') ?? string.Empty);
    }

    public static string RemoveNumerics(this string str)
    {
        return string.Concat(str?.Where(c => char.IsLetter(c)) ?? string.Empty);
    }

    public static string RemoveExtraSpaces(this string strText)
    {
        if (!string.IsNullOrEmpty(strText))
            strText = Regex.Replace(strText, @"\s+", " ");

        return strText;
    }

    /// <summary>
    /// String normalize helper.
    /// </summary>
    /// <param name="strThis"></param>
    /// <returns>sanitized string</returns>
    public static string? RemoveDiacritics(this string strThis)
    {
        if (strThis == null)
            return null;

        var sb = new StringBuilder();

        foreach (char c in strThis.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// ExampleTextSample => Example Text Sample
    /// </summary>
    /// <param name="input"></param>
    /// <returns>space delimited string</returns>
    public static string SeparateCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder result = new StringBuilder();
        result.Append(input[0]);

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
                result.Append(' ');

            result.Append(input[i]);
        }

        return result.ToString();
    }

    public static string NameOf(this object obj) => $"{obj.GetType().Name} => {obj.GetType().BaseType?.Name}";
    public static int MapValue(this int val, int inMin, int inMax, int outMin, int outMax) => (val - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
    public static float MapValue(this float val, float inMin, float inMax, float outMin, float outMax) => (val - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
    public static double MapValue(this double val, double inMin, double inMax, double outMin, double outMax) => (val - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;

    /// <summary>
    /// Multiplies the given <see cref="TimeSpan"/> by the scalar amount provided.
    /// </summary>
    public static TimeSpan Multiply(this TimeSpan timeSpan, double scalar) => new TimeSpan((long)(timeSpan.Ticks * scalar));

    /// <summary>
    /// Returns the AppData path including the <paramref name="moduleName"/>.
    /// e.g. "C:\Users\UserName\AppData\Local\MenuDemo\Settings"
    /// </summary>
    public static string LocalApplicationDataFolder(string moduleName = "Settings")
    {
        var result = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}\\{moduleName}");
        return result;
    }

    /// <summary>
    /// Use this if you only have a root resource dictionary.
    /// var rdBrush = Extensions.GetResource<SolidColorBrush>("PrimaryBrush");
    /// </summary>
    public static T? GetResource<T>(string resourceName) where T : class
    {
        try
        {
            if (Application.Current.Resources.TryGetValue($"{resourceName}", out object value))
                return (T)value;
            else
                return default(T);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Use this if you have merged theme resource dictionaries.
    /// var darkBrush = Extensions.GetThemeResource<SolidColorBrush>("PrimaryBrush", ElementTheme.Dark);
    /// var lightBrush = Extensions.GetThemeResource<SolidColorBrush>("PrimaryBrush", ElementTheme.Light);
    /// </summary>
    public static T? GetThemeResource<T>(string resourceName, ElementTheme? theme) where T : class
    {
        try
        {
            if (theme == null) { theme = ElementTheme.Default; }
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            foreach (var item in dictionaries)
            {
                // Do we have any themes in this resource dictionary?
                if (item.ThemeDictionaries.Count > 0)
                {
                    if (theme == ElementTheme.Dark)
                    {
                        if (item.ThemeDictionaries.TryGetValue("Dark", out var drd))
                        {
                            ResourceDictionary? dark = drd as ResourceDictionary;
                            if (dark != null)
                            {
                                Debug.WriteLine($"[INFO] Found dark theme resource dictionary.");
                                if (dark.TryGetValue($"{resourceName}", out var tmp))
                                    return (T)tmp;
                            }
                        }
                        else { Debug.WriteLine($"[WARNING] {nameof(ElementTheme.Dark)} theme was not found."); }
                    }
                    else if (theme == ElementTheme.Light)
                    {
                        if (item.ThemeDictionaries.TryGetValue("Light", out var lrd))
                        {
                            ResourceDictionary? light = lrd as ResourceDictionary;
                            if (light != null)
                            {
                                Debug.WriteLine($"[INFO] Found light theme resource dictionary.");
                                if (light.TryGetValue($"{resourceName}", out var tmp))
                                    return (T)tmp;
                            }
                        }
                        else { Debug.WriteLine($"[WARNING] {nameof(ElementTheme.Light)} theme was not found."); }
                    }
                    else if (theme == ElementTheme.Default)
                    {
                        if (item.ThemeDictionaries.TryGetValue("Default", out var drd))
                        {
                            ResourceDictionary? dflt = drd as ResourceDictionary;
                            if (dflt != null)
                            {
                                Debug.WriteLine($"[INFO] Found default theme resource dictionary.");
                                if (dflt.TryGetValue($"{resourceName}", out var tmp))
                                    return (T)tmp;
                            }
                        }
                        else { Debug.WriteLine($"[WARNING] {nameof(ElementTheme.Default)} theme was not found."); }
                    }
                    else
                    {
                        Debug.WriteLine($"[WARNING] No theme to match.");
                    }
                }
                else
                {
                    Debug.WriteLine($"[WARNING] No theme dictionaries found.");
                }
            }
            return default(T);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static IconElement? GetIcon(string imagePath)
    {
        IconElement? result = null;

        try
        {
            result = imagePath.ToLowerInvariant().EndsWith(".png") ?
                        (IconElement)new BitmapIcon() { UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute), ShowAsMonochrome = false } :
                        (IconElement)new FontIcon() { Glyph = imagePath };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetIcon: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// int cvrt = (int)FluentIcon.MapPin;
    /// string icon = IntToUTF16(cvrt);
    /// https://stackoverflow.com/questions/71546789/the-u-escape-sequence-in-c-sharp
    /// </summary>
    public static string IntToUTF16(int value)
    {
        var builder = new StringBuilder();
        builder.Append((char)value);
        return builder.ToString();
    }

    public static async Task<SoftwareBitmap> LoadFromFile(StorageFile file)
    {
        SoftwareBitmap softwareBitmap;
        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
        }
        return softwareBitmap;
    }

    public static async Task<string> LoadText(string relativeFilePath)
    {
#if IS_UNPACKAGED
        var sourcePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location ?? System.IO.Directory.GetCurrentDirectory()), relativeFilePath));
        var file = await StorageFile.GetFileFromPathAsync(sourcePath);
#else
        Uri sourceUri = new Uri("ms-appx:///" + relativeFilePath);
        var file = await StorageFile.GetFileFromApplicationUriAsync(sourceUri);
#endif
        return await FileIO.ReadTextAsync(file);
    }

    public static async Task<IList<string>> LoadLines(string relativeFilePath)
    {
        string fileContents = await LoadText(relativeFilePath);
        return fileContents.Split(Environment.NewLine).ToList();
    }

    /// <summary>
    /// Creates a Windows Runtime asynchronous operation that returns the last element of the observable sequence.
    /// Upon cancellation of the asynchronous operation, the subscription to the source sequence will be disposed.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">Source sequence to expose as an asynchronous operation.</param>
    /// <returns>Windows Runtime asynchronous operation object that returns the last element of the observable sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static IAsyncOperation<TSource> ToAsyncOperation<TSource>(this IObservable<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return AsyncInfo.Run(ct => source.ToTask(ct));
    }

    /// <summary>
    /// Returns a task that will receive the last value or the exception produced by the observable sequence.
    /// </summary>
    /// <typeparam name="TResult">The type of the elements in the source sequence.</typeparam>
    /// <param name="observable">Observable sequence to convert to a task.</param>
    /// <returns>A task that will receive the last element or the exception produced by the observable sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="observable"/> is <c>null</c>.</exception>
    public static Task<TResult> ToTask<TResult>(this IObservable<TResult> observable)
    {
        if (observable == null)
            throw new ArgumentNullException(nameof(observable));

        return observable.ToTask(new CancellationToken());
    }

    /// <summary>
    /// Returns a task that will receive the last value or the exception produced by the observable sequence.
    /// </summary>
    /// <typeparam name="TResult">The type of the elements in the source sequence.</typeparam>
    /// <param name="observable">Observable sequence to convert to a task.</param>
    /// <param name="state">The state to use as the underlying task's AsyncState.</param>
    /// <returns>A task that will receive the last element or the exception produced by the observable sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="observable"/> is <c>null</c>.</exception>
    public static Task<TResult> ToTask<TResult>(this IObservable<TResult> observable, object? state)
    {
        if (observable == null)
        {
            throw new ArgumentNullException(nameof(observable));
        }

        return observable.ToTask(new CancellationToken());
    }

    /// <summary>
    /// Returns a task that will receive the last value or the exception produced by the observable sequence.
    /// </summary>
    /// <typeparam name="TResult">The type of the elements in the source sequence.</typeparam>
    /// <param name="observable">Observable sequence to convert to a task.</param>
    /// <param name="cancellationToken">Cancellation token that can be used to cancel the task, causing unsubscription from the observable sequence.</param>
    /// <returns>A task that will receive the last element or the exception produced by the observable sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="observable"/> is <c>null</c>.</exception>
    public static Task<TResult> ToTask<TResult>(this IObservable<TResult> observable, CancellationToken cancellationToken)
    {
        if (observable == null)
        {
            throw new ArgumentNullException(nameof(observable));
        }

        return observable.ToTask(cancellationToken);
    }

    /// <summary>
    /// ToTask Helper Extension
    /// ((Func<double, double, double>)Math.Pow).ToTask(2d, 2d).ContinueWith(x => ((Action<string, object[]>) Console.WriteLine).ToTask("Power value: {0}", new object[] { x.Result })).Wait();
    /// </summary>
    public static Task<TResult> ToTask<TResult>(this Func<TResult> function, AsyncCallback? callback = default(AsyncCallback), object? @object = default(object), TaskCreationOptions creationOptions = default(TaskCreationOptions), TaskScheduler? scheduler = default(TaskScheduler))
    {
        return Task<TResult>.Factory.FromAsync(function.BeginInvoke(callback, @object), function.EndInvoke, creationOptions, (scheduler ?? TaskScheduler.Current) ?? TaskScheduler.Default);
    }
    public static Task<TResult> ToTask<T, TResult>(this Func<T, TResult> function, T arg, AsyncCallback? callback = default(AsyncCallback), object @object = default(object), TaskCreationOptions creationOptions = default(TaskCreationOptions), TaskScheduler? scheduler = default(TaskScheduler))
    {
        return Task<TResult>.Factory.FromAsync(function.BeginInvoke(arg, callback, @object), function.EndInvoke, creationOptions, (scheduler ?? TaskScheduler.Current) ?? TaskScheduler.Default);
    }

    /// <summary>
    /// Task extension to add a timeout.
    /// </summary>
    /// <returns>The task with timeout.</returns>
    /// <param name="task">Task.</param>
    /// <param name="timeoutInMilliseconds">Timeout duration in Milliseconds.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public async static Task<T> WithTimeout<T>(this Task<T> task, int timeoutInMilliseconds)
    {
        var retTask = await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds))
            .ConfigureAwait(false);

#pragma warning disable CS8603 // Possible null reference return.
        return retTask is Task<T> ? task.Result : default;
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    /// Task extension to add a timeout.
    /// </summary>
    /// <returns>The task with timeout.</returns>
    /// <param name="task">Task.</param>
    /// <param name="timeout">Timeout Duration.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout) => WithTimeout(task, (int)timeout.TotalMilliseconds);

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
    /// <summary>
    /// Attempts to await on the task and catches exception
    /// </summary>
    /// <param name="task">Task to execute</param>
    /// <param name="onException">What to do when method has an exception</param>
    /// <param name="continueOnCapturedContext">If the context should be captured.</param>
    public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null, bool continueOnCapturedContext = false)
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex) when (onException != null)
        {
            onException.Invoke(ex);
        }
    }

    /// <summary>
    /// Chainable task helper.
    /// var result = await SomeLongAsyncFunction().WithCancellation(cts.Token);
    /// </summary>
    /// <typeparam name="TResult">the type of task result</typeparam>
    /// <returns><see cref="Task"/>TResult</returns>
    public static Task<TResult> WithCancellation<TResult>(this Task<TResult> task, CancellationToken cancelToken)
    {
        TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
        CancellationTokenRegistration reg = cancelToken.Register(() => tcs.TrySetCanceled());
        task.ContinueWith(ant =>
        {
            reg.Dispose(); // NOTE: it's important to dispose of CancellationTokenRegistrations or they will hand around in memory until the application closes
            if (ant.IsCanceled)
                tcs.TrySetCanceled();
            else if (ant.IsFaulted)
                tcs.TrySetException(ant.Exception.InnerException);
            else
                tcs.TrySetResult(ant.Result);
        });
        return tcs.Task;  // Return the TaskCompletionSource result
    }

    public static Task<T> WithAllExceptions<T>(this Task<T> task)
    {
        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        task.ContinueWith(ignored =>
        {
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    Debug.WriteLine($"[TaskStatus.Canceled]");
                    tcs.SetCanceled();
                    break;
                case TaskStatus.RanToCompletion:
                    tcs.SetResult(task.Result);
                    Debug.WriteLine($"[TaskStatus.RanToCompletion]: {task.Result}");
                    break;
                case TaskStatus.Faulted:
                    // SetException will automatically wrap the original AggregateException in another
                    // one. The new wrapper will be removed in TaskAwaiter, leaving the original intact.
                    Debug.WriteLine($"[TaskStatus.Faulted]: {task.Exception?.Message}");
                    tcs.SetException(task.Exception ?? new Exception("Exception object was null"));
                    break;
                default:
                    Debug.WriteLine($"[TaskStatus.Invalid]: Continuation called illegally.");
                    tcs.SetException(new InvalidOperationException("Continuation called illegally."));
                    break;
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Task.Factory.StartNew (() => { throw null; }).IgnoreExceptions();
    /// </summary>
    public static void IgnoreExceptions(this Task task, bool logEx = false)
    {
        task.ContinueWith(t =>
        {
            AggregateException ignore = t.Exception;

            ignore?.Flatten().Handle(ex =>
            {
                if (logEx)
                    Debug.WriteLine("Exception type: {0}\r\nException Message: {1}", ex.GetType(), ex.Message);
                return true; // don't re-throw
            });

        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public static Task ContinueWithState<TState>(this Task task, Action<Task, TState> continuationAction, TState state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions)
    {
        return task.ContinueWith(
            (t, tupleObject) =>
            {
                var (closureAction, closureState) = ((Action<Task, TState>, TState))tupleObject!;

                closureAction(t, closureState);
            },
            (continuationAction, state),
            cancellationToken,
            continuationOptions,
            TaskScheduler.Default);
    }

    public static Task ContinueWithState<TResult, TState>(this Task<TResult> task, Action<Task<TResult>, TState> continuationAction, TState state, CancellationToken cancellationToken)
    {
        return task.ContinueWith(
            (t, tupleObject) =>
            {
                var (closureAction, closureState) = ((Action<Task<TResult>, TState>, TState))tupleObject!;

                closureAction(t, closureState);
            },
            (continuationAction, state),
            cancellationToken);
    }

    public static Task ContinueWithState<TResult, TState>(this Task<TResult> task, Action<Task<TResult>, TState> continuationAction, TState state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions)
    {
        return task.ContinueWith(
            (t, tupleObject) =>
            {
                var (closureAction, closureState) = ((Action<Task<TResult>, TState>, TState))tupleObject!;

                closureAction(t, closureState);
            },
            (continuationAction, state),
            cancellationToken,
            continuationOptions,
            TaskScheduler.Default);
    }

    public static bool ImplementsInterface(this Type baseType, Type interfaceType) => baseType.GetInterfaces().Any(interfaceType.Equals);

    public static void PostWithComplete<T>(this SynchronizationContext context, Action<T> action, T state)
    {
        context.OperationStarted();
        context.Post(o => {
            try { action((T)o!); }
            finally { context.OperationCompleted(); }
        },
            state
        );
    }

    public static void PostWithComplete(this SynchronizationContext context, Action action)
    {
        context.OperationStarted();
        context.Post(_ => {
            try { action(); }
            finally { context.OperationCompleted(); }
        },
            null
        );
    }

    /// <summary>
    /// Helper function to calculate an element's rectangle in root-relative coordinates.
    /// </summary>
    public static Windows.Foundation.Rect GetElementRect(this Microsoft.UI.Xaml.FrameworkElement element)
    {
        try
        {
            Microsoft.UI.Xaml.Media.GeneralTransform transform = element.TransformToVisual(null);
            Windows.Foundation.Point point = transform.TransformPoint(new Windows.Foundation.Point());
            return new Windows.Foundation.Rect(point, new Windows.Foundation.Size(element.ActualWidth, element.ActualHeight));
        }
        catch (Exception)
        {
            return new Windows.Foundation.Rect(0, 0, 0, 0);
        }
    }
}

