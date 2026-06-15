# PEAK CustomVoicer

BepInEx mod for [PEAK](https://store.steampowered.com/app/3527290/PEAK/) that adds a hold-to-open voice wheel (similar to the emote wheel on `R`).

## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/)
- Game path configured in `Config.Build.user.props` for local builds

## Install

1. Build the project or copy `PEAK.CustomVoicer.dll` to:
   `PEAK/BepInEx/plugins/PEAK.CustomVoicer/`
2. Place `voice_pack.json` and your audio files in the same folder.
3. Launch the game once to generate `BepInEx/config/com.paradoxyz.peak.customvoicer.cfg`.

## Voice pack format

```json
{
  "name": "MyPack",
  "entries": [
    { "label": "Hello", "file": "hello.ogg", "subtitle": "optional" }
  ]
}
```

Supported formats: `.wav`, `.ogg`, `.mp3`

## Controls

- Hold the configured key (default `V`) to open the voice wheel.
- Move the mouse to select a slot; release the key to close.
- Scroll the mouse wheel to change pages when you have more than 8 entries.

## Multiplayer

Only the player who selects a voice line needs this mod installed. Audio is streamed to other players through PEAK's built-in Photon Voice system (same proximity voice channel). Other players do not need the mod or your voice pack files.

Your normal microphone voice is unaffected — custom clips use a separate secondary Recorder.

## Build

```bash
dotnet build PEAK.CustomVoicer.sln -c Release
```

The DLL is copied automatically to your PEAK `BepInEx/plugins/PEAK.CustomVoicer/` folder when `Config.Build.user.props` points at your game install.
