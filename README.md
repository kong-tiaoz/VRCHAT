# VRCHAT

个人使用的 VRChat OSC 工具。

## QQ 音乐歌名与同步歌词

[使用说明](qqmusic-osc/README.md)

读取 QQMusicMonitor 的本地接口，将 **歌名、歌手和当前一句歌词** 发送到 VRChat 聊天框。无需修改模型。

```text
QQ 音乐 → QQMusicMonitor → 本仓库转发工具 → VRChat 聊天框
```

## 中英双向语音翻译

[VRCT 使用说明](vrct/README.md)

使用 VRCT 官方 GPU 版和本地模型：自己说中文后通过 OSC 发英文字幕，同时将听到的英语翻译为中文。默认使用 NLLB 1.3B 与 Whisper small，启动入口为 `vrct/启动翻译.cmd`；CPU 回退入口为 `vrct/启动CPU版.cmd`。

[英语陪练字幕窗口](vrct/subtitles/README.md)单独显示对方的英语原文和中文译文；默认置顶，可调整字号和位置。启动入口会同时启动翻译器与字幕窗，游戏聊天框留给自己的英文发言。窗口的完整 C# 源码、构建脚本和测试均在 `vrct/subtitles/`。

```text
你的中文 → VRCT → 英文 → VRChat 聊天框
对方英语 → VRCT → 英语原文＋中文译文 → 独立字幕窗
```

首次下载仓库时，请按 [VRCT 安装步骤](vrct/README.md#首次下载本仓库)准备官方程序和模型。首次启动会自动编译字幕窗，无需另装开发工具。官方程序、模型、个人配置和字幕历史不上传到本仓库。
