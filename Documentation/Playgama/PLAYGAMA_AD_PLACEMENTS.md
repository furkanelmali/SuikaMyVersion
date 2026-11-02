# Merge & Munch: Playgama ad placements

All web ads go through Playgama Bridge (`com.playgama.bridge` v2.4.0). The two placements are declared in
`Assets/WebGLTemplates/Bridge/playgama-bridge-config.json`. AdMob (`GoogleMobileAds` 9.5.0) is still used on
Android/iOS, but it is compiled out of the WebGL build (`#if !UNITY_WEBGL` in `AdsManager`).

| Placement id | Type | Trigger (real game event) | Code |
|---|---|---|---|
| `game_over` | Interstitial | The run ends: the end sequence finishes, then the ad shows before the game over screen. This is the same moment the mobile AdMob interstitial uses. | `UIManager.GameOverScene` → `AdsManager.TryShowInterstitial` |
| `continue` | Rewarded | Game over screen: the player taps **CONTINUE · WATCH AN AD**. The run keeps its score and play resumes with the cleared board and a new piece. Offered once per run. | `UIManager.WatchAndContinue` → `GameManager.ContinueRun` |
| — | Banner | Not used on web. The mobile menu banner is unchanged. | — |

## Rules applied
- **Never during play.** There is no ad call while a piece can be dropped. Opening the game, the first drop, pause and
  restart have no ad.
- **No game-side frequency logic.** Each game over asks Bridge for `game_over`, and Bridge applies the platform's
  initial delay and minimum interval. The game has no timer or counter.
- **Pause during ads.** On `Opened`, `PlaygamaService.SetAdOpen(true)` pauses `AudioListener` and sets
  `Time.timeScale` to 0 (through `ExternalPauseChanged`). On `Closed`/`Failed` both are restored.
- **Never blocks.** If interstitials are unsupported, the game over screen opens at once. If Bridge sends no state
  within 0.6 s (request skipped by frequency rules) or stays loading for 8 s, the screen opens anyway.
- **Rewarded is opt-in and honest.** The button says it shows an ad. It is hidden when `isRewardedSupported` is false,
  after it was used once in the run, and while the ad is running.
- **Reward only on `Rewarded`.** The continue is applied only if Bridge reported `RewardedState.Rewarded`, and only
  after the ad closed, so play never resumes behind the ad. `Closed`/`Failed` without `Rewarded` change nothing.
  `continueUsed` guards against a double payout.
- **Not required to progress.** Declining the offer leaves Restart/Home exactly as before.

## Platform messages
`game_ready` is sent after the first playable frame. `level_started` is sent at run start and after a continue,
`level_paused`/`level_resumed` when the pause panel opens or closes, and `level_failed` at game over.
