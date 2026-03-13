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
        // Create one IThumbnailCache instance for the whole batch.  Each call
        // to GetThumbnail(WTS_FORCEEXTRACTION) synchronously re-extracts the
        // thumbnail from desktop.ini and overwrites any stale thumbcache entry,
        // so Large-icon view shows the new color without requiring a view switch.
        IThumbnailCache? thumbCache = TryCreateThumbnailCache();
        try
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
                ForceThumbnailCacheUpdate(dir, thumbCache);
            }
        }
        finally
        {
            if (thumbCache != null) Marshal.ReleaseComObject(thumbCache);
        }

        NotifyShell(parentPath);
    }

    /// <summary>
    /// Removes color icons from all immediate subfolders of
    /// <paramref name="parentPath"/> that were applied by this app.
    /// </summary>
    public static void RevertSubfolders(string parentPath)
    {
        IThumbnailCache? thumbCache = TryCreateThumbnailCache();
        try
        {
            foreach (string dir in SafeEnumerateDirectories(parentPath))
            {
                string iniPath = Path.Combine(dir, "desktop.ini");

                if (!File.Exists(iniPath) || !IsOurs(iniPath))
                    continue;

                RemoveDesktopIni(dir, iniPath);
                ForceThumbnailCacheUpdate(dir, thumbCache);
            }
        }
        finally
        {
            if (thumbCache != null) Marshal.ReleaseComObject(thumbCache);
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

            NotifyDesktopIniWritten(iniPath, dir);
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

            NotifyDesktopIniRemoved(iniPath, dir);
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
    // Thumbnail cache — direct, synchronous update via IThumbnailCache COM API
    //
    // Root cause of the Large-icon inconsistency: Explorer's thumbnail service
    // regenerates thumbnails asynchronously in response to SHChangeNotify.  If
    // a stale thumbcache entry (default yellow folder) exists before colorize
    // runs, Explorer shows that stale entry until the async regeneration
    // completes — which is a race against when the user opens the folder.
    //
    // Fix: call IThumbnailCache::GetThumbnail(WTS_FORCEEXTRACTION) ourselves,
    // which re-runs the shell's FolderThumbnailProvider in-process, reads the
    // new desktop.ini / .ico, and OVERWRITES the stale entry synchronously,
    // before the user ever sees the folder.
    //
    // CLSID verified on this machine:
    //   {7EFC002A-071F-4CE7-B265-F4B4263D2FD2}  CLSID_UIThreadThumbnailCache
    //   InProcServer32 = thumbcache.dll  ThreadingModel = Both
    // -------------------------------------------------------------------------

    // Opaque shell item — only passed to IThumbnailCache, never called from C#.
    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem { }

    // Opaque shared bitmap — only checked for null and released.
    [ComImport, Guid("091162A4-BC96-411F-AAE8-C5122CD03363"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISharedBitmap { }

    // Full vtable order must match the COM interface exactly.
    [ComImport, Guid("F676C15D-596A-4CE2-8234-33996F445DB1"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IThumbnailCache
    {
        [PreserveSig]
        int GetThumbnail(
            IShellItem            pShellItem,
            uint                  cxyRequestedThumbSize,
            uint                  flags,
            out ISharedBitmap?    ppvThumb,
            out uint              pOutFlags,
            out Guid              pThumbnailID);

        [PreserveSig]
        int GetThumbnailByID(
            Guid                  thumbnailID,
            uint                  cxyRequestedThumbSize,
            out ISharedBitmap?    ppvThumb,
            out uint              pOutFlags);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc,
        [In] ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    private static readonly Guid IID_IShellItem =
        new("43826D1E-E718-42EE-BC55-A1E261C37BFE");

    // Verified in registry: CLSID_UIThreadThumbnailCache → thumbcache.dll
    private static readonly Guid CLSID_UIThreadThumbnailCache =
        new("7EFC002A-071F-4CE7-B265-F4B4263D2FD2");

    private const uint WTS_FORCEEXTRACTION = 0x00000004;

    private static IThumbnailCache? TryCreateThumbnailCache()
    {
        try
        {
            return (IThumbnailCache?)Activator.CreateInstance(
                Type.GetTypeFromCLSID(CLSID_UIThreadThumbnailCache)!);
        }
        catch { return null; }
    }

    /// <summary>
    /// Forces the shell to synchronously re-extract and cache the thumbnail
    /// for <paramref name="folderPath"/>, replacing any stale entry.
    /// </summary>
    private static void ForceThumbnailCacheUpdate(string folderPath, IThumbnailCache? cache)
    {
        if (cache == null) return;
        try
        {
            Guid iid = IID_IShellItem;
            int hr = SHCreateItemFromParsingName(folderPath, IntPtr.Zero,
                ref iid, out IShellItem shellItem);
            if (hr != 0) return;

            try
            {
                cache.GetThumbnail(shellItem, 256, WTS_FORCEEXTRACTION,
                    out ISharedBitmap? bmp, out _, out _);
                if (bmp != null) Marshal.ReleaseComObject(bmp);
            }
            finally { Marshal.ReleaseComObject(shellItem); }
        }
        catch { }
    }

    // -------------------------------------------------------------------------
    // Shell change notifications
    // -------------------------------------------------------------------------

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags,
        IntPtr dwItem1, IntPtr dwItem2);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ILCreateFromPathW(string pszPath);

    [DllImport("shell32.dll")]
    private static extern void ILFree(IntPtr pidl);

    private const int  SHCNE_CREATE     = 0x00000002;
    private const int  SHCNE_DELETE     = 0x00000004;
    private const int  SHCNE_ATTRIBUTES = 0x00000800;
    private const int  SHCNE_UPDATEDIR  = 0x00001000;
    private const int  SHCNE_UPDATEITEM = 0x00002000;
    private const uint SHCNF_IDLIST     = 0x0000;
    private const uint SHCNF_PATH       = 0x0005;
    private const uint SHCNF_FLUSH      = 0x1000;
    private const uint SHCNF_FLUSHNOWAIT = 0x2000;

    private static void NotifyDesktopIniWritten(string iniPath, string folderPath)
    {
        IntPtr iniPtr    = Marshal.StringToHGlobalUni(iniPath);
        IntPtr folderPtr = Marshal.StringToHGlobalUni(folderPath);
        SHChangeNotify(SHCNE_CREATE,     SHCNF_PATH | SHCNF_FLUSHNOWAIT, iniPtr,    IntPtr.Zero);
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH | SHCNF_FLUSHNOWAIT, folderPtr, IntPtr.Zero);
        Marshal.FreeHGlobal(folderPtr);
        Marshal.FreeHGlobal(iniPtr);

        IntPtr pidl = ILCreateFromPathW(folderPath);
        if (pidl == IntPtr.Zero) return;
        try   { SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_IDLIST | SHCNF_FLUSH, pidl, IntPtr.Zero); }
        finally { ILFree(pidl); }
    }

    private static void NotifyDesktopIniRemoved(string iniPath, string folderPath)
    {
        IntPtr iniPtr    = Marshal.StringToHGlobalUni(iniPath);
        IntPtr folderPtr = Marshal.StringToHGlobalUni(folderPath);
        SHChangeNotify(SHCNE_DELETE,     SHCNF_PATH | SHCNF_FLUSHNOWAIT, iniPtr,    IntPtr.Zero);
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH | SHCNF_FLUSHNOWAIT, folderPtr, IntPtr.Zero);
        Marshal.FreeHGlobal(folderPtr);
        Marshal.FreeHGlobal(iniPtr);

        IntPtr pidl = ILCreateFromPathW(folderPath);
        if (pidl == IntPtr.Zero) return;
        try   { SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_IDLIST | SHCNF_FLUSH, pidl, IntPtr.Zero); }
        finally { ILFree(pidl); }
    }

    private static void NotifyShell(string parentPath)
    {
        IntPtr pidl = ILCreateFromPathW(parentPath);
        if (pidl == IntPtr.Zero) return;
        try   { SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_IDLIST | SHCNF_FLUSH, pidl, IntPtr.Zero); }
        finally { ILFree(pidl); }
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
