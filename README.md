# AutoPowerOff

Windows 下的免安装小工具：可设置**每日定时关机**、**开机自启动**（当前用户），并在系统托盘常驻。到点后通过系统 `shutdown` 命令发起**温和关机**（约 60 秒倒计时，**不**使用 `/f` 强制结束进程）。

## 功能概览

- 设置每日关机时间（24 小时制，默认 **18:10**，无配置文件时生效）
- 启用/停用定时关机、可选**开机自启动**（写入当前用户注册表 `Run`）
- 关闭主窗口后**最小化到托盘**，需保持托盘运行才会在到点执行关机
- 托盘菜单：**打开设置**、**立即关机**、**关机说明**、**退出**
- 配置保存在 exe 同目录的 `config.json`

## 环境要求

- **Windows 10/11**（x64）
- 构建需要 **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**（`dotnet --version` 可看到 8.x）

若公司电脑**无管理员权限**或网络受限，可使用仓库内脚本将 SDK 安装到用户目录，或使用离线 zip，详见下文「无管理员 / 离线安装 SDK」。

## 构建与发布

在仓库根目录执行：

```powershell
.\publish.ps1
```

或手动：

```powershell
dotnet publish .\AutoPowerOff\AutoPowerOff.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish
```

输出：`publish\AutoPowerOff.exe`（自包含单文件，目标机无需单独安装 .NET 运行时）。

## 无管理员 / 离线安装 SDK

| 场景 | 做法 |
|------|------|
| 无管理员、可联网 | 运行 `.\install-dotnet-sdk-user.ps1`（安装到 `%USERPROFILE%\.dotnet`），关闭终端后重开，再执行 `.\publish.ps1` |
| 无法下载安装脚本 | 将微软官方 `dotnet-install.ps1` 放到 `tools\dotnet-install.ps1` 后重试 |
| 完全离线 | 在能上网的机器下载 [.NET 8 SDK x64 zip](https://dotnet.microsoft.com/download/dotnet/8.0)，拷贝到本机后执行 `.\install-dotnet-from-local-zip.ps1 -ZipPath <zip完整路径>` |

脚本说明为英文，可避免部分环境下 PowerShell 编码问题。

## 图标（可选）

将 `poweroff.ico` 放在 `AutoPowerOff\` 目录下与 `AutoPowerOff.csproj` 同级；存在时发布出的 exe 会嵌入该图标。未放置时不影响编译。

## 配置说明（`config.json`）

与 exe 同目录，示例字段：

| 字段 | 含义 |
|------|------|
| `Enabled` | 是否启用定时关机 |
| `ShutdownTime` | 每日时间，字符串 `"HH:mm"` |
| `RunAtStartup` | 是否开机自启动 |
| `LastTriggeredDate` | 上次成功发起关机的日期 `yyyy-MM-dd`（程序自动维护，同日只触发一次） |

修改默认时间可编辑代码中的默认值，或删除 `config.json` 后重启以恢复默认。

## 关机行为说明

- 定时关机与「立即关机」均使用：`shutdown /s /t 60`（**不含** `/f`）。
- 约 **60 秒**后关机，便于保存工作；若有程序无响应，系统可能提示「程序正在阻止关机」。
- 需要**取消已安排的关机**时，可在命令行执行：`shutdown /a`。

程序内 **关机说明** 中有更完整说明。

## 项目结构（简要）

```
auto_poweroff/
├── AutoPowerOff/           # WinForms 主工程
│   ├── AutoPowerOff.csproj
│   ├── Program.cs
│   ├── MainForm.cs         # 界面与托盘
│   ├── AppConfig.cs        # config.json
│   ├── ShutdownService.cs  # 定时检测与 shutdown 调用
│   └── StartupRegistry.cs  # 开机启动（HKCU Run）
├── publish.ps1             # 一键发布单文件 exe
├── install-dotnet-sdk-user.ps1
├── install-dotnet-from-local-zip.ps1
└── tools/                  # 可选：本地 dotnet-install.ps1
```

## 许可证

未指定；如需开源请自行补充 `LICENSE`。
