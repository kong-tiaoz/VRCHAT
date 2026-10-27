# VRCT：中英双向语音翻译

本仓库提供 [VRCT 官方项目](https://github.com/misyaguziya/VRCT) 的启动与分流脚本，以及完整的独立字幕窗源码。当前本机搭配 **v3.6.0 GPU（CUDA）版**，部署在本目录的 `app` 文件夹；原 CPU 版及升级前的设置保留在 `app-cpu`。这些程序、模型和个人设置不随 Git 仓库分发。

- 自己说中文 → 本地语音识别 → 翻译成英文 → OSC 发到 VRChat 聊天框。
- 听到英语 → 本地识别并翻译成中文 → 显示在 VRCT；不将对方的话自动发回游戏。
- 听到英语的原文和中文译文也会显示在[独立的英语陪练字幕窗口](subtitles/README.md)，游戏聊天框留给自己发言。
- 界面为简体中文；识别使用 Whisper small，翻译使用 NLLB 1.3B INT8；无需 API Key。
- 本机翻译和语音识别均使用 NVIDIA GeForce RTX 3070 Ti Laptop GPU，运行时计算精度保持「自动」；其他电脑请按自身硬件选择处理设备。
- 当前默认只做中英互译；关闭同一预设内日语、韩语的同时输出，减少每句的处理量。

## 首次下载本仓库

适用于 Windows 64 位。字幕窗使用系统自带的 .NET Framework 编译器和 WPF；无需安装 Python、.NET SDK 或 npm 包。测试脚本中的 WebSocket 集成测试另外需要 Node.js，日常使用不需要。

1. 下载本仓库后，从 [VRCT v3.6.0 官方发布页](https://github.com/misyaguziya/VRCT/releases/tag/v3.6.0)或[官方安装说明](https://misyaguziya.github.io/VRCT-Docs/docs/initial-setup/)获取程序。NVIDIA 显卡选择 CUDA 版，其他硬件使用 CPU 版。
2. 把官方便携程序的完整内容放到 `vrct/app/`，确认 `vrct/app/VRCT.exe` 存在；不能只复制单个 EXE。可选的第二套 CPU 程序放到 `vrct/app-cpu/`。
3. 先直接打开一次 `app/VRCT.exe`，完成首次设置和模型下载。界面选择简体中文，自己的语言设为简体中文，目标语言只启用 English；本地翻译选择 NLLB，语音识别选择 Whisper。本机使用 NLLB 1.3B INT8 / Whisper small；硬件较弱时可选 600M / base。
4. 在设备设置中选择自己的麦克风，以及 VRChat 使用的扬声器或耳机回环设备。CUDA 版在翻译和语音识别两处选择 GPU；不要复制其他电脑的音频设备或显卡配置。
5. 完全退出 VRCT，包括系统托盘中的程序，使它生成并保存 `app/config.json`。
6. 以后双击 `启动翻译.cmd`。入口会配置字幕分流，首次自动构建独立字幕窗，并同时启动两个程序。

配置中的连接令牌由每台电脑的 VRCT 自行生成，字幕窗直接读取本机配置，无需手动填写。

## 启动

1. 首次接入独立字幕时先退出 VRCT，再双击本目录的 **`启动翻译.cmd`**，同时打开翻译器和字幕窗。
2. VRChat 圆盘菜单打开 **Options → OSC → Enable**。
3. VRCT 设备设置中确认麦克风；扬声器选择 VRChat 正在使用的耳机输出设备。
4. 在 VRCT 主界面开启 **翻译、麦克风转写、扬声器转写**。实际按钮名称可能随界面翻译略有不同。
5. 保持 **聊天读取** 关闭，避免把画面 OCR 内容混入语音字幕。
6. 自己说中文后，英文字幕发送到游戏聊天框；对方说英语时，在独立字幕窗查看英语原文和中文译文。VRCT 主窗口可以最小化，但要保持运行。

本机需要回退时，先关闭 VRCT 和字幕窗，再双击 **`启动CPU版.cmd`**；CPU 版保留原来的 base / 600M 模型和升级前设置。两个版本不能同时运行。新下载仓库的电脑需要自行准备 CPU 程序和模型。

语言预设为中文与英语。若切换其他语言，在应用内修改目标语言即可。测试中 1.3B 模型有一条日语译文出现长段重复，因此当前只将它作为中英默认方案；600M 模型也已保留在 GPU 版的模型目录，可在设置中直接切换。

启动脚本会停止本工作区 `qqmusic-osc/bridge.cjs` 的歌词发送进程，避免两个程序覆盖同一个聊天框。QQ 音乐及 QQMusicMonitor 保持运行，歌词预览不受影响。关闭翻译后可重新运行原有歌词启动脚本。

## 配置

- 配置文件：`app/config.json`，由 VRCT 自行保存；请优先通过应用设置修改。
- CPU 回退配置：`app-cpu/config.json`。
- OSC 目标：`127.0.0.1:9000`，关闭聊天框提示音。
- 麦克风和扬声器启用 VAD 语音检测；初始设备跟随 Windows 默认设备。
- 扬声器转写采集整个所选输出设备。音乐和其他应用声音也可能被识别；使用时尽量关闭背景音乐。
- 已准备模型不需要云端语音或翻译服务。VRCT 自身仍可能联网检查更新、下载资源或发送其默认遥测。

语音播放需要另行配置 TTS 和虚拟麦克风；这里配置的是字幕翻译。

## 来源与校验

- [官方发布页](https://github.com/misyaguziya/VRCT/releases/tag/v3.6.0)
- 安装包 `VRCT_3.6.0_x64-setup.exe` SHA-256：`7177419926f693e2d020e60727ca263d4c0f17213987a9c0aa04c8bdc291e8ac`。
- CPU 原版程序包由官方安装脚本指定的 `ms-software/VRCT` Hugging Face 仓库取得，固定版本 `v3.6.0`；程序包 SHA-256：`ea13cf910774ba29459c32499810d6e68412b0c008aeed463d8700efcabaad7f`。
- GPU 程序包同样来自官方安装脚本指定仓库，文件 `VRCT_cuda.zip` 的 SHA-256：`7270b7871149f86b12845df30b283e30dceb2e6a15b106df3c8d8ae0b6423e24`。
- 当前语音模型：`Systran/faster-whisper-small`；翻译模型：`OpenNMT/nllb-200-distilled-1.3B-ct2-int8`；分词器：`facebook/nllb-200-distilled-1.3B`。
- CPU 原版使用 `Systran/faster-whisper-base` 和 `JustFrederik/nllb-200-distilled-600M-ct2-int8`。
- `downloads/model-manifests.json` 与 `downloads/gpu-upgrade-manifests.json` 记录下载时的模型提交和文件校验信息；大模型文件按发布的 SHA-256 校验。
- 官方程序、模型、下载包和个人设置均排除在 Git 之外。本仓库维护启动与分流脚本、完整字幕窗源码、构建脚本、测试和使用说明。
- VRCT 的源码和第三方组件授权以官方仓库及随程序附带的许可为准；本仓库的字幕窗通过其本机 WebSocket 接口接收结果，没有修改官方 EXE。

## 验证范围

已校验下载包、模型文件、启动脚本语法及最终 GPU/语言配置。通过 VRCT 官方后台接口，用相同 6 句中英句子在预热后测试：CPU / 600M 平均 481ms，GPU / 1.3B（`int8_float16`）平均 241ms。两组均只启用一个目标语言；测量包括文字翻译处理，不包括完整语音断句及识别时间。程序启动后显示的 GPU 精度为「自动」，由 VRCT 选择兼容类型。

麦克风与扬声器两路 Whisper small 均成功初始化，读取到的处理设备仍为 CUDA。测试时整机显存约 3998MiB / 8192MiB，包含当时其他程序的占用。验证期间关闭了两项游戏聊天框输出，测试句子未发送到 VRChat。

测试记录保存在 `downloads/benchmark-cpu-en.json`、`downloads/benchmark-gpu.json`；当前部署信息在 `app/deployment-ready.json`。原始三语对比也保留在下载目录，用于记录 1.3B 的日语重复问题。真实对话效果仍受收音、断句和游戏负载影响。

[官方首次设置说明](https://misyaguziya.github.io/VRCT-Docs/docs/initial-setup/) · [扬声器转写说明](https://misyaguziya.github.io/VRCT-Docs/docs/feature-guide/speaker-to-text/)
