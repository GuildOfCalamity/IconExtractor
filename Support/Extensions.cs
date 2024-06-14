using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using IconExtractor.Models;

using Vanara.PInvoke;


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

    public static string NameOf(this object obj) => $"{obj.GetType().Name} => {obj.GetType().BaseType?.Name}";
}

