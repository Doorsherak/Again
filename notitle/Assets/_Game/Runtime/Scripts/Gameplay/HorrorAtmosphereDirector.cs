using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HorrorAtmosphereDirector : MonoBehaviour
{
    [System.Serializable]
    struct QualityPreset
    {
        [Tooltip("QualitySettings.names 와 일치하는 이름(예: PC, iGPU, Mobile).")]
        public string qualityName;

        [Header("Vignette")]
        public bool enableVignette;
        [Range(0f, 1f)] public float vignetteMinAlpha;
        [Range(0f, 1f)] public float vignetteMaxAlpha;
        [Range(64, 512)] public int vignetteTextureSize;
        public float vignettePulseSpeed;
        [Range(0f, 0.2f)] public float vignettePulseAmount;

        [Header("Stingers")]
        public bool enableStingers;
        public bool spatializeStingers;
        public Vector2 stingerInterval;
        public Vector2 stingerVolumeRange;

        [Header("Light Flicker")]
        public bool enableLampFlickerOnWatchStart;
        [Range(0f, 1f)] public float watchStartFlickerChance;
        public int flickerBurstCount;
        public float flickerBurstSpacing;

        [Header("Audio Low-Pass")]
        public bool enableLowPass;
        public float responseSpeed;
    }

    [Header("Quality Tuning")]
    [SerializeField] bool enableQualityTuning = true;
    [SerializeField] bool autoTuneOnQualityChange = true;
    [SerializeField] QualityPreset pcPreset = MakePcPreset();
    [SerializeField] QualityPreset iGpuPreset = MakeIGpuPreset();
    [SerializeField] QualityPreset mobilePreset = MakeMobilePreset();

    [Header("Enable")]
    [SerializeField] bool enableDirector = true;
    [SerializeField] bool disableInMenuScenes = true;
    [SerializeField] string[] menuSceneNames = { "StartScreen", "Options", "Credits" };

    [Header("Tension")]
    [SerializeField, Range(0f, 1f)] float baseTension = 0.25f;
    [SerializeField, Range(0f, 1f)] float watchingTension = 0.85f;
    [SerializeField, Range(0f, 1f)] float progressWeight = 0.45f;
    [SerializeField] float responseSpeed = 4f;

    [Header("Safety")]
    public bool photosensitiveSafeMode = true;
    [Range(0f, 1f)] public float safeMaxVignettePulseAmount = 0.03f;
    public float safeMaxVignettePulseSpeed = 0.8f;

    [Header("Vignette")]
    [SerializeField] bool enableVignette = true;
    [SerializeField, Range(0f, 1f)] float vignetteMinAlpha = 0.06f;
    [SerializeField, Range(0f, 1f)] float vignetteMaxAlpha = 0.28f;
    [SerializeField] float vignettePulseSpeed = 0.6f;
    [SerializeField, Range(0f, 0.2f)] float vignettePulseAmount = 0.02f;
    [SerializeField, Range(64, 512)] int vignetteTextureSize = 256;
    [SerializeField, Range(0f, 1f)] float vignetteInnerRadius = 0.35f;
    [SerializeField, Range(0f, 1f)] float vignetteOuterRadius = 0.95f;
    [SerializeField] int overlaySortingOrder = 200;

    [Header("Audio Low-Pass (Optional)")]
    [SerializeField] bool enableLowPass = false;
    [SerializeField] float lowPassMinCutoff = 6500f;
    [SerializeField] float lowPassMaxCutoff = 22000f;
    [SerializeField, Range(0.1f, 10f)] float lowPassResonanceQ = 1f;

    [Header("Light Flicker (Optional)")]
    [SerializeField] bool enableLampFlickerOnWatchStart = true;
    [SerializeField, Range(0f, 1f)] float watchStartFlickerChance = 0.65f;
    [SerializeField] int flickerBurstCount = 2;
    [SerializeField] float flickerBurstSpacing = 0.12f;

    [Header("Stingers (Optional)")]
    [SerializeField] bool enableStingers = true;
    [SerializeField] AudioSource stingerSource;
    [SerializeField] AudioClip[] stingerClips;
    [SerializeField] Vector2 stingerInterval = new Vector2(12f, 24f);
    [SerializeField] Vector2 stingerVolumeRange = new Vector2(0.12f, 0.28f);
    [SerializeField] Vector2 stingerPitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] bool spatializeStingers = true;
    [SerializeField, Range(0f, 1f)] float behindChance = 0.6f;
    [SerializeField] float stingerDistance = 3.5f;

    ExperimentBootstrap _bootstrap;
    Camera _camera;
    AudioLowPassFilter _lowPass;
    Component[] _flickerLamps;

    RawImage _vignetteImage;
    Texture2D _vignetteTexture;

    float _tension;
    bool _wasWatching;
    Coroutine _flickerCo;
    float _stingerTimer;
    int _lastStingerIndex = -1;
    int _lastQualityLevel = -999;
    int _vignetteBuiltSize = -1;
    float _vignetteBuiltInner = -1f;
    float _vignetteBuiltOuter = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        var existing = Object.FindFirstObjectByType<HorrorAtmosphereDirector>(FindObjectsInactive.Include);
        if (existing) return;
        var go = new GameObject("HorrorAtmosphereDirector_Auto");
        DontDestroyOnLoad(go);
        go.AddComponent<HorrorAtmosphereDirector>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyQualityTuning(force: true, rebindAfterApply: false);
        BindSceneReferences();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        ApplyQualityTuning(force: true, rebindAfterApply: false);
        BindSceneReferences();
    }

    void BindSceneReferences()
    {
        _bootstrap = Object.FindFirstObjectByType<ExperimentBootstrap>(FindObjectsInactive.Include);
        _camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

        if (enableLowPass)
        {
            var listener = Object.FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);
            if (listener != null)
            {
                _lowPass = listener.GetComponent<AudioLowPassFilter>();
                if (_lowPass == null) _lowPass = listener.gameObject.AddComponent<AudioLowPassFilter>();
                _lowPass.lowpassResonanceQ = lowPassResonanceQ;
            }
        }

#if UNITY_2023_1_OR_NEWER
        _flickerLamps = FindFlickerLamps();
#else
        _flickerLamps = FindFlickerLamps();
#endif

        EnsureVignetteOverlay();
        EnsureStingerSource();
        UpdateStingerSourceRuntimeSettings();
        RebuildVignetteIfNeeded();
    }

    static QualityPreset MakePcPreset()
    {
        return new QualityPreset
        {
            qualityName = "PC",
            enableVignette = true,
            vignetteMinAlpha = 0.06f,
            vignetteMaxAlpha = 0.28f,
            vignetteTextureSize = 256,
            vignettePulseSpeed = 0.6f,
            vignettePulseAmount = 0.02f,

            enableStingers = true,
            spatializeStingers = true,
            stingerInterval = new Vector2(12f, 24f),
            stingerVolumeRange = new Vector2(0.12f, 0.28f),

            enableLampFlickerOnWatchStart = true,
            watchStartFlickerChance = 0.65f,
            flickerBurstCount = 2,
            flickerBurstSpacing = 0.12f,

            enableLowPass = false,
            responseSpeed = 4f,
        };
    }

    static QualityPreset MakeIGpuPreset()
    {
        var p = MakePcPreset();
        p.qualityName = "iGPU";
        p.vignetteTextureSize = 192;
        p.vignetteMaxAlpha = 0.24f;
        p.vignettePulseAmount = 0.015f;
        p.stingerInterval = new Vector2(14f, 26f);
        p.watchStartFlickerChance = 0.5f;
        p.responseSpeed = 3.5f;
        return p;
    }

    static QualityPreset MakeMobilePreset()
    {
        var p = MakePcPreset();
        p.qualityName = "Mobile";
        p.vignetteTextureSize = 128;
        p.vignetteMaxAlpha = 0.22f;
        p.vignettePulseAmount = 0.01f;
        p.spatializeStingers = false;
        p.stingerInterval = new Vector2(18f, 30f);
        p.enableLampFlickerOnWatchStart = false;
        p.enableLowPass = false;
        p.responseSpeed = 3.0f;
        return p;
    }

    void ApplyQualityTuning(bool force, bool rebindAfterApply)
    {
        if (!enableQualityTuning) return;
        int level = QualitySettings.GetQualityLevel();
        if (!force && level == _lastQualityLevel) return;
        _lastQualityLevel = level;

        var preset = ResolvePreset(GetQualityNameSafe(level));

        enableVignette = preset.enableVignette;
        vignetteMinAlpha = preset.vignetteMinAlpha;
        vignetteMaxAlpha = preset.vignetteMaxAlpha;
        vignetteTextureSize = preset.vignetteTextureSize;
        vignettePulseSpeed = preset.vignettePulseSpeed;
        vignettePulseAmount = preset.vignettePulseAmount;

        enableStingers = preset.enableStingers;
        spatializeStingers = preset.spatializeStingers;
        stingerInterval = preset.stingerInterval;
        stingerVolumeRange = preset.stingerVolumeRange;

        enableLampFlickerOnWatchStart = preset.enableLampFlickerOnWatchStart;
        watchStartFlickerChance = preset.watchStartFlickerChance;
        flickerBurstCount = preset.flickerBurstCount;
        flickerBurstSpacing = preset.flickerBurstSpacing;

        enableLowPass = preset.enableLowPass;
        responseSpeed = preset.responseSpeed;

        if (rebindAfterApply)
            BindSceneReferences();
    }

    static string GetQualityNameSafe(int level)
    {
        var names = QualitySettings.names;
        if (names != null && level >= 0 && level < names.Length) return names[level];
        return string.Empty;
    }

    QualityPreset ResolvePreset(string qualityName)
    {
        if (!string.IsNullOrEmpty(qualityName))
        {
            if (NameEquals(qualityName, pcPreset.qualityName) || NameEquals(qualityName, "PC")) return pcPreset;
            if (NameEquals(qualityName, iGpuPreset.qualityName) || NameEquals(qualityName, "iGPU")) return iGpuPreset;
            if (NameEquals(qualityName, mobilePreset.qualityName) || NameEquals(qualityName, "Mobile")) return mobilePreset;
        }
        return pcPreset;
    }

    static bool NameEquals(string a, string b)
        => !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
           string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);

    void Update()
    {
        if (!Application.isPlaying) return;
        if (enableQualityTuning && autoTuneOnQualityChange)
            ApplyQualityTuning(force: false, rebindAfterApply: true);
        if (!enableDirector) { SetEnabled(false); return; }

        bool inMenu = disableInMenuScenes && IsMenuScene(SceneManager.GetActiveScene().name);
        if (inMenu) { SetEnabled(false); return; }

        SetEnabled(true);

        bool watching = _bootstrap != null && !_bootstrap.IsEnding && _bootstrap.IsWatching;
        if (watching && !_wasWatching) OnWatchStart();
        _wasWatching = watching;

        float targetTension = ComputeTargetTension(watching);
        float dt = Time.unscaledDeltaTime;
        float k = 1f - Mathf.Exp(-Mathf.Max(0.01f, responseSpeed) * dt);
        _tension = Mathf.Lerp(_tension, targetTension, k);

        ApplyVignette(_tension);
        ApplyLowPass(_tension);
        UpdateStingers(_tension);
    }

    float ComputeTargetTension(bool watching)
    {
        float t = baseTension;
        if (_bootstrap != null && !_bootstrap.IsEnding)
        {
            float progress = 0f;
            int req = Mathf.Max(0, _bootstrap.RequiredSamples);
            if (req > 0) progress = Mathf.Clamp01((float)_bootstrap.CollectedSamples / req);
            t += progress * progressWeight;
        }

        if (watching) t = Mathf.Max(t, watchingTension);
        return Mathf.Clamp01(t);
    }

    void OnWatchStart()
    {
        if (!enableLampFlickerOnWatchStart) return;
        if (_flickerLamps == null || _flickerLamps.Length == 0) return;
        if (Random.value > watchStartFlickerChance) return;

        if (_flickerCo != null) StopCoroutine(_flickerCo);
        _flickerCo = StartCoroutine(FlickerBurst());
    }

    IEnumerator FlickerBurst()
    {
        int count = Mathf.Max(1, flickerBurstCount);
        float spacing = Mathf.Max(0.01f, flickerBurstSpacing);

        for (int i = 0; i < count; i++)
        {
            var lamp = PickRandomFlickerLamp();
            if (lamp != null)
                lamp.SendMessage("TriggerFlicker", SendMessageOptions.DontRequireReceiver);
            yield return new WaitForSecondsRealtime(spacing);
        }

        _flickerCo = null;
    }

    Component PickRandomFlickerLamp()
    {
        if (_flickerLamps == null || _flickerLamps.Length == 0) return null;
        for (int guard = 0; guard < 12; guard++)
        {
            var lamp = _flickerLamps[Random.Range(0, _flickerLamps.Length)];
            if (lamp == null) continue;
            if (lamp is Behaviour b && !b.isActiveAndEnabled) continue;
            return lamp;
        }
        return null;
    }

    static Component[] FindFlickerLamps()
    {
        // Avoid a compile-time dependency on FlickeringLamp (it may live in a different assembly).
#if UNITY_2023_1_OR_NEWER
        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var lights = GameObject.FindObjectsOfType<Light>(true);
#endif
        if (lights == null || lights.Length == 0) return System.Array.Empty<Component>();

        var list = new System.Collections.Generic.List<Component>(8);
        for (int i = 0; i < lights.Length; i++)
        {
            var l = lights[i];
            if (l == null) continue;
            var comp = l.GetComponent("FlickeringLamp");
            if (comp != null) list.Add(comp);
        }
        return list.ToArray();
    }

    void EnsureVignetteOverlay()
    {
        if (!enableVignette) return;
        if (_vignetteImage != null) return;

        var canvasGo = new GameObject("Canvas_HorrorOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = overlaySortingOrder;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var vignetteGo = new GameObject("Vignette", typeof(RectTransform), typeof(RawImage));
        vignetteGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)vignetteGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _vignetteImage = vignetteGo.GetComponent<RawImage>();
        _vignetteImage.raycastTarget = false;

        if (_vignetteTexture != null) Destroy(_vignetteTexture);
        _vignetteTexture = BuildVignetteTexture(vignetteTextureSize, vignetteInnerRadius, vignetteOuterRadius);
        _vignetteBuiltSize = vignetteTextureSize;
        _vignetteBuiltInner = vignetteInnerRadius;
        _vignetteBuiltOuter = vignetteOuterRadius;
        _vignetteImage.texture = _vignetteTexture;
        _vignetteImage.color = new Color(0f, 0f, 0f, 0f);
    }

    void RebuildVignetteIfNeeded()
    {
        if (!enableVignette) return;
        if (_vignetteImage == null) return;

        bool sizeChanged = _vignetteBuiltSize != vignetteTextureSize;
        bool shapeChanged = !Mathf.Approximately(_vignetteBuiltInner, vignetteInnerRadius) ||
                            !Mathf.Approximately(_vignetteBuiltOuter, vignetteOuterRadius);
        if (!sizeChanged && !shapeChanged) return;

        if (_vignetteTexture != null) Destroy(_vignetteTexture);
        _vignetteTexture = BuildVignetteTexture(vignetteTextureSize, vignetteInnerRadius, vignetteOuterRadius);
        _vignetteBuiltSize = vignetteTextureSize;
        _vignetteBuiltInner = vignetteInnerRadius;
        _vignetteBuiltOuter = vignetteOuterRadius;
        _vignetteImage.texture = _vignetteTexture;
    }

    void ApplyVignette(float tension)
    {
        if (!enableVignette || _vignetteImage == null) return;

        float minA = Mathf.Clamp01(vignetteMinAlpha);
        float maxA = Mathf.Clamp01(vignetteMaxAlpha);
        float baseA = Mathf.Lerp(minA, maxA, tension);

        float pulseSpeed = Mathf.Max(0f, vignettePulseSpeed);
        float pulseAmt = Mathf.Max(0f, vignettePulseAmount);
        if (photosensitiveSafeMode)
        {
            pulseSpeed = Mathf.Min(pulseSpeed, Mathf.Max(0f, safeMaxVignettePulseSpeed));
            pulseAmt = Mathf.Min(pulseAmt, Mathf.Clamp01(safeMaxVignettePulseAmount));
        }

        float pulse = 0f;
        if (pulseSpeed > 0f && pulseAmt > 0f)
        {
            float s = Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
            pulse = s * pulseAmt * tension;
        }

        float a = Mathf.Clamp01(baseA + pulse);
        _vignetteImage.color = new Color(0f, 0f, 0f, a);
    }

    void ApplyLowPass(float tension)
    {
        if (_lowPass == null) return;
        if (!enableLowPass) { _lowPass.enabled = false; return; }

        _lowPass.enabled = true;
        _lowPass.lowpassResonanceQ = lowPassResonanceQ;

        float min = Mathf.Clamp(lowPassMinCutoff, 10f, 22000f);
        float max = Mathf.Clamp(lowPassMaxCutoff, 10f, 22000f);
        if (min > max) (min, max) = (max, min);
        _lowPass.cutoffFrequency = Mathf.Lerp(max, min, tension);
    }

    void SetEnabled(bool on)
    {
        if (_vignetteImage != null)
        {
            if (!on) _vignetteImage.color = new Color(0f, 0f, 0f, 0f);
            _vignetteImage.enabled = enableVignette && on;
        }

        if (_lowPass != null)
        {
            if (!on) _lowPass.enabled = false;
        }

        if (!on && stingerSource != null)
            stingerSource.Stop();
    }

    bool IsMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (menuSceneNames == null) return false;
        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            var n = menuSceneNames[i];
            if (string.IsNullOrEmpty(n)) continue;
            if (string.Equals(sceneName, n, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static Texture2D BuildVignetteTexture(int size, float innerRadius, float outerRadius)
    {
        int s = Mathf.Clamp(size, 64, 1024);
        float inner = Mathf.Clamp01(innerRadius);
        float outer = Mathf.Clamp01(outerRadius);
        if (outer <= inner) outer = Mathf.Min(1f, inner + 0.01f);

        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[s * s];
        int i = 0;
        for (int y = 0; y < s; y++)
        {
            float ny = (y / (s - 1f)) * 2f - 1f;
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (s - 1f)) * 2f - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.InverseLerp(inner, outer, r);
                a = Mathf.Clamp01(a);
                a = Mathf.SmoothStep(0f, 1f, a);
                byte alpha = (byte)Mathf.RoundToInt(a * 255f);
                pixels[i++] = new Color32(255, 255, 255, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    void EnsureStingerSource()
    {
        if (!enableStingers) return;
        if (stingerSource != null) return;

        var go = new GameObject("Horror_StingerSource");
        DontDestroyOnLoad(go);
        stingerSource = go.AddComponent<AudioSource>();
        stingerSource.playOnAwake = false;
        stingerSource.loop = false;
        stingerSource.spatialBlend = spatializeStingers ? 1f : 0f;
        stingerSource.rolloffMode = AudioRolloffMode.Linear;
        stingerSource.minDistance = 1f;
        stingerSource.maxDistance = Mathf.Max(4f, stingerDistance * 2f);

        _stingerTimer = NextStingerDelay(_tension);
    }

    void UpdateStingerSourceRuntimeSettings()
    {
        if (stingerSource == null) return;
        stingerSource.spatialBlend = spatializeStingers ? 1f : 0f;
        stingerSource.maxDistance = Mathf.Max(4f, stingerDistance * 2f);
        if (!enableStingers) stingerSource.Stop();
    }

    void UpdateStingers(float tension)
    {
        if (!enableStingers) return;
        if (stingerClips == null || stingerClips.Length == 0) return;
        if (stingerSource == null) return;
        if (_camera == null) return;

        _stingerTimer -= Time.unscaledDeltaTime;
        if (_stingerTimer > 0f) return;

        PlayStinger();
        _stingerTimer = NextStingerDelay(tension);
    }

    float NextStingerDelay(float tension)
    {
        float min = Mathf.Max(0.5f, stingerInterval.x);
        float max = Mathf.Max(min, stingerInterval.y);
        float baseDelay = Random.Range(min, max);
        float tensionFactor = Mathf.Lerp(1.15f, 0.75f, Mathf.Clamp01(tension));
        return baseDelay * tensionFactor;
    }

    void PlayStinger()
    {
        int idx = PickStingerIndex();
        if (idx < 0) return;

        var clip = stingerClips[idx];
        if (clip == null) return;

        if (spatializeStingers)
        {
            Vector3 dir = Random.value < behindChance
                ? -_camera.transform.forward
                : (_camera.transform.right * (Random.value < 0.5f ? -1f : 1f));
            dir.y += Random.Range(-0.25f, 0.25f);
            dir.Normalize();
            stingerSource.transform.position = _camera.transform.position + dir * Mathf.Max(0.5f, stingerDistance);
        }
        else
        {
            stingerSource.transform.position = _camera.transform.position;
        }

        float volMin = Mathf.Min(stingerVolumeRange.x, stingerVolumeRange.y);
        float volMax = Mathf.Max(stingerVolumeRange.x, stingerVolumeRange.y);
        float volume = Mathf.Clamp01(Random.Range(volMin, volMax));

        float pitchMin = Mathf.Min(stingerPitchRange.x, stingerPitchRange.y);
        float pitchMax = Mathf.Max(stingerPitchRange.x, stingerPitchRange.y);
        stingerSource.pitch = Mathf.Max(0.01f, Random.Range(pitchMin, pitchMax));
        stingerSource.PlayOneShot(clip, volume);
    }

    int PickStingerIndex()
    {
        if (stingerClips == null || stingerClips.Length == 0) return -1;
        int start = Random.Range(0, stingerClips.Length);
        for (int i = 0; i < stingerClips.Length; i++)
        {
            int idx = (start + i) % stingerClips.Length;
            if (idx == _lastStingerIndex && stingerClips.Length > 1) continue;
            if (stingerClips[idx] == null) continue;
            _lastStingerIndex = idx;
            return idx;
        }
        return -1;
    }
}
