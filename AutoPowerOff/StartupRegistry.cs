using Microsoft.Win32;

namespace AutoPowerOff;

internal static class StartupRegistry
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AutoPowerOff";

    public static void SetEnabled(bool enable, string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key is null)
            return;

        if (enable)
        {
            var quoted = executablePath.Contains(' ', StringComparison.Ordinal)
                ? $"\"{executablePath}\""
                : executablePath;
            key.SetValue(ValueName, quoted);
        }
        else
        {
            if (key.GetValue(ValueName) is not null)
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is not null;
    }
}
