using System.Text.Json;

namespace AutoPowerOff;

public sealed class AppConfig
{
    public bool Enabled { get; set; }
    /// <summary>每日关机时间，格式 HH:mm（24 小时）。</summary>
    public string ShutdownTime { get; set; } = "18:10";
    public bool RunAtStartup { get; set; }
    /// <summary>上次成功触发关机的本地日期 yyyy-MM-dd，用于同日只触发一次。</summary>
    public string? LastTriggeredDate { get; set; }

    private static string ConfigPath =>
        Path.Combine(AppContext.BaseDirectory, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new AppConfig();
            var json = File.ReadAllText(ConfigPath);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            if (string.IsNullOrWhiteSpace(cfg.ShutdownTime))
                cfg.ShutdownTime = "18:10";
            return cfg;
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, options));
    }
}
