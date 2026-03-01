using System.Runtime.InteropServices;
using FeColorizer;

switch (args.Length > 0 ? args[0].ToLowerInvariant() : "")
{
    case "--colorize":
    {
        string path = SanitizePath(args.Length >= 2 ? args[1] : "");
        if (!Directory.Exists(path))
        {
            NativeMsgBox.Show("No valid folder path provided.", "FeColorizer",
                NativeMsgBox.MB_OK | NativeMsgBox.MB_ICONWARNING);
            return;
        }
        Colorizer.ColorizeSubfolders(path);
        break;
    }

    case "--revert":
    {
        string path = SanitizePath(args.Length >= 2 ? args[1] : "");
        if (!Directory.Exists(path))
        {
            NativeMsgBox.Show("No valid folder path provided.", "FeColorizer",
                NativeMsgBox.MB_OK | NativeMsgBox.MB_ICONWARNING);
            return;
        }
        Colorizer.RevertSubfolders(path);
        break;
    }

    case "--generate-icons":
        // Called silently by the installer to pre-populate %AppData%\FeColorizer\icons\
        IconGenerator.GenerateAll();
        break;

    default:
        NativeMsgBox.Show(
            "FeColorizer is installed and running.\n\n" +
            "Right-click any folder or drive in Explorer to use it.\n\n" +
            "To uninstall, go to Windows Settings \u2192 Apps \u2192 FeColorizer.",
            "FeColorizer",
            NativeMsgBox.MB_OK | NativeMsgBox.MB_ICONINFO);
        break;
}

// -------------------------------------------------------------------------

/// <summary>
/// Fixes the trailing-backslash quoting bug in Windows shell commands.
/// "D:\" is passed by Explorer as D:" (the \" escapes the closing quote).
/// This strips stray quotes and restores the correct drive root path.
/// </summary>
static string SanitizePath(string raw)
{
    string path = raw.Trim().Trim('"').TrimEnd('\\').TrimEnd('"');

    if (path.Length == 2 && char.IsLetter(path[0]) && path[1] == ':')
        path += Path.DirectorySeparatorChar;

    return path;
}

// -------------------------------------------------------------------------

internal static class NativeMsgBox
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int MessageBoxW(nint hWnd, string lpText, string lpCaption, uint uType);

    public const uint MB_OK           = 0x00000000;
    public const uint MB_YESNO        = 0x00000004;
    public const uint MB_ICONERROR    = 0x00000010;
    public const uint MB_ICONQUESTION = 0x00000020;
    public const uint MB_ICONWARNING  = 0x00000030;
    public const uint MB_ICONINFO     = 0x00000040;
    public const int  IDYES           = 6;

    public static int Show(string text, string caption, uint type) =>
        MessageBoxW(0, text, caption, type);
}
