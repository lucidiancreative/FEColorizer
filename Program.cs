using System.Runtime.InteropServices;
using FeColorizer;

if (args.Length == 0)
{
    int result = NativeMsgBox.Show(
        "FeColorizer is not yet registered as a context menu item.\n\n" +
        "Click Yes to register it (requires Administrator privileges).",
        "FeColorizer \u2014 Setup",
        NativeMsgBox.MB_YESNO | NativeMsgBox.MB_ICONQUESTION);

    if (result == NativeMsgBox.IDYES)
        Install();

    return;
}

switch (args[0].ToLowerInvariant())
{
    case "--install":
        Install();
        break;

    case "--uninstall":
        Uninstall();
        break;

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

    default:
        NativeMsgBox.Show($"Unknown argument: {args[0]}", "FeColorizer",
            NativeMsgBox.MB_OK | NativeMsgBox.MB_ICONWARNING);
        break;
}

// -------------------------------------------------------------------------

static void Install()
{
    try
    {
        string exePath = Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, "FeColorizer.exe");

        IconGenerator.GenerateAll();
        RegistryHelper.Install(exePath);

        NativeMsgBox.Show(
            "FeColorizer has been registered.\n\n" +
            "Right-click any folder or drive in Explorer to see the menu.",
            "FeColorizer \u2014 Installed",
            NativeMsgBox.MB_OK | NativeMsgBox.MB_ICONINFO);
    }
    catch (Exception ex)
    {
        NativeMsgBox.Show(
            $"Installation failed:\n{ex.Message}\n\n" +
            "Make sure you are running as Administrator.",
            "FeColorizer \u2014 Error",
            NativeMsgBox.MB_OK | NativeMsgBox.MB_ICONERROR);
    }
}

/// <summary>
/// Fixes the trailing-backslash quoting bug in Windows shell commands.
/// "D:\" is passed by Explorer as D:" (the \" escapes the closing quote).
/// This strips stray quotes and restores the correct drive root path.
/// </summary>
static string SanitizePath(string raw)
{
    // Strip any stray surrounding or trailing quotes introduced by the parser
    string path = raw.Trim().Trim('"').TrimEnd('\\').TrimEnd('"');

    // Restore trailing backslash for drive roots (e.g. "D:" → "D:\")
    if (path.Length == 2 && char.IsLetter(path[0]) && path[1] == ':')
        path += Path.DirectorySeparatorChar;

    return path;
}

static void Uninstall()
{
    try
    {
        RegistryHelper.Uninstall();
        NativeMsgBox.Show(
            "FeColorizer has been removed from the context menu.",
            "FeColorizer \u2014 Uninstalled",
            NativeMsgBox.MB_OK | NativeMsgBox.MB_ICONINFO);
    }
    catch (Exception ex)
    {
        NativeMsgBox.Show(
            $"Uninstall failed:\n{ex.Message}",
            "FeColorizer \u2014 Error",
            NativeMsgBox.MB_OK | NativeMsgBox.MB_ICONERROR);
    }
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
