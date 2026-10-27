# 英语陪练字幕窗口

独立的 Windows 桌面窗口，接收 VRCT 的英语原文和中文译文。它不发送 OSC，不占用 VRChat 聊天框。你说的中文仍由 VRCT 翻译成英文，发到游戏聊天框。

## 使用

1. 首次接入时，先退出正在运行的 VRCT。
2. 双击 `vrct/启动翻译.cmd`，或本机已建立的桌面 **VRChat 实时翻译** 入口。同时打开 VRCT 和本窗口，并在启动前配置好分流。新下载仓库请先按[首次安装说明](../README.md#首次下载本仓库)准备官方程序和模型。
3. VRCT 保持开启 **翻译、你的语音转文字、他人语音转文字**。
4. 字幕窗口显示“已连接”后，对方说话时会在这里出现英语原文及中文译文。
5. 可以最小化 VRCT 主窗口。关闭 VRCT 会停止识别；关闭字幕窗口仅关闭显示。

窗口默认置顶，拖动左上角标题可以移动，右下角可以缩放。`A− / A+` 调整字号，滑块调整背景透明度，“跟随最新”可以暂停或恢复自动滚动。位置、尺寸、字号和置顶状态会自动保存；显示历史只保留在内存中，最多 40 条，关闭窗口后清除。

PC 游戏建议使用窗口或无边框模式，使置顶字幕可以覆盖在游戏上方。

## 分流设置

启动脚本仅在 VRCT 尚未运行时写入以下三项，避免与程序正在保存的配置冲突：

- `WEBSOCKET_SERVER = true`：开启已有的本机字幕输出接口，保留 VRCT 原有身份验证令牌。
- `SEND_MESSAGE_TO_VRC = true`：你的中文翻译成英文后继续发到游戏。
- `SEND_RECEIVED_MESSAGE_TO_VRC = false`：接收到的外语译文不再发到游戏聊天框。

原配置首次备份到对应程序目录的 `config.before-private-subtitles.json`。如果启动入口时 VRCT 已经在运行，脚本不会改写它的实时配置；首次接入需要退出后重新打开。

本窗口只接收本机 `127.0.0.1` / `localhost` / `::1` 的字幕，不使用云服务，不保存或显示连接令牌。它会忽略自己的语音、手动发送的聊天和设置消息。

**保持 VRCT 的“聊天读取”关闭。** v3.6.0 把语音接收和 OCR 都标记为 `RECEIVED`，没有提供足够字段可靠区分；打开聊天读取时，画面识别结果也可能进入这个窗口。本程序不会假装能从这类消息中识别出具体玩家。

切换 CPU/GPU 版本时，先关闭 VRCT 和字幕窗口，再使用对应入口。

## 源码和检查

- `CaptionProtocol.cs`：解析、方向筛选和配置读取。
- `CaptionClient.cs`：本机 WebSocket 连接、身份验证、消息分片与自动重连。
- `SubtitleWindow.cs`：WPF 桌面窗口和窗口偏好保存。
- `build.ps1`：使用 Windows 自带的 .NET Framework 编译器构建，无需安装 SDK 或 npm 包。
- `Tests.cs` / `integration-test.cjs`：协议、配置、窗口偏好保存，以及本机认证、Unicode 消息分片、方向筛选和重连测试。
- `configure-routing.ps1` / `test-routing.ps1`：分流设置、原配置备份、不相关设置保护及 Windows PowerShell 兼容性检查。

首次正常启动会自动构建 `bin/EnglishSubtitles.exe`。也可以进入本目录手动构建和验证：

```powershell
Set-Location .\vrct\subtitles
.\build.ps1
.\bin\SubtitleTests.exe .\test-output
node .\integration-test.cjs
.\test-routing.ps1
```

只预览字幕窗、不连接 VRCT：

```powershell
.\bin\EnglishSubtitles.exe --demo
```

`bin/`、`state.json`、`test-output/` 都不提交 Git。窗口代码和测试源码保存在本仓库，VRCT 官方程序保持独立。
