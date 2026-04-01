using System.Diagnostics;

namespace AutoPowerOff;

/// <summary>
/// 每日到点检测并调用系统温和关机（约 60 秒倒计时，不强制结束进程）。
/// </summary>
internal sealed class ShutdownService : IDisposable
{
    /// <summary>与 shutdown.exe 一致：正常关机、约 60 秒后执行，不加 /f 以便程序自行退出。</summary>
    internal const string ShutdownArguments = "/s /t 60";

    private readonly System.Threading.Timer _timer;
    private readonly object _gate = new();
    private AppConfig _config;

    public event EventHandler? ShutdownInitiated;

    public ShutdownService(AppConfig initialConfig)
    {
        _config = initialConfig;
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
