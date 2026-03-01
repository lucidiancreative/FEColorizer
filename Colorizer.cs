using System.Runtime.InteropServices;

namespace FeColorizer;

/// <summary>
/// Applies or reverts colored folder icons for the immediate subfolders
/// of a given parent directory, using the desktop.ini mechanism.
/// </summary>
public static class Colorizer
{
    private const string Section   = "FeColorizer";
    private const string MarkerKey = "Applied";
    private const string MarkerVal = "1";

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Colorizes all immediate subfolders of <paramref name="parentPath"/>
    /// whose names start with A-Z.
    /// </summary>
    public static void ColorizeSubfolders(string parentPath)
    {
        foreach (string dir in SafeEnumerateDirectories(parentPath))
        {
            string name = Path.GetFileName(dir);
            var color = ColorMap.GetColor(name);
            if (color is null) continue;

            string iniPath = Path.Combine(dir, "desktop.ini");

            // Skip only if we already applied this exact color (idempotent).
            // Overwrite anything else — including desktop.ini from other sources.
            if (File.Exists(iniPath) && IsOurs(iniPath)) continue;

            char letter = ColorMap.GetLetter(name)!.Value;
            string iconPath = IconGenerator.GetOrCreateIcon(letter, color.Value);

            WriteDesktopIni(dir, iniPath, iconPath);
        }

        NotifyShell(parentPath);
    }

    /// <summary>
    /// Removes color icons from all immediate subfolders of
    /// <paramref name="parentPath"/> that were applied by this app.
    /// </summary>
    public static void RevertSubfolders(string parentPath)
    {
        foreach (string dir in SafeEnumerateDirectories(parentPath))
        {
            string iniPath = Path.Combine(dir, "desktop.ini");

            if (!File.Exists(iniPath) || !IsOurs(iniPath))
                continue;

            RemoveDesktopIni(dir, iniPath);
        }

        NotifyShell(parentPath);
    }

    // -------------------------------------------------------------------------
    // desktop.ini helpers
    // -------------------------------------------------------------------------

    private static void WriteDesktopIni(string dir, string iniPath, string iconPath)
    {
        try
        {
            ClearFolderReadOnly(dir);

            if (File.Exists(iniPath))
                File.SetAttributes(iniPath, FileAttributes.Normal);

            string content =
                $"[.ShellClassInfo]\r\n" +
                $"IconResource={iconPath},0\r\n" +
                $"\r\n" +
                $"[{Section}]\r\n" +
                $"{MarkerKey}={MarkerVal}\r\n";

            File.WriteAllText(iniPath, content, System.Text.Encoding.Unicode);
            File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);

            var folderAttrs = File.GetAttributes(dir);
            File.SetAttributes(dir, folderAttrs | FileAttributes.ReadOnly);

            // Immediately invalidate Explorer's cached icon for this folder
            NotifyItemChanged(dir);
        }
        catch
        {
            // Skip folders we cannot write to
        }
    }

    private static void RemoveDesktopIni(string dir, string iniPath)
    {
        try
        {
            File.SetAttributes(iniPath, FileAttributes.Normal);
            File.Delete(iniPath);

            var attrs = File.GetAttributes(dir);
            File.SetAttributes(dir, attrs & ~FileAttributes.ReadOnly);
        }
        catch { }
    }

    private static bool IsOurs(string iniPath)
    {
        try
        {
            foreach (string line in File.ReadLines(iniPath))
            {
                if (line.Trim().Equals($"{MarkerKey}={MarkerVal}", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }
        return false;
    }

    private static void ClearFolderReadOnly(string dir)
    {
        var attrs = File.GetAttributes(dir);
        if ((attrs & FileAttributes.ReadOnly) != 0)
            File.SetAttributes(dir, attrs & ~FileAttributes.ReadOnly);
    }

    // -------------------------------------------------------------------------
    // Shell refresh
    // -------------------------------------------------------------------------

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags,
        IntPtr dwItem1, IntPtr dwItem2);

    private const int  SHCNE_ASSOCCHANGED = 0x08000000;
    private const int  SHCNE_UPDATEDIR    = 0x00001000;
    private const int  SHCNE_UPDATEITEM   = 0x00002000;
    private const uint SHCNF_PATH         = 0x0005;
    private const uint SHCNF_IDLIST       = 0x0000;
    private const uint SHCNF_FLUSH        = 0x1000;
    private const uint SHCNF_FLUSHNOWAIT  = 0x2000;

    /// <summary>
    /// Notifies Explorer that a specific folder's icon has changed,
    /// forcing it to drop any cached thumbnail and re-read the .ico file.
    /// </summary>
    internal static void NotifyItemChanged(string folderPath)
    {
        IntPtr ptr = Marshal.StringToHGlobalUni(folderPath);
        SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATH | SHCNF_FLUSHNOWAIT, ptr, IntPtr.Zero);
        Marshal.FreeHGlobal(ptr);
    }

    private static void NotifyShell(string parentPath)
    {
        // Update the parent directory listing
        IntPtr ptr = Marshal.StringToHGlobalUni(parentPath);
        SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATH, ptr, IntPtr.Zero);
        Marshal.FreeHGlobal(ptr);

        // Flush: force Explorer to process all queued notifications and
        // invalidate the icon cache for any items that changed
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH,
            IntPtr.Zero, IntPtr.Zero);
    }

    // -------------------------------------------------------------------------
    // Safe directory enumeration
    // -------------------------------------------------------------------------

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(path, "*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false,
                AttributesToSkip = FileAttributes.ReparsePoint,
            });
        }
        catch { yield break; }

        foreach (var d in dirs)
            yield return d;
    }
}
