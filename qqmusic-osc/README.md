# QQ 音乐 → VRChat 聊天框

通过 [QQMusicMonitor](https://github.com/GotKiCry/QQMusicMonitor) 获取 QQ 音乐的歌名、歌手、播放进度及同步歌词，然后通过 OSC 发送到 VRChat。

示例格式：

```text
♫ 歌名 — 歌手
♪ 当前一句歌词
```

## 准备

- Windows 10/11、QQ 音乐、VRChat。
- 安装 Node.js 18 或更新版本，并加入 PATH；不需要安装 npm 依赖。
- 从 [QQMusicMonitor 官方发布页](https://github.com/GotKiCry/QQMusicMonitor/releases/tag/v1.0.2) 安装 1.0.2。本工具按此版本的数据结构接入。
- 默认将 QQMusicMonitor 安装到本目录下的 `monitor` 文件夹，使程序路径为 `monitor/qqmusic-monitor.exe`。也可通过环境变量 `QQMUSIC_MONITOR_EXE` 指定现有安装的完整路径。
- QQ 音乐开启系统媒体传输控件，并开始播放歌曲。QQMusicMonitor 1.0.2 从 QQ 音乐本地歌词缓存读取歌词，未命中时不能保证有歌词。
- QQMusicMonitor 本地 HTTP 服务保持开启，使用默认端口 3000。

## 使用

1. VRChat 圆盘菜单：**Options → OSC → Enabled**。
2. 双击 `启动聊天框.cmd`，启动 QQMusicMonitor 和后台转发。
3. 需要查看识别结果时，双击 `预览歌词.cmd`。预览只在窗口显示，不发送 OSC。
4. 双击 `停止聊天框.cmd` 停止转发；QQMusicMonitor 保持运行。

也可以在本目录运行：

```powershell
node bridge.cjs
node bridge.cjs --preview
```

直接运行上述命令时，先自行打开 QQMusicMonitor。按 Ctrl+C 停止。启动脚本会检查进程，避免重复启动；通过终端手动运行时请自行避免重复。

## 行为

- 从 `http://127.0.0.1:3000/api/current` 每 250ms 读取一次。
- 优先使用 QRC 时间轴，缺失时尝试解析 LRC。
- 发送到本机 `127.0.0.1:9000` 的 `/chatbox/input`，关闭通知音。
- 消息至少间隔 2 秒；间隔内只保留最新歌词，快歌可能跳过部分句子。
- 无变化时每 12 秒刷新；暂停或数据服务断开时清空上次输出。
- 总长度最多 144 个 Unicode 字符，歌名及歌手部分最多 75 个字符。
- 避免同时启用其他软件的聊天框自动发送，否则内容可能相互覆盖。

## 排查

- 打开本地接口地址，应看到歌曲 JSON；打不开时检查 QQMusicMonitor 服务及端口设置。
- 有歌名但无歌词：先检查 QQMusicMonitor 自己是否显示歌词。
- 预览正常但游戏不显示：确认 OSC 开关、游戏接收端口及聊天框显示设置。
- 后台错误记录在 `bridge-error.log`；连接提示在 `bridge.log`。
- 如 Node.js 不在 PATH，可通过环境变量 `QQMUSIC_OSC_NODE` 指定 `node.exe` 的完整路径。

## 验证与上游

运行 `node test.cjs`，检查中文编码、QRC/LRC 歌词定位、暂停、长度限制、OSC 打包和本地 UDP 收发。测试使用随机回环端口，不向 VRChat 发送测试文字。

已在本机读取 QQ 音乐实时歌曲和歌词，并验证 VRChat 监听端口及转发进程。游戏内最终视觉显示仍需实际确认。

QQMusicMonitor 为独立上游项目；本仓库仅包含转发工具，不包含其安装包、源码副本、个人设置或歌词缓存。
