<!-- .github/copilot-instructions.md: Guidance for AI coding agents on this Unity project -->
# Copilot Instructions — Again (Unity project)

Purpose: help AI coding agents become productive quickly by surfacing the project's architecture, conventions, and developer workflows that are discoverable in the repository.

Big picture
- Unity Editor project. Source lives in `Assets/`, engine config in `ProjectSettings/`, and package metadata in `Packages/`.
- Scenes and runtime orchestration use Unity's `SceneManager` and a small set of utility singletons (see `Assets/ScreenFader.cs`).

Where to start (read these files)
- `Assets/StartScreenManager.cs` — main menu: `EventSystem` handling, `PlayerPrefs` key `MasterVolume`, keyboard shortcuts, and scene-load flow.
- `Assets/ScreenFader.cs` — global overlay fader; exposes `Ensure()`, `FadeAndLoad(...)`, and controls input blocking via `Image.raycastTarget`.
- `ProjectSettings/ProjectVersion.txt` — the Unity Editor version to match locally before building.
- `Packages/packages-lock.json` — lists package dependencies (AI packages, FMOD, URP and others).

Project-specific patterns and conventions
- Singletons: prefer the existing `Ensure()` + static `Instance` pattern for global helpers (don't introduce a different lifecycle model).
- UI: use `CanvasGroup` for fade/visibility + `interactable`/`blocksRaycasts` toggles; many UI coroutines use `Time.unscaledDeltaTime` (preserve that for consistent UI behavior when timeScale==0).
- Scene loading: code prefers name-based loads with a build-settings check (see `StartScreenManager.CoLoadScene()`); if a name isn't found it falls back to `SceneManager.LoadScene(1)`.
- Defensive runtime setup: scripts create an `EventSystem` if missing (so tests or trimmed scenes may not have one).
- Preferences: volume and similar values are persisted with `PlayerPrefs` keys such as `MasterVolume`.

Build / run / test workflows (how a human would do it)
- Recommended: open the project with the Unity Editor version from `ProjectSettings/ProjectVersion.txt`.
- Open solution: `notitle.sln` in the repo root for C# editing in Visual Studio / Rider.
- Build from CLI (example — adapt `Unity.exe` path):
  - PowerShell example:
    ```powershell
    $unity = "C:\Program Files\Unity\Hub\Editor\<EDITOR_VERSION>\Editor\Unity.exe"
    & $unity -quit -batchmode -projectPath "C:\path\to\repo\notitle" -buildWindows64Player "C:\path\to\build\notitle.exe"
    ```
- Run tests with Unity Test Runner via CLI (example):
  - EditMode tests:
    ```powershell
    & $unity -batchmode -quit -projectPath "C:\path\to\repo\notitle" -runTests -testPlatform EditMode -testResults "C:\temp\editmode-results.xml"
    ```

External integrations to be aware of
- FMOD (Assets and `FMODUnity*.csproj`) — check `Assets/` for FMOD plugins; builds may require the FMOD runtime.
- JBooth MicroVerse, Flexalon and other third-party packages present under `Assets/` and `Packages/`.
- Several Unity AI packages are present in `Packages/packages-lock.json` — avoid changing package versions without confirming Editor compatibility.

Editing and PR guidance
- Avoid editing generated / binary folders: `Library/`, `Temp/`, `Build/`, `obj/`, `GeneratedAssets/` — these are large and should not be committed.
- Preserve in-source comments (many files have Korean comments). If you modify comments, keep intent intact and ask for clarification.
- When adding global behavior follow existing patterns: `Ensure()` singletons, `DontDestroyOnLoad`, and Canvas overlay patterns.

Search examples (quick navigation for agents)
- Find the fader: `Assets/ScreenFader.cs` (controls input during fades).
- Find menu behavior: `Assets/StartScreenManager.cs` (uses `PlayerPrefs`, `EventSystem`, `ScreenFader.FadeAndLoad`).

When unsure — ask
- If a build requires credentials (3rd-party service keys) or specific editor-only settings, ask the repo owner rather than guessing.

Feedback
- If any part of this doc is unclear or missing (e.g., build server details, required Editor package versions, or CI steps), tell me which area to expand and I'll update the file.

-- end
