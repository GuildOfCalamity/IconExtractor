using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
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
}

