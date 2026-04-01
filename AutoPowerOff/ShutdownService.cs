using System.Diagnostics;
using System.Windows.Forms;

namespace AutoPowerOff;

/// <summary>
/// 每日到点检测并调用系统温和关机（约 60 秒倒计时，不强制结束进程）。
/// </summary>
internal sealed class ShutdownService : IDisposable
{
    /// <summary>与 shutdown.exe 一致：正常关机、约 60 秒后执行，不加 /f 以便程序自行退出。</summary>
    internal const string ShutdownArguments = "/s /t 60";

    /// <summary>若距离上次键鼠输入不足该毫秒数，视为“仍在工作”，到点关机前弹出确认。</summary>
    internal const int RecentActivityThresholdMs = 90_000;

    private readonly System.Threading.Timer _timer;
    private readonly object _gate = new();
    private readonly Control? _uiContext;
    private AppConfig _config;

    public event EventHandler? ShutdownInitiated;

    public ShutdownService(AppConfig initialConfig, Control? uiContext)
    {
        _config = initialConfig;
        _uiContext = uiContext;
        _timer = new System.Threading.Timer(_ => Tick(), null, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));
    }

    public void UpdateConfig(AppConfig config)
    {
        lock (_gate)
            _config = config;
    }

    private void Tick()
    {
        AppConfig cfg;
        lock (_gate)
            cfg = _config;

        if (!cfg.Enabled)
            return;

        if (!TimeOnly.TryParse(cfg.ShutdownTime, out var timeOnly))
            return;

        var now = DateTime.Now;
        var today = now.Date;
        var triggerAt = today + timeOnly.ToTimeSpan();

        var todayStr = today.ToString("yyyy-MM-dd");
        if (cfg.LastTriggeredDate == todayStr)
            return;

        if (now < triggerAt)
            return;

        var idleMs = NativeInput.GetIdleMilliseconds();
        if (idleMs < RecentActivityThresholdMs)
        {
            if (!ConfirmShutdownWithUserActive())
            {
                cfg.LastTriggeredDate = todayStr;
                cfg.Save();
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = ShutdownArguments,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            cfg.LastTriggeredDate = todayStr;
            cfg.Save();
            ShutdownInitiated?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // 忽略：无权限等情况下下次周期会重试
        }
    }

    /// <summary>返回 true 表示用户确认仍要关机；false 表示取消今日定时关机。</summary>
    private bool ConfirmShutdownWithUserActive()
    {
        const string msg =
            "检测到您最近仍在使用鼠标或键盘（可能处于工作状态）。\r\n\r\n是否仍要执行今日定时关机？\r\n\r\n" +
            "选「否」将跳过今天的自动关机，明天仍会按设定时间提醒。";

        if (_uiContext is null)
            return true;

        if (!_uiContext.InvokeRequired)
        {
            return MessageBox.Show(
                       _uiContext,
                       msg,
                       "定时关机确认",
                       MessageBoxButtons.YesNo,
                       MessageBoxIcon.Question,
                       MessageBoxDefaultButton.Button2) ==
                   DialogResult.Yes;
        }

        object? result = null;
        _uiContext.Invoke(
            new Action(() =>
            {
                result = MessageBox.Show(
                    _uiContext,
                    msg,
                    "定时关机确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
            }));
        return result is DialogResult.Yes;
    }

    public static void ShutdownNow()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = ShutdownArguments,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
    }

    public void Dispose() => _timer.Dispose();
}
