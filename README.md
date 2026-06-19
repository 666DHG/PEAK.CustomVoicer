# PEAK CustomVoicer

**English** | [简体中文](README.zh-CN.md)

BepInEx mod for [PEAK](https://store.steampowered.com/app/3527290/PEAK/) that adds a hold-to-open voice wheel for local audio clips.

Only the player who selects a voice line needs the mod installed. Clips are mixed into PEAK's normal Photon Voice stream, so other players hear them through the usual proximity voice channel and do not need your voice pack files.

This is best for short voice lines and sound bites. Audio quality follows PEAK voice chat encoding.

## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/)

## Install

1. Copy `PEAK.CustomVoicer.dll` to:
   ```text
   PEAK/BepInEx/plugins/PEAK.CustomVoicer/
   ```
2. Put `PEAK.CustomVoicer.VoicePackTool.exe` and your audio files in the same folder.
3. Run `PEAK.CustomVoicer.VoicePackTool.exe` to create or update `voice_pack.json`.
4. Start the game.

## Voice Packs

Voice content lives next to the plugin:

```text
PEAK/BepInEx/plugins/PEAK.CustomVoicer/
|-- PEAK.CustomVoicer.dll
|-- PEAK.CustomVoicer.VoicePackTool.exe
|-- voice_pack.json
|-- hello.ogg
|-- meme1.wav
`-- ...
```

The tool scans `.wav`, `.ogg`, and `.mp3` files and updates `voice_pack.json`. Existing labels and subtitles are preserved.

You can also edit `voice_pack.json` manually:

```json
{
  "name": "MyPack",
  "entries": [
    { "label": "Hello", "file": "hello.ogg", "subtitle": "optional subtitle" },
    { "label": "Meme", "file": "meme1.wav" }
  ]
}
```

- `label`: text shown on the wheel
- `file`: audio filename in the plugin folder
- `subtitle`: optional local log text

Restart the game, or re-enter a run, after changing the pack.

## Controls

- Hold `;` by default to open the voice wheel.

## Config

BepInEx creates this file on first launch:

```text
PEAK/BepInEx/config/com.paradoxyz.peak.customvoicer.cfg
```

Useful settings:

| Key | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Enable the voice wheel |
| `VoiceWheelKey` | `Semicolon` | Hold key for the wheel |
| `VoicePackFile` | `voice_pack.json` | Pack file in the plugin folder |
| `Volume` | `1.0` | Local monitor volume only |
| `NormalizeVoiceClips` | `true` | Normalize clips in memory before playback and streaming |
| `TargetRmsDb` | `-18` | Target RMS loudness in dBFS |
| `StreamVolume` | `0.8` | Volume applied to clips sent through Photon Voice |

`VoiceWheelKey` uses Unity `KeyCode` names, such as `V`, `B`, `LeftBracket`, or `F1`.

`Volume` only controls what you hear locally. `StreamVolume` controls what other players receive through Photon Voice. Normalization is applied at load time in memory and does not modify your original audio files.

## Developers

Create `Config.Build.user.props` from the template and set your PEAK install path:

```xml
<PeakGameRootDir>C:\Path\To\PEAK\</PeakGameRootDir>
```

Build and auto-deploy:

```bash
dotnet build PEAK.CustomVoicer.sln -c Release
```
