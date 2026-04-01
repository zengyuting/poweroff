using System.Threading;
using System.Windows.Forms;

namespace AutoPowerOff;

internal static class Program
{
    /// <summary>会话内单实例，避免同名 exe 多开。</summary>
    private const string SingleInstanceMutexName = @"Local\AutoPowerOff.SingleInstance.7c2e9f4a";

    [STAThread]
    private static void Main()
    {
        var mutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            MessageBox.Show(
                "本程序已在运行，请在系统托盘查找已打开的实例。\r\n如需打开设置，请单击托盘图标。",
                "自动关机",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        finally
        {
            mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }
}
