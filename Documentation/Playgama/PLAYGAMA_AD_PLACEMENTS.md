# Merge & Munch: Playgama ad placements

All web ads go through Playgama Bridge (`com.playgama.bridge` v2.4.0). The placements are declared in
`Assets/WebGLTemplates/Bridge/playgama-bridge-config.json`. AdMob (`GoogleMobileAds` 9.5.0) is still used on
Android/iOS, but it is compiled out of the WebGL build (`#if !UNITY_WEBGL` in `AdsManager`).

| Placement id | Type | Trigger (real game event) | Code |
|---|---|---|---|
| `game_over` | Interstitial | The run ends: the end sequence finishes, then the ad shows before the game over screen. This is the same moment the mobile AdMob interstitial uses. | `UIManager.GameOverScene` → `AdsManager.TryShowInterstitial` |
| `continue` | Rewarded | Game over screen: the player taps **CONTINUE · WATCH AN AD**. The run keeps its score and play resumes with the cleared board and a new piece. Offered once per run. | `UIManager.WatchAndContinue` → `GameManager.ContinueRun` |
| `chef_break` | Interstitial | **Chef's break** card. After 180 s of play without an ad, the next merge that creates a rank 5+ piece freezes the game on a card (score, CONTINUE, "A SHORT AD MAY PLAY"). The ad runs only after the player taps CONTINUE. | `UIManager.OnPieceMerged` → `UpdateAdBreaks` → `ContinueFromBreak` |
| `pause_exit` | Interstitial | Pause menu: the player taps **RESTART** or **HOME**. The ad runs before leaving. | `UIManager.LeavePause` |
| `cleanup` | Rewarded | HUD button **CLEAN UP · WATCH AN AD**, shown once about 10 pieces are on the board, with a 150 s cooldown after use. Pops the 8 smallest pieces and restarts the loss countdown. No score is awarded. | `UIManager.WatchAndCleanup` → `GameManager.ClearSmallestPieces` |
| — | Banner | Not used on web. The mobile menu banner is unchanged. | — |

## Rules applied
- **Never during play.** There is no ad call while a piece can be dropped: every ad runs on a frozen game, after a
  game over, a player tap on a card or an opt-in button, or a pause menu exit. Opening the game, the first drop, pausing,
  resuming and Play Again have no ad.
- **The chef's break needs a calm moment.** The card waits until no piece is in the player's hand and the board is not
  in the loss countdown. Pausing cancels a waiting card, so it never appears right after Resume.
- **Moderation note.** Playgama's requirements name levels, game end and pause as logical pauses and do not cover endless
  games. The chef's break is this game's own level-like break. If moderation objects, set
  `AdsManager.BreakCardsEnabled` to `false`, which removes only the card.
- **Frequency.** Bridge applies the platform's initial delay and minimum interval (60 s by default) to every
  interstitial and reports a skipped request as `Failed`. The game's only timer is `AdsManager.PlaySinceAd`, which spaces
  the chef's break cards: it counts actual play and resets on any ad or card.
- **Pause during ads.** On `Opened`, `PlaygamaService.SetAdOpen(true)` pauses `AudioListener` and sets
  `Time.timeScale` to 0 (through `ExternalPauseChanged`). On `Closed`/`Failed` both are restored.
- **Never blocks.** If interstitials are unsupported, the game over screen opens at once, and the card and pause exit
  ads are skipped. If Bridge sends no state
  within 0.6 s (request skipped by frequency rules) or stays loading for 8 s, the screen opens anyway.
- **Rewarded is opt-in and honest.** Both buttons say they show an ad and what they give. They are hidden when
  `isRewardedSupported` is false and while an ad is running. Continue is offered once per run (a recovery ad, Playgama
  2.2.4). Cleanup is a booster (2.2.3), limited by its cooldown.
- **Reward only on `Rewarded`.** Continue and cleanup are applied only if Bridge reported `RewardedState.Rewarded`, and only
  after the ad closed, so play never resumes behind the ad. `Closed`/`Failed` without `Rewarded` change nothing.
  `continueUsed` guards against a double continue payout.
- **Not required to progress.** Declining the offer leaves Restart/Home exactly as before.

## Platform messages
`game_ready` is sent after the first playable frame. `level_started` is sent at run start and after a continue,
`level_paused`/`level_resumed` when the pause panel, the chef's break or a tapped ad freezes and releases play, and `level_failed` at game over.
