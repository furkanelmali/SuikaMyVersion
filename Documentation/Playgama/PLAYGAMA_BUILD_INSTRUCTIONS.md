# Merge & Munch: Playgama WebGL build instructions

Branch: `playgama`. Editor: **Unity 6000.4.8f1** with **WebGL Build Support**.

## What is in the project
| Piece | Location |
|---|---|
| Unity MCP | `Packages/manifest.json` → `com.coplaydev.unity-mcp` (v10.0.0), client config in `.mcp.json` (instance `SuikaMyVersion`) |
| Playgama Bridge SDK | `Packages/manifest.json` → `com.playgama.bridge` = `bridge-unity.git#v2.4.0` (pinned) |
| Bridge WebGL template | `Assets/WebGLTemplates/Bridge/` (official files plus title, touch/long-press guards, hidden-tab audio backstop) |
| Bridge config (ad placements, leaderboard) | `Assets/WebGLTemplates/Bridge/playgama-bridge-config.json` |
| Platform layer | `Assets/Scripts/Platform/`: `PlaygamaService` (storage, pause/audio, lifecycle), `GamePrefs` (replaces PlayerPrefs), `LeaderboardService` (best-score board, optional sign-in) |
| Ads | `Assets/Scripts/AdsManager.cs`: Bridge on WebGL, AdMob on Android/iOS |
| Game UI | `Assets/Scripts/UIManager.cs` (screens), `Assets/Scripts/UI/` (press/pop feel, aspect match). Built into the scene by `Assets/Scripts/Editor/GameUIBuilder.cs`; sprites and icons drawn by `UISpriteFactory.cs` into `Assets/UI/Generated/` |
| Design source | Design canvas "Merge & Munch UI" (HUD, pause + settings, game over, leaderboard, main menu) |
| Size cleanup | `Assets/Scripts/Editor/PlaygamaSceneSetup.cs` (`Strip Unused Heavy Content`, already applied) |
| Build script | `Assets/Scripts/Editor/PlaygamaWebBuild.cs` |

All Bridge calls are inside `#if UNITY_WEBGL`. Android/iOS builds keep PlayerPrefs and AdMob as before.

## Instant start
A fresh launch (and **Restart**) puts the player straight into a run: no main menu and no Unity splash screen.
The main menu only appears when the player taps **Home** (`UIManager.openMenuOnLoad`). This applies to every platform.

## Rebuilding the UI
Menu **Merge&Munch → UI → Build Scene UI** regenerates the sprites and rebuilds the `UI` canvas in `SampleScene`
(the old canvas is replaced, fields on `UIManager`, `GameManager` and `ObjectSpawner` are rewired). Hand edits to the
`UI` canvas are lost on a rebuild, so change `GameUIBuilder` instead.

## Leaderboard
Board id `best_score` (`isMain: true`) is declared in the Bridge config. The game submits the stored best at every
game over and after an optional sign-in. Depending on the platform's leaderboard type:
`in_game` opens the game's own RANKING screen, `native_popup` opens the platform window, `native` only submits,
`not_available` hides the RANKING buttons. **Create the `best_score` leaderboard in the Playgama developer console**
before testing it there.

## Build from the editor
1. `File → Build Profiles` → **Web** → **Switch Platform**.
2. Menu **Merge&Munch → Playgama → Build WebGL + ZIP**. This applies the WebGL player settings, builds
   `Assets/Scenes/SampleScene.unity` to `Builds/Playgama/WebGL/`, checks the Bridge files, and zips them to
   `Builds/Playgama/MergeAndMunch_Playgama_v1.0.zip` with `index.html` at the root.

## Build from the command line (editor closed)
```sh
"/Applications/Unity/Hub/Editor/6000.4.8f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -buildTarget WebGL \
  -projectPath "$PWD" -executeMethod PlaygamaWebBuild.Build -quit -logFile /tmp/mm-playgama-build.log
```
Success markers in the log: `MM_PLAYGAMA_CONFIGURE_OK`, `MM_PLAYGAMA_BUILD Succeeded`, `MM_PLAYGAMA_ZIP_OK`.

## WebGL player settings applied by `PlaygamaWebBuild.Configure`
- Template `PROJECT:Bridge`, splash screen off
- **Gzip** compression, **decompression fallback on** (Playgama 5.1.1), **data caching off** (Playgama 3.2.1: no IndexedDB)
- Exceptions: explicitly thrown only, no diagnostics overlay
- Memory: 64 MB initial, 1024 MB max, geometric growth
- Managed stripping **Medium**, IL2CPP *optimize size*, *Master*
- WebGL quality level = the Android one (*Low*)

## Local test
```sh
cd Builds/Playgama/WebGL && python3 -m http.server 8080
```
Open `http://localhost:8080/`. Bridge runs its mock platform on localhost. The Playgama Developer Console's
**Test Game** step is the authoritative check for ads and storage.

## Playgama console checklist (manual)
1. Upload `MergeAndMunch_Playgama_v1.0.zip` as a new game draft.
2. Orientation **Portrait**, language **English**, title **Merge & Munch**.
3. Run **Test Game / QA** in the console before *Submit*.
