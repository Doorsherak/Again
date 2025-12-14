using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentBootstrap : MonoBehaviour
{
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
    [SerializeField] float pickupHeight = 0.9f;

    ExperimentHud _hud;
    Transform _player;
    CharacterController _playerController;
    CorridorBuilder _corridor;
    JumpscareTrigger _jumpscare;

    int _collected;
    bool _isWatching;
    bool _ending;
    Coroutine _setupCo;
    Coroutine _observationCo;

    Vector3 _lastPlayerPos;
    float _speedTimer;
    float _avgSpeed;
    readonly Queue<float> _speedHistory = new Queue<float>(10);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        var existing = Object.FindFirstObjectByType<ExperimentBootstrap>(FindObjectsInactive.Include);
        if (existing) return;
        var go = new GameObject("ExperimentBootstrap_Auto");
        DontDestroyOnLoad(go);
        go.AddComponent<ExperimentBootstrap>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _ending = false;
        _collected = 0;
        _speedHistory.Clear();
        _speedTimer = 0f;
        _avgSpeed = 0f;

        if (_setupCo != null) StopCoroutine(_setupCo);
        if (_observationCo != null) StopCoroutine(_observationCo);

        TryBindSceneObjects();
        EnsureHud();

        if (_hud) _hud.SetVisible(scene.name != startSceneName);
        if (scene.name == startSceneName) return;

        if (_hud)
        {
            _hud.SetObjective($"샘플 회수: 0 / {requiredSamples}");
            _hud.ShowMessage("투약 완료. 환각 반응 테스트를 시작합니다.", 2.2f);
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

    IEnumerator CoObservationLoop()
    {
        while (!_ending)
        {
            _isWatching = false;
            _hud?.SetStatus("이동 가능");
            yield return new WaitForSecondsRealtime(Random.Range(freeMoveDuration.x, freeMoveDuration.y));

            _isWatching = true;
            ResetSpeedSampling();
            _hud?.SetStatus("관찰 중 - 움직이지 마");
            yield return new WaitForSecondsRealtime(Random.Range(watchDuration.x, watchDuration.y));
        }
    }

    void Update()
    {
        if (_ending || !_player || !_isWatching) return;

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
    {
        _collected++;
        _hud?.SetObjective($"샘플 회수: {_collected} / {requiredSamples}");
        _hud?.ShowMessage("샘플 회수 완료.", 1.2f);
    }

    public bool CanExit() => _collected >= requiredSamples;

    public void OnReachedExit()
    {
        if (_ending) return;
        if (!CanExit())
        {
            _hud?.ShowMessage($"샘플이 부족합니다. ({_collected}/{requiredSamples})", 1.8f);
            return;
        }

        StartCoroutine(CoWin());
    }

    IEnumerator CoWin()
    {
        _ending = true;
        _hud?.ShowMessage("실험 종료. 회수 성공.", 2.0f);
        yield return new WaitForSecondsRealtime(1.5f);
        SceneManager.LoadScene(startSceneName);
    }

    IEnumerator CoFail()
    {
        if (_ending) yield break;
        _ending = true;

        Time.timeScale = 1f;
        _hud?.ShowMessage("실험 실패. 움직임 감지.", 1.6f);
        if (_jumpscare != null)
        {
            _jumpscare.autoRestart = true;
            if (_jumpscare.TryTrigger()) yield break;
        }

        yield return new WaitForSecondsRealtime(1.2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
