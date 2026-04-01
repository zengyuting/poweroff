# AutoPowerOff

Windows 下的免安装小工具：可设置**每日定时关机**、**开机自启动**（当前用户），并在系统托盘常驻。

到点后通过系统 `shutdown` 发起**温和关机**（`shutdown /s /t 60`，**不含** `/f`）。若在到点时间附近检测到**约 90 秒内仍有键盘或鼠标操作**，会先**弹出对话框**询问是否仍要关机；选「否」则跳过当天自动关机。长时间无键鼠操作时则直接执行温和关机。

**同一 Windows 登录会话内仅允许运行一个实例**（命名互斥体）；重复启动 exe 会提示已在运行并退出。

---

## 下载即用（推荐给使用者）

1. 在 GitHub 打开本仓库的 **Releases**（发布），下载最新版本中的 **`AutoPowerOff.exe`**。这是**自包含单文件**，使用者**无需**安装 .NET，也**无需**自行编译。
2. 把 exe 放到任意文件夹（例如桌面或 `D:\Tools`），**双击运行**即可。
3. 首次运行会在同目录生成 `config.json`；点击窗口 **×** 会**隐藏到系统托盘**（不会退出），需要到点关机时请保持托盘里本程序在运行。

> **维护者说明**：请在每次发版时用 `publish.ps1` 生成 `publish\AutoPowerOff.exe`，并上传到 GitHub Releases，这样访问仓库的人只需下载，无需构建。

---

## 使用说明

### 功能概览

- 设置每日关机时间（24 小时制，默认 **18:10**，无配置文件或首次使用时生效）
- 启用/停用定时关机、可选**开机自启动**（当前用户，注册表 `Run`）
- 标题栏仅保留 **×**（无最小化按钮）；点击 **×** 为**隐藏到托盘**，不退出程序
- 主界面灰色提示：× 与托盘说明、**键鼠活动检测与到点确认**说明
- 托盘菜单：**打开设置**、**立即关机**、**关机说明**、**退出**
- **单实例**：同一会话内再次双击 exe 会提示已在运行，请到托盘使用已有实例

### 配置（`config.json`）

与 exe 同目录，由程序自动读写：

| 字段 | 含义 |
|------|------|
| `Enabled` | 是否启用定时关机 |
| `ShutdownTime` | 每日时间，字符串 `"HH:mm"` |
| `RunAtStartup` | 是否开机自启动 |
| `LastTriggeredDate` | 上次已成功发起关机流程的日期（程序维护；选「否」跳过关机时也会记入，避免反复弹窗） |

### 关机行为

- 定时关机与「立即关机」均使用：`shutdown /s /t 60`（**不含** `/f`）。
- **工作检测**：若到点时距离上次键鼠输入不足 **约 90 秒**，先弹出确认；选「是」再执行关机，选「否」则**当天不再自动关机**。若已超过约 90 秒无键鼠输入，则**不再询问**，直接温和关机。
- 约 **60 秒**后真正关机；若有程序无响应，系统可能提示「程序正在阻止关机」。
- 取消已安排的关机：在命令行执行 `shutdown /a`。

更完整的说明见程序内 **关机说明**。

---

## 从源码构建（仅开发者 / 自行打包）

一般使用者请直接下载 Releases 里的 exe，**只有**要改代码或自己打安装包时才需要本节内容。

- **系统**：Windows 10/11 x64  
- **需要**：[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（仅本机编译用，打好的 exe 给他人用时对方**不必**装 .NET）

在仓库根目录执行：

```powershell
.\publish.ps1
```

生成物：`publish\AutoPowerOff.exe`（自包含单文件）。

**为什么 exe 这么大？** 自包含发布把 **.NET 运行时 + 基础库 + WinForms** 都打进一个文件，常见约 **几十到一百多 MB**，这样别人电脑**不用单独装 .NET**。本仓库已开启 **`EnableCompressionInSingleFile`**，会在单文件内压缩嵌入的程序集，体积会比未压缩时小一截（具体因版本而异）。

**还能更小吗？** 若接受「用户机器必须已装 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)」，可改用**框架依赖**发布（exe 通常只有几 MB 级），但分发时要额外说明需安装运行时，不适合「下载一个 exe 就能用」的场景。发 GitHub Releases 时也可再打一个 **zip**，便于浏览器下载，体积与 exe 相近。

若公司电脑无管理员权限、无法直接装 SDK，可使用仓库内 `install-dotnet-sdk-user.ps1` 或 `install-dotnet-from-local-zip.ps1`（详见脚本内英文说明）。

可选：将 `poweroff.ico` 放在 `AutoPowerOff\` 目录下再发布（存在时自动用作 exe 图标）。

---

## 项目结构（简要）

```
auto_poweroff/
├── AutoPowerOff/           # WinForms 主工程
│   ├── Program.cs          # 入口、单实例 Mutex
│   ├── MainForm.cs         # 界面与托盘
│   ├── ShutdownService.cs  # 定时检测、温和关机、活动确认
│   ├── NativeInput.cs      # 键鼠空闲时间（Win32）
│   ├── AppConfig.cs
│   └── StartupRegistry.cs
├── publish.ps1
├── install-dotnet-sdk-user.ps1
├── install-dotnet-from-local-zip.ps1
└── tools/                  # 可选：离线放置 dotnet-install.ps1
```

---

## 许可证

未指定；如需开源请自行补充 `LICENSE`。
