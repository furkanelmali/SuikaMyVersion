# Merge & Munch: Playgama WebGL release report

Branch `playgama` (from `main` @ `3f91274`). Date 2026-11-02. Unity 6000.4.8f1 (built-in pipeline), Playgama Bridge Unity
**v2.4.0**, Unity MCP **v10.0.0**. Same approach as the Cube Invasion release.

> **Verdict: ready for the Playgama console's Test Game step.** Real ads, the real leaderboard, real devices and
> browsers other than Chromium still need to be checked there before submitting.

## A. What was done
**MCP.** `com.coplaydev.unity-mcp` (v10.0.0) is in `Packages/manifest.json`, and `.mcp.json` points the `UnityMCP` server at the
`SuikaMyVersion` instance. Open the project in the editor and restart Claude Code to use it.

**Instant start (hook).** The game opens straight into a run: no main menu and no Unity splash screen. The first
piece is in the dropper on the first frame. Restart also goes straight back into play. The main menu only appears from
**Home**. Previously the menu showed on every launch, and the `ResetNum` flag lived in PlayerPrefs.

**Platform layer (new):** `Assets/Scripts/Platform/`
- `PlaygamaService`: creates the Bridge receiver before the first scene, loads the `mm_save` key in one `storage.get`
  (play starts at once; only saved values wait for it), batches writes and retries failed ones, handles
  `audioStateChanged`/`pauseStateChanged` and focus, and sends `game_ready` and the `level_*` messages.
- `GamePrefs`: replaces PlayerPrefs in game code (PlayerPrefs on device, one Bridge key on web): high score,
  daily best, sound/music, haptics.
- `LeaderboardService`: `best_score` board, entries, native popup and optional sign-in.

**Ads.** `AdsManager` is the single ad entry: AdMob on Android/iOS (unchanged), Bridge on WebGL (AdMob compiled out).
Placements are in `PLAYGAMA_AD_PLACEMENTS.md`.

**UI, rebuilt from scratch.** The old canvas (menus, fake random-score leaderboard, dead login/register panels and
the broken "Watch&Contunie" button that muted the sound) was deleted. The new UI follows the "Merge & Munch UI" design
canvas, a warm pastel kitchen style. It uses Lilita One (OFL) and sprites and icons generated in code
(`UISpriteFactory`), with no third-party UI pack. Screens:
- **HUD**: score card, best chip, next-piece bubble, pause button.
- **Pause + settings**: resume, sound and music toggles (saved), restart, home. Opening it freezes physics and the loss
  timer; before, the board kept falling while paused.
- **Game over**: final score, NEW BEST chip, rewarded **CONTINUE · WATCH AN AD**, PLAY AGAIN, home, ranking.
- **Leaderboard (new)**: top 7 with medals, the player's own row, and optional **SIGN IN**.
- **Main menu** (via Home): title, best, PLAY, ranking, sound and music.
The layout is designed at 390×844, matches width on phones and height on wider screens, and was checked at 390×844,
768×1024 and a landscape desktop pane.

**Removed for web compliance or size**
- `WorldTimeAPI` HTTP call (third-party host, plain http) is skipped on WebGL.
- Inactive scene objects `kitchenrender` (45 MB mesh), `Plane` and the reference screenshot (9.6 MB texture) are
  removed. `GUIPackCartoon/Demo/Resources` is renamed to `DemoResources`, which stops about 70 MB of demo art being
  forced into every build (the game never loads from it).
- Legacy scripts `BannerAd`, `InterstitialAdd` and `LeaderBoard` (random fake scores) are deleted.

**WebGL player settings** (`PlaygamaWebBuild.Configure`): Bridge template, no splash, Gzip + decompression fallback,
data caching off, 64→1024 MB memory, Medium stripping, IL2CPP size/Master, WebGL quality = Android's (Low).
Android/iOS player settings are unchanged, except that the splash screen is off for all platforms (it is a global setting).

**Backdrop and game feel (second pass, DOTween).**
- The fixed-width kitchen picture is replaced by a full-bleed kitchen backdrop (design canvas "Full-bleed kitchen
  backdrop"). Wall, wallpaper, bunting, tiles, counter and cabinet bands repeat horizontally, and a pendant lamp is
  centred, so there are no side gaps at any width. It was checked at 390×844, 4:3, 16:9 and 2:1. Textures are drawn in
  code by `BackdropFactory`, and the scene is built by `KitchenSceneBuilder` (run through **Build Scene UI**).
- Visible container: posts on the previously invisible walls, a dashed limit line at the real loss height (it turns red
  and pulses with danger), and a dashed drop guide from the held piece to where it would land. `CameraFit` widens the
  view on portrait phones so both posts stay on screen; before, the container edges were off screen on phones.
- Juice: the new piece pops into the dropper and the dropper bounces on spawn and drop; the NEXT bubble bounces;
  merge scores float in the game font with an outline and grow with the points; chained merges show "COMBO xN" with
  rising merge pitch; the HUD score counts up and the card bumps; beating the best mid-run shows "NEW BEST!".
- UI motion: cards drop in with a springy overshoot, dims fade, menu items pop in one after another, PLAY and CONTINUE
  breathe, the HUD slides in at run start, and buttons squash and spring back. The game over score counts up, a new
  best gets confetti, leaderboard rows pop in, and every button clicks (follows the sound toggle). All UI tweens run
  on unscaled time, so they work while paused.
- Fixes found on the way: the dropper's own mesh collider blocked the guide raycast; a press during the spawn pop could
  leave a piece at a partial scale (`DOKill(true)` / `DOComplete`).

## B. Ads and leaderboard
| Id | Type | Trigger |
|---|---|---|
| `game_over` | Interstitial | Run ended, before the game over card (same moment as the mobile AdMob interstitial) |
| `continue` | Rewarded | Game over card → CONTINUE · WATCH AN AD; once per run; play resumes with the score kept |
| `best_score` | Leaderboard (`isMain`) | Best score submitted at every game over and after sign-in |

## C. Size
| Build | ZIP |
|---|---|
| First WebGL build (old UI, before cleanup) | 62.0 MB |
| After cleanup | 21.4 MB |
| New UI | 20.7 MB |
| **Final (backdrop + juice)** | **19.6 MB**; limit 300 MB |

## D. Tests run
- Compile: WebGL, Android and iOS player scripts (`PlaygamaWebBuild.CheckMobileCompile`), 0 errors.
- Browser (local Bridge mock, Chromium): the game opens straight into play; drag/drop and merging work; the best score
  persists across reloads through Bridge storage; pause freezes the run and resume continues it; sound/music toggles
  persist across scene reloads; Home → main menu → PLAY works; no console errors.
- Ad and leaderboard flows were exercised with JS stubs of Bridge's events (the mock reports ads and leaderboards
  as unsupported): `game_over` interstitial → card; `continue` rewarded → reward after close → play resumes, offered
  only once; the unsupported case hides CONTINUE and RANKING; leaderboard entries, medals, the player's own row,
  sign-in → submit → refresh.
- `?platform_id=playgama` cannot start outside the portal (Bridge init timeout), as with Cube Invasion.
- **Not tested:** real ads, the real leaderboard, real phones, Safari/Firefox, FPS and memory.

## E. Developer steps left
1. In the Playgama console: create the **`best_score`** leaderboard, then upload
   `Builds/Playgama/MergeAndMunch_Playgama_v1.0.zip` (Portrait, English, title **Merge & Munch**).
2. Run **Test Game** and check the ads, leaderboard and sign-in. Then submit.

## F. Open issues
1. A tap on the canvas right after page load without any prior pointer movement can read position (0,0) in Unity WebGL
   (seen only with automation; real mice and touches carry positions).
2. Post-processing v2 logs "shader not supported" for its debug shaders on WebGL (log lines only).
3. The `_Recovery/*.unity` autosave scenes still reference the deleted legacy scripts (not in the build).
