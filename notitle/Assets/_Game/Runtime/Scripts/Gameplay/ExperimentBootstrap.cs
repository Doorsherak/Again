using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentBootstrap : MonoBehaviour
{
    enum RunState
    {
        None,
        InMenu,
        Playing,
        Failing,
        Succeeding,
        Transitioning
    }

    public static ExperimentBootstrap Instance { get; private set; }

    [Header("Config")]
    [SerializeField] ExperimentConfig config;
    [SerializeField] bool loadConfigFromResourcesIfMissing = true;
    [SerializeField] string configResourcePath = "Configs/ExperimentConfig";
    [SerializeField] bool useConfigOverrides = true;

    [Header("Run")]
    [SerializeField] int requiredSamples = 3;
    [SerializeField] string startSceneName = "StartScreen";
    [SerializeField] string playerTag = "Player";

    [Header("Observation Rule (Mugunghwa)")]
    [SerializeField] bool enableObservationRule = true;
    [SerializeField] float stillSpeedThreshold = 0.12f;
    [SerializeField] Vector2 freeMoveDuration = new Vector2(2.5f, 5.5f);
    [SerializeField] Vector2 watchDuration = new Vector2(1.2f, 2.8f);

    [Header("Spawns")]
    [SerializeField] bool spawnSamples = true;
    [SerializeField] bool spawnExit = true;
    [SerializeField] bool requireAnalysisStep = true;
    [SerializeField] bool spawnAnalyzer = true;
    [SerializeField] float pickupHeight = 0.9f;
    [SerializeField] float analyzerSecondsPerSample = 1.1f;

    [Header("Audio")]
    [SerializeField] AudioSource uiSfxSource;
    [SerializeField] AudioClip samplePickupClip;
    [SerializeField] Vector2 samplePickupVolumeRange = new Vector2(0.7f, 0.9f);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    [Header("Dev Hotkeys")]
    [SerializeField] bool enableDevHotkeys = true;
    [SerializeField] int devFixedSeed = 42;
#endif

    ExperimentHud _hud;
    Transform _player;
    CharacterController _playerController;
    CorridorBuilder _corridor;
    JumpscareTrigger _jumpscare;
    readonly List<ExperimentSamplePickup> _activeSamples = new List<ExperimentSamplePickup>(16);
    ExperimentExitTrigger _exit;
    ExperimentAnalyzerStation _analyzer;

    int _collected;
    int _rawSamples;
    int _submittedSamples;
    bool _isWatching;
    RunState _state;
    Coroutine _setupCo;
    Coroutine _observationCo;

    Vector3 _lastPlayerPos;
    float _speedTimer;
    float _avgSpeed;
    readonly Queue<float> _speedHistory = new Queue<float>(10);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var existing = Object.FindFirstObjectByType<ExperimentBootstrap>(FindObjectsInactive.Include);
        if (existing)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }
        var go = new GameObject("ExperimentBootstrap_Auto");
        DontDestroyOnLoad(go);
        go.AddComponent<ExperimentBootstrap>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveAndApplyConfig();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void ResolveAndApplyConfig()
    {
        if (!useConfigOverrides) return;

        if (config == null && loadConfigFromResourcesIfMissing && !string.IsNullOrEmpty(configResourcePath))
            config = Resources.Load<ExperimentConfig>(configResourcePath);

        if (config != null)
            ApplyConfig(config);
    }

    void ApplyConfig(ExperimentConfig cfg)
    {
        if (cfg == null) return;

        requiredSamples = cfg.requiredSamples;
        if (!string.IsNullOrEmpty(cfg.startSceneName)) startSceneName = cfg.startSceneName;
        if (!string.IsNullOrEmpty(cfg.playerTag)) playerTag = cfg.playerTag;

        enableObservationRule = cfg.enableObservationRule;
        stillSpeedThreshold = cfg.stillSpeedThreshold;
        freeMoveDuration = cfg.freeMoveDuration;
        watchDuration = cfg.watchDuration;

        spawnSamples = cfg.spawnSamples;
        spawnExit = cfg.spawnExit;
        requireAnalysisStep = cfg.requireAnalysisStep;
        spawnAnalyzer = cfg.spawnAnalyzer;
        pickupHeight = cfg.pickupHeight;
        analyzerSecondsPerSample = cfg.analyzerSecondsPerSample;

        if (cfg.samplePickupClip != null) samplePickupClip = cfg.samplePickupClip;
        samplePickupVolumeRange = cfg.samplePickupVolumeRange;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _state = string.Equals(scene.name, startSceneName, System.StringComparison.OrdinalIgnoreCase)
            ? RunState.InMenu
            : RunState.Playing;
        _collected = 0;
        _rawSamples = 0;
        _submittedSamples = 0;
        _isWatching = false;
        _speedHistory.Clear();
        _speedTimer = 0f;
        _avgSpeed = 0f;
        _activeSamples.Clear();
        _exit = null;
        _analyzer = null;

        if (_setupCo != null) StopCoroutine(_setupCo);
        if (_observationCo != null) StopCoroutine(_observationCo);

        TryBindSceneObjects();
        ResetPlayerStateForNewRun();
        EnsureHud();

        if (_hud) _hud.SetVisible(scene.name != startSceneName);
        if (scene.name == startSceneName) return;

        if (_hud)
        {
            UpdateObjective();
            _hud.ShowMessage(
                requireAnalysisStep
                    ? "\uC2E4\uD5D8 \uC2DC\uC791. \uC0D8\uD50C\uC744 \uC218\uC9D1\uD558\uACE0 \uBD84\uC11D\uB300\uC5D0 \uC81C\uCD9C\uD55C \uB4A4 \uD0C8\uCD9C\uD558\uC138\uC694."
                    : "\uC2E4\uD5D8 \uC2DC\uC791. \uC0D8\uD50C\uC744 \uC218\uC9D1\uD558\uC138\uC694.",
                2.4f);
        }

        if (_corridor)
        {
            _setupCo = StartCoroutine(CoSetupAfterBuild());
        }

        if (enableObservationRule && _player)
        {
            _observationCo = StartCoroutine(CoObservationLoop());
        }
    }

    void TryBindSceneObjects()
    {
        _player = FindPlayer();
        _playerController = _player ? _player.GetComponent<CharacterController>() : null;
        _lastPlayerPos = _player ? _player.position : Vector3.zero;

        _corridor = Object.FindFirstObjectByType<CorridorBuilder>(FindObjectsInactive.Include);
        _jumpscare = Object.FindFirstObjectByType<JumpscareTrigger>(FindObjectsInactive.Include);
    }

    void ResetPlayerStateForNewRun()
    {
        if (_player == null) return;

        var movement = _player.GetComponent("FirstPersonMovement") as Behaviour;
        if (movement != null && !movement.enabled) movement.enabled = true;

        var crouch = FindComponentByTypeName(_player, "Crouch");
        if (crouch != null) crouch.SendMessage("ForceStand", SendMessageOptions.DontRequireReceiver);
    }

    static Component FindComponentByTypeName(Transform root, string typeName)
    {
        if (root == null || string.IsNullOrEmpty(typeName)) return null;
        var comps = root.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            if (c != null && c.GetType().Name == typeName) return c;
        }
        return null;
    }

    Transform FindPlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag(playerTag);
        if (tagged) return tagged.transform;

        var cc = Object.FindFirstObjectByType<CharacterController>(FindObjectsInactive.Include);
        if (cc) return cc.transform;

        return null;
    }

    void EnsureHud()
    {
        if (_hud && _hud.gameObject) return;
        var existing = Object.FindFirstObjectByType<ExperimentHud>(FindObjectsInactive.Include);
        if (existing) { _hud = existing; return; }

        var go = new GameObject("ExperimentHUD");
        _hud = go.AddComponent<ExperimentHud>();
        DontDestroyOnLoad(go);
    }

    IEnumerator CoSetupAfterBuild()
    {
        // CorridorBuilder builds in Awake; wait a frame to ensure children exist
        yield return null;
        yield return null;

        if (spawnSamples) SpawnSamplePickups();
        if (requireAnalysisStep && spawnAnalyzer) SpawnAnalyzerStation();
        if (spawnExit) SpawnExitTrigger();
    }

    void SpawnSamplePickups()
    {
        var modules = GetCorridorModulesByIndex();
        if (modules.Count < 3) return;

        // Avoid first 2 and last 1 to prevent immediate pickup/exit overlap
        int start = Mathf.Min(2, modules.Count - 1);
        int endExclusive = Mathf.Max(start + 1, modules.Count - 1);
        var candidates = modules.GetRange(start, endExclusive - start);
        if (candidates.Count == 0) return;

        int spawnCount = Mathf.Clamp(requiredSamples, 1, 6);
        var chosen = new HashSet<int>();
        for (int i = 0; i < spawnCount; i++)
        {
            int guard = 0;
            int idx;
            do
            {
                idx = Random.Range(0, candidates.Count);
                guard++;
            } while (!chosen.Add(idx) && guard < 50);

            var t = candidates[idx];
            var pos = t.position + Vector3.up * pickupHeight;

            var pickupGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickupGo.name = "SamplePickup";
            pickupGo.transform.position = pos;
            pickupGo.transform.localScale = new Vector3(0.25f, 0.12f, 0.35f);

            var col = pickupGo.GetComponent<Collider>();
            col.isTrigger = true;

            var rb = pickupGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var pickup = pickupGo.AddComponent<ExperimentSamplePickup>();
            pickup.bootstrap = this;
            _activeSamples.Add(pickup);

            // Simple emissive-ish look without custom materials
            var renderer = pickupGo.GetComponent<MeshRenderer>();
            if (renderer && renderer.sharedMaterial)
                renderer.sharedMaterial.color = new Color(0.1f, 0.8f, 0.9f, 1f);
        }
    }

    void SpawnExitTrigger()
    {
        var modules = GetCorridorModulesByIndex();
        if (modules.Count == 0) return;

        var last = modules[modules.Count - 1];
        var exitPos = last.position + last.forward * 1.5f + Vector3.up * 1.0f;

        var go = new GameObject("ExperimentExit");
        go.transform.position = exitPos;
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.4f, 2.4f, 2.4f);

        var exit = go.AddComponent<ExperimentExitTrigger>();
        exit.bootstrap = this;
        _exit = exit;
    }

    void SpawnAnalyzerStation()
    {
        var modules = GetCorridorModulesByIndex();
        if (modules.Count < 3) return;

        int idx = Mathf.Clamp(modules.Count / 2, 1, modules.Count - 2);
        var t = modules[idx];

        var go = new GameObject("ExperimentAnalyzer");
        go.transform.position = t.position + Vector3.up * 1.0f;

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.8f, 2.4f, 2.8f);

        var station = go.AddComponent<ExperimentAnalyzerStation>();
        station.bootstrap = this;
        station.secondsPerSample = analyzerSecondsPerSample;
        _analyzer = station;
    }

    List<Transform> GetCorridorModulesByIndex()
    {
        var list = new List<(int index, Transform tr)>();
        if (!_corridor) return new List<Transform>();

        for (int i = 0; i < _corridor.transform.childCount; i++)
        {
            var child = _corridor.transform.GetChild(i);
            int idx = ParsePrefixIndex(child.name);
            list.Add((idx, child));
        }

        list.Sort((a, b) => a.index.CompareTo(b.index));
        var result = new List<Transform>(list.Count);
        foreach (var it in list) result.Add(it.tr);
        return result;
    }

    static int ParsePrefixIndex(string name)
    {
        // matches "000_" prefix used by CorridorBuilder
        if (string.IsNullOrEmpty(name) || name.Length < 3) return int.MaxValue;
        var slice = name.Substring(0, 3);
        if (int.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx)) return idx;
        return int.MaxValue;
    }

    string ResolveStartSceneIdentifier()
    {
        if (string.IsNullOrEmpty(startSceneName)) return null;

        if (startSceneName.Contains("/") || startSceneName.Contains("\\") || startSceneName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
            return startSceneName;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;

            var name = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, startSceneName, System.StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }

    IEnumerator CoObservationLoop()
    {
        while (_state == RunState.Playing)
        {
            _isWatching = false;
            _hud?.SetStatus(string.Empty);
            yield return new WaitForSecondsRealtime(Random.Range(freeMoveDuration.x, freeMoveDuration.y));
            if (_state != RunState.Playing) break;

            _isWatching = true;
            ResetSpeedSampling();
            _hud?.SetStatus("\uAD00\uCC30 \uC911 - \uC6C0\uC9C1\uC774\uC9C0 \uB9C8");
            yield return new WaitForSecondsRealtime(Random.Range(watchDuration.x, watchDuration.y));
        }

        _isWatching = false;
        _hud?.SetStatus(string.Empty);
    }

    void Update()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        HandleDevHotkeys();
#endif
        if (_state != RunState.Playing || !_player || !_isWatching) return;

        UpdateAverageSpeed();
        if (_avgSpeed > stillSpeedThreshold)
        {
            StartCoroutine(CoFail());
        }
    }

    void UpdateAverageSpeed()
    {
        _speedTimer += Time.unscaledDeltaTime;
        if (_speedTimer < 0.1f) return;

        var pos = _player.position;
        float speed;
        if (_playerController != null)
        {
            var v = _playerController.velocity;
            v.y = 0f;
            speed = v.magnitude;
        }
        else
        {
            float dist = Vector3.Distance(pos, _lastPlayerPos);
            speed = dist / Mathf.Max(0.0001f, _speedTimer);
        }

        _lastPlayerPos = pos;
        _speedTimer = 0f;

        _speedHistory.Enqueue(speed);
        while (_speedHistory.Count > 10) _speedHistory.Dequeue();

        float sum = 0f;
        foreach (var s in _speedHistory) sum += s;
        _avgSpeed = _speedHistory.Count == 0 ? 0f : sum / _speedHistory.Count;
    }

    void ResetSpeedSampling()
    {
        _speedTimer = 0f;
        _avgSpeed = 0f;
        _speedHistory.Clear();
        if (_player) _lastPlayerPos = _player.position;
    }

    public void OnCollectedSample()
        => OnCollectedSample(null);

    public void OnCollectedSample(ExperimentSamplePickup pickup)
    {
        bool wasLocked = !CanExit();
        _collected++;

        if (pickup != null) _activeSamples.Remove(pickup);
        CleanupDestroyedSamples();

        if (requireAnalysisStep)
        {
            _rawSamples++;
            UpdateObjective();
            _hud?.ShowMessage("\uC0D8\uD50C \uD68D\uB4DD. \uBD84\uC11D\uB300\uB85C \uAC00\uC138\uC694.", 1.2f);
        }
        else
        {
            _submittedSamples++;
            UpdateObjective();
            _hud?.ShowMessage("\uC0D8\uD50C \uD68C\uC218 \uC644\uB8CC.", 1.2f);
        }

        if (samplePickupClip != null)
        {
            float min = Mathf.Min(samplePickupVolumeRange.x, samplePickupVolumeRange.y);
            float max = Mathf.Max(samplePickupVolumeRange.x, samplePickupVolumeRange.y);
            float volume = Random.Range(min, max);
            if (uiSfxSource != null) uiSfxSource.PlayOneShot(samplePickupClip, volume);
            else AudioSource.PlayClipAtPoint(samplePickupClip, _player ? _player.position : Vector3.zero, volume);
        }

        if (!requireAnalysisStep && wasLocked && CanExit())
            _hud?.ShowMessage("Exit unlocked.", 1.6f);
    }

    public int CollectedSamples => _collected;
    public int RequiredSamples => requiredSamples;
    public int RemainingSamples => Mathf.Max(0, requiredSamples - _submittedSamples);
    public int RawSamples => _rawSamples;
    public int SubmittedSamples => _submittedSamples;
    public bool RequireAnalysisStep => requireAnalysisStep;
    public bool IsWatching => _isWatching;
    public bool IsEnding => _state == RunState.Failing || _state == RunState.Succeeding || _state == RunState.Transitioning;
    public float AverageSpeed => _avgSpeed;
    public Transform ExitTransform => _exit ? _exit.transform : null;

    public bool CanExit() => _submittedSamples >= requiredSamples;

    public enum HintTargetKind { Sample, Analyzer, Exit }

    public bool TryGetHintTarget(Vector3 fromWorld, out Vector3 targetWorldPos, out HintTargetKind kind)
    {
        if (CanExit() && _exit != null)
        {
            targetWorldPos = _exit.transform.position;
            kind = HintTargetKind.Exit;
            return true;
        }

        if (requireAnalysisStep && _rawSamples > 0 && _analyzer != null)
        {
            targetWorldPos = _analyzer.transform.position;
            kind = HintTargetKind.Analyzer;
            return true;
        }

        CleanupDestroyedSamples();
        ExperimentSamplePickup nearest = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < _activeSamples.Count; i++)
        {
            var s = _activeSamples[i];
            if (s == null) continue;
            float sqr = (s.transform.position - fromWorld).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = s;
            }
        }

        if (nearest != null)
        {
            targetWorldPos = nearest.transform.position;
            kind = HintTargetKind.Sample;
            return true;
        }

        targetWorldPos = Vector3.zero;
        kind = HintTargetKind.Sample;
        return false;
    }

    public bool TryProcessOneSample()
    {
        if (!requireAnalysisStep) return false;
        if (_state != RunState.Playing) return false;
        if (CanExit()) return false;
        if (_rawSamples <= 0) return false;

        bool wasLocked = !CanExit();
        _rawSamples--;
        _submittedSamples++;
        UpdateObjective();
        _hud?.ShowMessage($"\uBD84\uC11D \uC644\uB8CC: {_submittedSamples}/{requiredSamples}", 1.1f);

        if (wasLocked && CanExit())
            _hud?.ShowMessage("Exit unlocked.", 1.6f);

        return true;
    }

    void UpdateObjective()
    {
        if (_hud == null) return;
        if (requireAnalysisStep)
            _hud.SetObjective($"\uC81C\uCD9C: {_submittedSamples} / {requiredSamples} (\uBCF4\uC720: {_rawSamples})");
        else
            _hud.SetObjective($"\uC0D8\uD50C \uAC1C\uC218: {_submittedSamples} / {requiredSamples}");
    }

    void CleanupDestroyedSamples()
    {
        for (int i = _activeSamples.Count - 1; i >= 0; i--)
        {
            if (_activeSamples[i] == null)
                _activeSamples.RemoveAt(i);
        }
    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    void HandleDevHotkeys()
    {
        if (!enableDevHotkeys) return;

        var transitioner = SceneTransitioner.Ensure();
        if (transitioner != null && transitioner.IsTransitioning) return;
        if (_state == RunState.Transitioning) return;

        if (Input.GetKeyDown(KeyCode.F1) && _state == RunState.Playing)
        {
            OnCollectedSample(null);
            _hud?.ShowMessage("DEV: +1 sample", 0.8f);
        }

        if (Input.GetKeyDown(KeyCode.F2) && _state == RunState.Playing)
        {
            ForceUnlockExit();
        }

        if (Input.GetKeyDown(KeyCode.F3) && _state == RunState.Playing)
        {
            StartCoroutine(CoFail());
        }

        if (Input.GetKeyDown(KeyCode.F4) && _state == RunState.Playing)
        {
            StartCoroutine(CoWin());
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            _state = RunState.Transitioning;
            _isWatching = false;
            StartCoroutine(SceneTransitioner.LoadScene(SceneManager.GetActiveScene().buildIndex));
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            _state = RunState.Transitioning;
            _isWatching = false;
            var startId = ResolveStartSceneIdentifier();
            StartCoroutine(!string.IsNullOrEmpty(startId) ? SceneTransitioner.LoadScene(startId) : SceneTransitioner.LoadScene(0));
        }

        if (Input.GetKeyDown(KeyCode.F7) && _state == RunState.Playing)
        {
            enableObservationRule = !enableObservationRule;
            if (!enableObservationRule)
            {
                if (_observationCo != null) StopCoroutine(_observationCo);
                _observationCo = null;
                _isWatching = false;
                _hud?.SetStatus(string.Empty);
                _hud?.ShowMessage("DEV: Observation OFF", 1.0f);
            }
            else
            {
                if (_player != null) _observationCo = StartCoroutine(CoObservationLoop());
                _hud?.ShowMessage("DEV: Observation ON", 1.0f);
            }
        }

        if (Input.GetKeyDown(KeyCode.F8) && _state == RunState.Playing)
        {
            CorridorBuilder.UseSeedOverride = !CorridorBuilder.UseSeedOverride;
            CorridorBuilder.SeedOverride = devFixedSeed;
            _hud?.ShowMessage(
                $"DEV: SeedOverride {(CorridorBuilder.UseSeedOverride ? "ON" : "OFF")} ({CorridorBuilder.SeedOverride})",
                1.6f);

            _state = RunState.Transitioning;
            _isWatching = false;
            StartCoroutine(SceneTransitioner.LoadScene(SceneManager.GetActiveScene().buildIndex));
        }
    }

    void ForceUnlockExit()
    {
        _collected = Mathf.Max(_collected, requiredSamples);
        _rawSamples = 0;
        _submittedSamples = requiredSamples;
        for (int i = 0; i < _activeSamples.Count; i++)
        {
            var s = _activeSamples[i];
            if (s != null) Destroy(s.gameObject);
        }
        _activeSamples.Clear();

        UpdateObjective();
        _hud?.ShowMessage("DEV: Exit unlocked.", 1.2f);
    }
#endif

    public void OnReachedExit()
    {
        if (_state != RunState.Playing) return;
        if (!CanExit())
        {
            _hud?.ShowMessage($"\uC0D8\uD50C\uC774 \uBD80\uC871\uD569\uB2C8\uB2E4. ({_collected}/{requiredSamples})", 1.8f);
            return;
        }

        StartCoroutine(CoWin());
    }

    IEnumerator CoWin()
    {
        if (_state != RunState.Playing) yield break;
        _state = RunState.Succeeding;
        _isWatching = false;
        _hud?.ShowMessage("\uC2E4\uD5D8 \uC885\uB8CC. \uD68C\uC218 \uC131\uACF5.", 2.0f);
        yield return new WaitForSecondsRealtime(2.0f);
        _state = RunState.Transitioning;
        var startId = ResolveStartSceneIdentifier();
        yield return !string.IsNullOrEmpty(startId) ? SceneTransitioner.LoadScene(startId) : SceneTransitioner.LoadScene(0);
    }

    IEnumerator CoFail()
    {
        if (_state != RunState.Playing) yield break;
        _state = RunState.Failing;
        _isWatching = false;

        Time.timeScale = 1f;
        _hud?.ShowMessage("\uC2E4\uD5D8 \uC2E4\uD328. \uC6C0\uC9C1\uC784 \uAC10\uC9C0.", 1.6f);
        if (_jumpscare != null)
        {
            _jumpscare.autoRestart = true;
            if (_jumpscare.TryTrigger())
            {
                _state = RunState.Transitioning;
                yield break;
            }
        }

        yield return new WaitForSecondsRealtime(1.2f);
        _state = RunState.Transitioning;
        yield return SceneTransitioner.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
