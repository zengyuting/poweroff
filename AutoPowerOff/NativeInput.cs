using System.Runtime.InteropServices;

namespace AutoPowerOff;

/// <summary>
/// 通过 Win32 GetLastInputInfo 获取距离上次键盘/鼠标输入的时间。
/// </summary>
internal static class NativeInput
{
    /// <summary>若 API 失败，返回 0（视为“刚有输入”，便于走确认流程）。</summary>
    public static uint GetIdleMilliseconds()
    {
        var lii = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref lii))
            return 0;
        return GetTickCount() - lii.dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
}
