# PEAK CustomVoicer

BepInEx mod for [PEAK](https://store.steampowered.com/app/3527290/PEAK/) that adds a hold-to-open voice wheel (similar to the emote wheel on `R`).

## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/)

## Install

1. Copy `PEAK.CustomVoicer.dll` to:
   ```
   PEAK/BepInEx/plugins/PEAK.CustomVoicer/
   ```
2. Place `PEAK.CustomVoicer.VoicePackTool.exe` and your audio files in the same folder (see [Voice pack](#voice-pack) below).
3. Launch the game once so BepInEx generates the config file.

## Configuration

### Voice pack

All voice content lives in the plugin folder:

```
PEAK/BepInEx/plugins/PEAK.CustomVoicer/
|-- PEAK.CustomVoicer.dll
|-- PEAK.CustomVoicer.VoicePackTool.exe
|-- voice_pack.json
|-- hello.ogg
|-- meme1.wav
`-- ...
```

Run `PEAK.CustomVoicer.VoicePackTool.exe` after adding, removing, or renaming audio files. The tool scans the same folder for `.wav`, `.ogg`, and `.mp3` files and creates or updates `voice_pack.json` automatically. Existing labels and subtitles are preserved, and the old JSON is backed up before the tool writes changes.

Advanced users can still edit `voice_pack.json` to define wheel slots (8 per page; use the mouse wheel to flip pages when you have more):

```json
{
  "name": "MyPack",
  "entries": [
    { "label": "Hello", "file": "hello.ogg", "subtitle": "optional subtitle" },
    { "label": "Meme", "file": "meme1.wav" }
  ]
}
```

| Field | Description |
|-------|-------------|
| `label` | Text shown on the wheel slot |
| `file` | Audio filename in the same folder |
| `subtitle` | Optional; logged locally when played |

Supported audio formats: `.wav`, `.ogg`, `.mp3`

Tool options:

```bash
PEAK.CustomVoicer.VoicePackTool.exe --dry-run
PEAK.CustomVoicer.VoicePackTool.exe --help
```

After changing the JSON or adding files, restart the game (or re-enter a run) to reload the pack.

### BepInEx settings

First launch creates:

```
PEAK/BepInEx/config/com.paradoxyz.peak.customvoicer.cfg
```

Open it in a text editor, or use a mod manager's config UI if available.

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| General | `Enabled` | `true` | Master toggle for the voice wheel |
| General | `VoiceWheelKey` | `Semicolon` | Hold this key to open the wheel (default: `;`) |
| General | `VoicePackFile` | `voice_pack.json` | JSON filename inside the plugin folder |
| Audio | `Volume` | `1.0` | Local fallback volume (0-1) when not streaming via Photon Voice |

**Change the hotkey:** set `VoiceWheelKey` to a Unity `KeyCode` name, for example:

```ini
[General]
VoiceWheelKey = Semicolon
```

Other examples: `V`, `B`, `LeftBracket`, `F1`. See [Unity KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) for valid names.

If you already have a config from an older version, delete the `VoiceWheelKey` line or set it to `Semicolon` manually. BepInEx keeps existing values and does not overwrite them on update.

### Build-time path (developers only)

To auto-deploy the DLL when building, create `Config.Build.user.props` in the repo root (copy from `Config.Build.user.props.template`) and set your game install:

```xml
<PeakGameRootDir>C:\Path\To\PEAK\</PeakGameRootDir>
```

Then run:

```bash
dotnet build PEAK.CustomVoicer.sln -c Release
```

### Maintainer release

Releases are built locally to avoid exposing self-hosted runner paths in public GitHub Actions logs.

Prerequisites:

- .NET SDK
- [GitHub CLI](https://cli.github.com/)
- PEAK with BepInExPack PEAK installed locally
- `PEAK_GAME_ROOT` set to the PEAK install directory

```powershell
$env:PEAK_GAME_ROOT="C:\Path\To\PEAK"
gh auth login
.\tools\release.ps1 -Version 1.2.3
```

The release script builds the solution, packages `PEAK.CustomVoicer.dll` with `PEAK.CustomVoicer.VoicePackTool`, and creates a GitHub release with the zip plus standalone DLL and exe assets. Existing tags or releases are not overwritten.

## Controls

- **Hold `;`** (semicolon, default) to open the voice wheel.
- Move the mouse to select a slot; **release the key** to close.
- **Scroll the mouse wheel** to change pages when you have more than 8 entries.

## Multiplayer

Only the player who selects a voice line needs this mod installed. Audio is streamed to other players through PEAK's built-in Photon Voice system (same proximity voice channel). Other players do not need the mod or your voice pack files.

Your normal microphone voice is unaffected. Custom clips use a separate secondary Recorder.

Quality follows voice chat encoding (Opus); best for short clips, not high-fidelity music. All players should use the same graphics API (Vulkan or DX12) if voice chat is flaky.
