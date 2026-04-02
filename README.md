# MiniGameManager

A standalone Unity package that manages mini-game definitions, lifecycle, and results. Supports JSON-authored definitions and optional integration with MapLoaderFramework, CutsceneManager, and SaveManager.

## Features

- **JSON mini-game definitions** — load from `Resources/MiniGames/*.json` and `persistentDataPath/MiniGames/`
- Launch / complete / abort lifecycle with `OnMiniGameStarted`, `OnMiniGameCompleted`, `OnMiniGameAborted` events
- Per-mini-game result tracking: score, completed flag, timestamp
- `canReplay` toggle — prevents re-launching already-completed mini-games
- `LaunchCallback` delegate — plug in your own scene loader or prefab instantiator
- `MiniGameTrigger` component — launch mini-games from scene triggers without code
- **Optional** MapLoaderFramework bridge — aborts active mini-game on map change; auto-registers mod `minigames/*.json` files when mods change (`MINIGAMEMANAGER_MLF`)
- **Optional** CutsceneManager bridge — launch / complete / abort via cutscene custom-event payloads (`MINIGAMEMANAGER_CSM`)
- **Optional** SaveManager bridge — persist completion results across sessions (`MINIGAMEMANAGER_SM`)
- **Optional** LocalizationManager bridge — resolve localized mini-game titles and descriptions via `titleLocalizationKey` / `descriptionLocalizationKey` fields (`MINIGAMEMANAGER_LM`)


## Installation

### A — Unity Package Manager (Git URL)

```
https://github.com/rolandkaechele/com.rolandkaechele.minigamemanager.git
```

### B — Local disk

Place the `MiniGameManager/` folder anywhere under your project's `Assets/` directory.

### C — npm / postinstall

```bash
npm install
```

`postinstall.js` creates the required runtime folders.


## Folder Structure

```
MiniGameManager/
├── Runtime/
│   ├── MiniGameData.cs               # MiniGameData, MiniGameResult
│   ├── MiniGameManager.cs            # Manager MonoBehaviour + JSON loader
│   ├── MiniGameTrigger.cs            # Scene trigger component
│   ├── MapLoaderMiniGameBridge.cs    # Optional: MLF integration
│   ├── CutsceneMiniGameBridge.cs     # Optional: CutsceneManager integration
│   ├── SaveMiniGameBridge.cs         # Optional: SaveManager integration
│   └── LocalizationMiniGameBridge.cs # Optional: LocalizationManager integration
├── Editor/
│   └── MiniGameManagerEditor.cs      # Custom inspector
├── Examples/
│   └── MiniGames/
├── package.json
├── postinstall.js
├── LICENSE
└── README.md
```


## Quick Start

### 1. Scene Setup

Add `MiniGameManager` to a persistent manager GameObject.

### 2. Define a Mini-Game (JSON)

Place one `.json` file per mini-game in `Assets/Resources/MiniGames/`:

```json
{
  "id": "sorting_puzzle",
  "title": "Reactor Sorting Puzzle",
  "description": "Sort the reactor rods in the correct order.",
  "sceneOrPrefab": "MiniGames/SortingPuzzle",
  "category": 0,
  "canReplay": false,
  "trackScore": true,
  "minPassScore": 100
}
```

### 3. Launch from Code

```csharp
var mgr = FindFirstObjectByType<MiniGameManager.Runtime.MiniGameManager>();

// Assign a launcher (e.g. load an additive scene)
mgr.LaunchCallback = id =>
{
    var data = mgr.GetData(id);
    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(data.sceneOrPrefab, LoadSceneMode.Additive);
};

// Subscribe to results
mgr.OnMiniGameCompleted += result =>
    Debug.Log($"{result.miniGameId} completed with score {result.score}");

// Launch
mgr.Launch("sorting_puzzle");

// Complete (call this from inside the mini-game scene)
mgr.Complete("sorting_puzzle", score: 250);
```

### 4. MiniGameTrigger Component

Add `MiniGameTrigger` to any scene object to launch without code:

| Field | Description |
| ----- | ----------- |
| `Mini Game Id` | ID of the mini-game to launch |
| `Trigger Mode` | `OnStart`, `OnTriggerEnter`, `OnInteract` |
| `Require Flag Not Set` | Optional flag checked by SaveMiniGameBridge |
| `Trigger Tag` | Collider tag filter (default: `"Player"`) |
| `Disable After Trigger` | Disable this GameObject after triggering |

Call `trigger.Interact()` from code for `OnInteract` mode.


## MiniGameData JSON Fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | string | Unique identifier |
| `title` | string | Human-readable title |
| `description` | string | Short description |
| `titleLocalizationKey` | string | Localization key for the title |
| `descriptionLocalizationKey` | string | Localization key for the description |
| `sceneOrPrefab` | string | Scene name or Resources path for the mini-game |
| `previewIconResource` | string | Resources-relative path to preview icon |
| `category` | int | `Puzzle=0`, `Action=1`, `Racing=2`, `Shooting=3`, `Platformer=4`, `Custom=5` |
| `unlockCondition` | string | Save-flag that must be set to unlock (empty = always available) |
| `canReplay` | bool | Allow replaying after completion |
| `trackScore` | bool | Whether score is tracked for win/loss |
| `minPassScore` | int | Minimum score for a successful completion |


## JSON File Locations

| Content | Bundled path | Hot-reload / mod path |
| ------- | ------------ | --------------------- |
| Mini-game definitions | `Assets/Resources/MiniGames/*.json` | `persistentDataPath/MiniGames/*.json` |

Toggle hot-reload with **Load From Persistent Data Path** on the `MiniGameManager` component.


## Runtime API

| Member | Description |
| ------ | ----------- |
| `LoadAllMiniGames()` | Reload all definitions |
| `Launch(string id)` | Start a mini-game |
| `Complete(string id, int score)` | Record completion and fire `OnMiniGameCompleted` |
| `Abort(string id)` | Abort without recording a result |
| `IsPlaying` | True while a mini-game is active |
| `ActiveMiniGameId` | Id of the running mini-game, or `null` |
| `HasCompleted(string id)` | True if completed at least once |
| `GetData(string id) → MiniGameData` | Look up a definition |
| `GetResult(string id) → MiniGameResult` | Look up the latest result |
| `GetAllMiniGames()` | All loaded definitions |
| `GetAllResults()` | All recorded results |
| `OnMiniGameStarted` | Event: launched (id) |
| `OnMiniGameCompleted` | Event: completed (MiniGameResult) |
| `OnMiniGameAborted` | Event: aborted (id) |
| `LaunchCallback` | Delegate to handle scene/prefab loading |


## MapLoaderFramework Integration

Enable `MINIGAMEMANAGER_MLF` in Player Settings › Scripting Define Symbols.

Add `MapLoaderMiniGameBridge` to the same GameObject as `MiniGameManager`.

| Inspector Field | Default | Description |
| --------------- | ------- | ----------- |
| `Abort On Map Load` | `true` | Abort the active mini-game when a new map loads |
| `Reload On Mods Changed` | `true` | Re-register mini-game definitions from enabled mod `minigames/` subfolders when mods change |

Mod mini-game JSON files are loaded from each mod's `minigames/` subfolder (e.g. `Mods/my_mod/minigames/bonus_race.json`). Place `minigame_files` entries in `mod_manifest.json` to declare them.


## CutsceneManager Integration

Enable `MINIGAMEMANAGER_CSM` in Player Settings › Scripting Define Symbols.

Add `CutsceneMiniGameBridge` to the same GameObject as `MiniGameManager` and `CutsceneManager`.

Use these strings in cutscene `Custom` step payloads:

| Payload | Action |
| ------- | ------ |
| `"minigame.launch:sorting_puzzle"` | Launch the mini-game |
| `"minigame.complete:sorting_puzzle"` | Complete with score 0 |
| `"minigame.complete:sorting_puzzle:250"` | Complete with score 250 |
| `"minigame.abort:sorting_puzzle"` | Abort the mini-game |


## SaveManager Integration

Enable `MINIGAMEMANAGER_SM` in Player Settings › Scripting Define Symbols.

Add `SaveMiniGameBridge` to the same GameObject as `MiniGameManager` and `SaveManager`.

| Feature | Description |
| ------- | ----------- |
| `SaveResults()` | Persist all results to the active save slot |
| `LoadResults()` | Restore results from the active save slot |
| `Auto Save On Complete` | Automatically persist on every completion |
| Flag gating | `MiniGameTrigger.ConditionCheck` wired to `SaveManager.IsSet` |


## LocalizationManager Integration

Enable `MINIGAMEMANAGER_LM` in Player Settings › Scripting Define Symbols.

Add `LocalizationMiniGameBridge` to any GameObject. It provides convenience helpers that resolve localized strings and fall back to the raw field values when no localization key is set.

```csharp
var bridge = FindFirstObjectByType<LocalizationMiniGameBridge>();

// Look up by id
string title = bridge.GetTitle("sorting_puzzle");
string desc  = bridge.GetDescription("sorting_puzzle");

// Or pass the data object directly
string title2 = bridge.GetTitle(data);
```

| Method | Description |
| ------ | ----------- |
| `GetTitle(string id)` | Localized title for the mini-game, or raw `title` as fallback |
| `GetTitle(MiniGameData data)` | Same, given a `MiniGameData` object |
| `GetDescription(string id)` | Localized description, or raw `description` as fallback |
| `GetDescription(MiniGameData data)` | Same, given a `MiniGameData` object |


## Integration Defines Summary

| Define | Effect |
| ------ | ------ |
| `MINIGAMEMANAGER_MLF` | Aborts active mini-game on map change; loads mod `minigames/*.json` on mod reload |
| `MINIGAMEMANAGER_CSM` | CutsceneManager custom-event commands |
| `MINIGAMEMANAGER_SM` | Persist results via SaveManager |
| `MINIGAMEMANAGER_LM` | Localized title/description lookup via LocalizationManager |


## Dependencies

| Dependency | Role |
| ---------- | ---- |
| Unity 2022.3+ | Required |
| MapLoaderFramework | Optional — enable `MINIGAMEMANAGER_MLF` |
| CutsceneManager | Optional — enable `MINIGAMEMANAGER_CSM` |
| SaveManager | Optional — enable `MINIGAMEMANAGER_SM` |
| LocalizationManager | Optional — enable `MINIGAMEMANAGER_LM` |


## Repository

`https://github.com/RolandKaechele/MiniGameManager`


## License

MIT — see [LICENSE](LICENSE)
