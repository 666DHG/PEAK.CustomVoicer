# PEAK CustomVoicer

[English](README.md) | **简体中文**

这是一个用于 [PEAK](https://store.steampowered.com/app/3527290/PEAK/) 的 BepInEx mod，会添加一个按住呼出的语音轮盘，用来播放本地音频片段。

在播放音频时，音频会混入 PEAK 原本的 Photon Voice 语音流，其他玩家会通过正常的距离语音频道听到，不需要安装 mod，也不需要你的语音包文件。

本 mod 更适合短语音和音效片段。音质会受 PEAK 语音聊天编码影响。

## 前置

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/)

## 安装

1. 将 `PEAK.CustomVoicer.dll` 复制到：
   ```text
   PEAK/BepInEx/plugins/PEAK.CustomVoicer/
   ```
2. 将 `PEAK.CustomVoicer.VoicePackTool.exe` 和你的音频文件放到同一目录。
3. 运行 `PEAK.CustomVoicer.VoicePackTool.exe`，生成或更新 `voice_pack.json`。
4. 启动游戏。

## 语音包

语音内容和插件放在同一目录：

```text
PEAK/BepInEx/plugins/PEAK.CustomVoicer/
|-- PEAK.CustomVoicer.dll
|-- PEAK.CustomVoicer.VoicePackTool.exe
|-- voice_pack.json
|-- hello.ogg
|-- meme1.wav
`-- ...
```

工具会扫描 `.wav`、`.ogg` 和 `.mp3` 文件，并更新 `voice_pack.json`。已有的标签和字幕会被保留。

也可以手动编辑 `voice_pack.json`：

```json
{
  "name": "MyPack",
  "entries": [
    { "label": "Hello", "file": "hello.ogg", "subtitle": "optional subtitle" },
    { "label": "Meme", "file": "meme1.wav" }
  ]
}
```

- `label`：轮盘上显示的文字
- `file`：插件目录中的音频文件名
- `subtitle`：可选，本地日志文本

修改语音包后，需要重启游戏，或重新进入一局游戏来重新加载。

## 操作

- 默认按住 `;` 打开语音轮盘。

## 配置

BepInEx 会在首次启动后创建配置文件：

```text
PEAK/BepInEx/config/com.paradoxyz.peak.customvoicer.cfg
```

常用配置：

| Key | 默认值 | 说明 |
| --- | --- | --- |
| `Enabled` | `true` | 是否启用语音轮盘 |
| `VoiceWheelKey` | `Semicolon` | 呼出轮盘的按键 |
| `VoicePackFile` | `voice_pack.json` | 插件目录中的语音包文件 |
| `Volume` | `1.0` | 仅本地监听音量 |
| `NormalizeVoiceClips` | `true` | 加载时在内存中归一化音频 |
| `TargetRmsDb` | `-18` | RMS 目标响度，单位 dBFS |
| `StreamVolume` | `0.8` | 发送到 Photon Voice 的音量 |

`VoiceWheelKey` 使用 Unity `KeyCode` 名称，例如 `V`、`B`、`LeftBracket` 或 `F1`。

`Volume` 只影响你本地听到的监听音量；`StreamVolume` 影响其他玩家通过 Photon Voice 听到的音量。归一化只在加载时于内存中处理，不会修改你的原始音频文件。

## 开发

复制模板创建 `Config.Build.user.props`，并设置 PEAK 安装路径：

```xml
<PeakGameRootDir>C:\Path\To\PEAK\</PeakGameRootDir>
```

构建并自动部署：

```bash
dotnet build PEAK.CustomVoicer.sln -c Release
```
