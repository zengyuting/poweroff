# AutoPowerOff

Windows 下的免安装小工具：可设置**每日定时关机**、**开机自启动**（当前用户），并在系统托盘常驻。到点后通过系统 `shutdown` 命令发起**温和关机**（约 60 秒倒计时，**不**使用 `/f` 强制结束进程）。

---

## 下载即用（推荐给使用者）

1. 在 GitHub 打开本仓库的 **Releases**（发布），下载最新版本中的 **`AutoPowerOff.exe`**。这是**自包含单文件**，使用者**无需**安装 .NET，也**无需**自行编译。
2. 把 exe 放到任意文件夹（例如桌面或 `D:\Tools`），**双击运行**即可。
3. 首次运行会在同目录生成 `config.json`；关闭窗口会退到**系统托盘**，需要到点关机时请保持托盘里本程序在运行。

> **维护者说明**：请在每次发版时用 `publish.ps1` 生成 `publish\AutoPowerOff.exe`，并上传到 GitHub Releases，这样访问仓库的人只需下载，无需构建。

---

## 使用说明

### 功能概览

- 设置每日关机时间（24 小时制，默认 **18:10**，无配置文件时生效）
- 启用/停用定时关机、可选**开机自启动**（当前用户，注册表 `Run`）
- 关闭主窗口后**最小化到托盘**，需保持托盘运行才会在到点执行关机
- 托盘菜单：**打开设置**、**立即关机**、**关机说明**、**退出**

### 配置（`config.json`）

与 exe 同目录，由程序自动读写：

| 字段 | 含义 |
|------|------|
| `Enabled` | 是否启用定时关机 |
| `ShutdownTime` | 每日时间，字符串 `"HH:mm"` |
| `RunAtStartup` | 是否开机自启动 |
| `LastTriggeredDate` | 上次成功发起关机的日期（程序维护，同日只触发一次） |

### 关机行为

- 定时与「立即关机」均使用：`shutdown /s /t 60`（**不含** `/f`）。
- 约 **60 秒**后关机；若有程序无响应，系统可能提示「程序正在阻止关机」。
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

若公司电脑无管理员权限、无法直接装 SDK，可使用仓库内 `install-dotnet-sdk-user.ps1` 或 `install-dotnet-from-local-zip.ps1`（详见脚本内英文说明）。

可选：将 `poweroff.ico` 放在 `AutoPowerOff\` 目录下再发布，可为 exe 嵌入图标。

---

## 项目结构（简要）

```
auto_poweroff/
├── AutoPowerOff/           # WinForms 主工程
├── publish.ps1             # 生成可发布的单文件 exe
├── install-dotnet-sdk-user.ps1
├── install-dotnet-from-local-zip.ps1
└── tools/                  # 可选：离线放置 dotnet-install.ps1
```

---

## 许可证

未指定；如需开源请自行补充 `LICENSE`。
