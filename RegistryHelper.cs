using Microsoft.Win32;

namespace FeColorizer;

/// <summary>
/// Registers and unregisters the FeColorizer right-click context menu entries
/// for both regular folders (Directory) and drive roots (Drive).
///
///   HKEY_CLASSES_ROOT\Directory\shell\FeColorizer_Apply\   → folders
///   HKEY_CLASSES_ROOT\Directory\shell\FeColorizer_Revert\
///   HKEY_CLASSES_ROOT\Drive\shell\FeColorizer_Apply\       → drive roots (D:\, etc.)
///   HKEY_CLASSES_ROOT\Drive\shell\FeColorizer_Revert\
/// </summary>
public static class RegistryHelper
{
    private const string ApplyVerb  = "FeColorizer_Apply";
    private const string RevertVerb = "FeColorizer_Revert";

    private static readonly string[] ShellRoots = ["Directory\\shell", "Drive\\shell"];

    public static void Install(string exePath)
    {
        foreach (string root in ShellRoots)
        {
            Register(root, ApplyVerb,  "Colorize subfolders",    exePath, "--colorize");
            Register(root, RevertVerb, "Remove folder colors",   exePath, "--revert");
        }
    }

    public static void Uninstall()
    {
        foreach (string root in ShellRoots)
        {
            SafeDelete($"{root}\\{ApplyVerb}");
            SafeDelete($"{root}\\{RevertVerb}");

            // Previous name before rename to FeColorizer
            SafeDelete($"{root}\\Colorized_Apply");
            SafeDelete($"{root}\\Colorized_Revert");
        }

        // Original cascading key (very first version)
        SafeDelete(@"Directory\shell\Colorized");
    }

    public static bool IsInstalled()
    {
        using var key = Registry.ClassesRoot.OpenSubKey($"Directory\\shell\\{ApplyVerb}");
        return key is not null;
    }

    // -------------------------------------------------------------------------

    private static void Register(string shellRoot, string verb, string label,
        string exePath, string flag)
    {
        using var key = Registry.ClassesRoot.CreateSubKey($"{shellRoot}\\{verb}");
        key.SetValue("", label);
        key.SetValue("Icon", $"\"{exePath}\",0");
        using var cmd = key.CreateSubKey("command");
        cmd.SetValue("", $"\"{exePath}\" {flag} \"%1\"");
    }

    private static void SafeDelete(string subKey)
    {
        try { Registry.ClassesRoot.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false); }
        catch { }
    }
}
