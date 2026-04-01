using System.Drawing;
using System.Globalization;

namespace AutoPowerOff;

internal sealed class MainForm : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayMenu;
    private readonly DateTimePicker _timePicker;
    private readonly CheckBox _chkEnabled;
    private readonly CheckBox _chkStartup;
    private readonly Button _btnSave;
    private AppConfig _config = new();
    private ShutdownService? _shutdownService;
    private bool _allowClose;

    public MainForm()
    {
        Text = "自动关机";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(380, 278);
        ShowInTaskbar = true;

        var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (exeIcon is not null)
        {
            Icon = exeIcon;
        }

        var lblTime = new Label
        {
            AutoSize = true,
            Location = new Point(12, 18),
            Text = "每日关机时间：",
        };

        _timePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Location = new Point(132, 14),
            Width = 140,
            Height = 28,
        };

        _chkEnabled = new CheckBox
        {
            AutoSize = true,
            Location = new Point(12, 56),
            Text = "启用定时关机",
        };

        _chkStartup = new CheckBox
        {
            AutoSize = true,
            Location = new Point(12, 86),
            Text = "开机自启动",
        };

        var lnkHelp = new LinkLabel
        {
            AutoSize = true,
            Location = new Point(12, 116),
            Text = "关机说明",
        };
        lnkHelp.LinkClicked += (_, _) => ShowShutdownHelp(this);

        var lblTrayHint = new Label
        {
            AutoSize = false,
            Location = new Point(12, 148),
            Size = new Size(356, 72),
            Text =
                "提示：点击窗口右上角 × 将隐藏到系统托盘（不会退出）。\r\n" +
                "定时关机须保持本程序在托盘中运行。\r\n" +
                "到点若检测到约 90 秒内仍有键鼠操作，会先询问是否关机；长时间无操作则直接温和关机。",
            ForeColor = SystemColors.GrayText,
        };

        _btnSave = new Button
        {
            Text = "保存",
            Location = new Point(268, 232),
            Size = new Size(96, 34),
            MinimumSize = new Size(96, 34),
        };
        _btnSave.Click += (_, _) => SaveFromUi();

        // lblTrayHint 先于 lnkHelp，且二者竖向错开，避免灰色提示盖住「关机说明」链接
        Controls.AddRange(new Control[] { lblTime, _timePicker, _chkEnabled, _chkStartup, lblTrayHint, lnkHelp, _btnSave });

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("打开设置", null, (_, _) => ShowFromTray());
        _trayMenu.Items.Add("立即关机…", null, (_, _) => ShutdownNowClick());
        _trayMenu.Items.Add("关机说明…", null, (_, _) => ShowShutdownHelp(this));
        _trayMenu.Items.Add("-");
        _trayMenu.Items.Add("退出", null, (_, _) => ExitApp());

        _notifyIcon = new NotifyIcon
        {
            Icon = exeIcon is not null ? (Icon)exeIcon.Clone() : SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = _trayMenu,
            Text = "自动关机",
        };
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();

        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        FormClosed += MainForm_FormClosed;
        Resize += MainForm_Resize;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        var isFirstRun = !File.Exists(configPath);

        _config = AppConfig.Load();
        ApplyConfigToUi();

        try
        {
            if (_config.RunAtStartup)
                StartupRegistry.SetEnabled(true, Application.ExecutablePath);
            else if (StartupRegistry.IsRegistered())
                StartupRegistry.SetEnabled(false, Application.ExecutablePath);
        }
        catch
        {
            // 忽略注册表错误
        }

        _shutdownService = new ShutdownService(_config, this);
        _shutdownService.UpdateConfig(_config);

        if (isFirstRun)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "自动关机",
                "点击 × 会隐藏到托盘。请保持本程序在托盘中，定时关机才会生效。",
                ToolTipIcon.Info);
        }
    }

    private void ApplyConfigToUi()
    {
        _chkEnabled.Checked = _config.Enabled;
        _chkStartup.Checked = _config.RunAtStartup;
        if (TimeOnly.TryParse(_config.ShutdownTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            _timePicker.Value = DateTime.Today + t.ToTimeSpan();
        else
            _timePicker.Value = DateTime.Today + new TimeSpan(18, 10, 0);
    }

    private void SaveFromUi()
    {
        _config.Enabled = _chkEnabled.Checked;
        _config.RunAtStartup = _chkStartup.Checked;
        var t = TimeOnly.FromDateTime(_timePicker.Value);
        _config.ShutdownTime = t.ToString("HH:mm", CultureInfo.InvariantCulture);
        _config.Save();

        try
        {
            StartupRegistry.SetEnabled(_config.RunAtStartup, Application.ExecutablePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "无法写入开机自启动设置：\n" + ex.Message, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        _shutdownService?.UpdateConfig(_config);
        _notifyIcon.ShowBalloonTip(2000, "已保存", "设置已保存。", ToolTipIcon.Info);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private static void ShowShutdownHelp(IWin32Window? owner)
    {
        const string body =
            "本程序到点关机与「立即关机」均使用温和方式，等价于：\r\n" +
            "shutdown /s /t 60\r\n\r\n" +
            "含义说明：\r\n" +
            "• /s：正常关机（非休眠）。\r\n" +
            "• /t 60：约 60 秒后关机，期间 Windows 会提示注销；多数程序可先保存并自行退出。\r\n" +
            "• 未使用 /f：不强制结束进程，有利于减少数据未保存、下次开机异常修复等情况。\r\n\r\n" +
            "定时关机前的工作检测：\r\n" +
            "若到点时发现您约 90 秒内仍有鼠标或键盘操作，会先弹出对话框询问是否仍要关机；" +
            "选「否」将跳过当天的自动关机（次日仍按设定时间执行）。若长时间无键鼠输入，则直接按温和方式关机。\r\n\r\n" +
            "请注意：\r\n" +
            "若有程序无响应或拒绝退出，关机可能会被推迟或弹出「程序正在阻止关机」；" +
            "您可在系统界面选择仍要关机或结束对应程序。\r\n\r\n" +
            "若需取消已安排的关机，可在命令行执行：shutdown /a";

        using var dlg = new Form
        {
            Text = "关机说明",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(440, 320),
            ShowInTaskbar = false,
        };
        var btn = new Button
        {
            Text = "确定",
            Dock = DockStyle.Bottom,
            Height = 36,
            DialogResult = DialogResult.OK,
        };
        var tb = new TextBox
        {
            Text = body,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            TabStop = false,
        };
        dlg.Controls.Add(btn);
        dlg.Controls.Add(tb);
        dlg.AcceptButton = btn;
        dlg.ShowDialog(owner);
    }

    private void ShutdownNowClick()
    {
        var r = MessageBox.Show(
            "确定要关机吗？系统将在约 60 秒后关机，请尽快保存工作。\n" +
            "（温和关机，不强制结束程序；若有程序卡死可能无法按时关机。）",
            "立即关机",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (r != DialogResult.Yes)
            return;
        try
        {
            ShutdownService.ShutdownNow();
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法执行关机：\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExitApp()
    {
        _allowClose = true;
        _notifyIcon.Visible = false;
        Close();
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _shutdownService?.Dispose();
        _shutdownService = null;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
            return;
        e.Cancel = true;
        Hide();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
            Hide();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Dispose();
            _trayMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
