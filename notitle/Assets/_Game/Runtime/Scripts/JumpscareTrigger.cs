using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class JumpscareTrigger : MonoBehaviour
{
    [Header("오브젝트 참조")]
    public GameObject monster;                  // 점프스케어에 쓸 몬스터(또는 상자, 실루엣)
    public CanvasGroup fadeCanvasGroup;         // 화면을 까맣게 덮을 CanvasGroup
    public MonoBehaviour playerController;      // 플레이어 이동 스크립트
    public AudioSource sfxSource;               // 점프스케어 SFX 재생용 AudioSource(카메라 쪽 추천)
    public AudioClip jumpscareClip;             // 점프스케어 효과음
    public Vector2 sfxVolumeRange = new Vector2(0.9f, 1f);
    public Vector2 sfxPitchRange = new Vector2(0.95f, 1.05f);
    public bool restoreSfxPitchAfterPlay = true;

    [Header("빌드업 옵션")]
    public bool usePreScare = true;             // 빌드업(라이트/앰비언스) 사용할지
    public float preScareDuration = 2.0f;       // 빌드업 전체 길이
    public Light[] flickerLights;               // 깜빡이게 만들 라이트들
    public bool useAmbienceRamp = true;         // 앰비언스 볼륨 올릴지
    public AudioSource ambienceSource;          // 배경 앰비언스 AudioSource
    public float ambienceTargetVolume = 0.8f;   // 빌드업 끝에서의 목표 볼륨
    public float flickerIntervalMin = 0.05f;
    public float flickerIntervalMax = 0.2f;
    public bool usePreScareFade = true;
    [Range(0f, 1f)] public float preScareFadeAlpha = 0.2f;
    public bool useIntensityFlicker = false;
    [Range(0f, 2f)] public float flickerIntensityMin = 0.6f;
    [Range(0f, 2f)] public float flickerIntensityMax = 1.0f;

    [Header("메인 점프스케어 타이밍")]
    public float delayBeforeMonster = 0.0f;     // 빌드업 이후 몬스터 등장까지 지연
    public float soundToMonsterOffset = -0.05f; // 음원 vs 몬스터 타이밍 오프셋(+, - 가능)
    public float timingJitter = 0.15f;          // 매번 랜덤으로 흔들릴 시간(단조로움 방지)
    public float holdOnMonster = 0.8f;          // 몬스터가 보이는 채로 멈춰 있는 시간

    [Header("페이드 / 재시작")]
    public float fadeDuration = 1.0f;           // 화면이 완전히 까매질 때까지
    public float delayBeforeRestart = 0.5f;     // 완전 암전 이후 대기
    public bool autoRestart = true;             // 자동으로 씬 다시 로드할지
    public bool showCursorOnEnd = false;        // 페이드 후 커서 보이게(메뉴용)

    [Header("카메라 쉐이크 옵션")]
    public bool useCameraShake = true;
    public Transform cameraTransform;           // 흔들 카메라 Transform
    public float shakeDuration = 0.3f;
    public float shakeIntensity = 0.1f;
    public bool useRotationShake = true;
    public float shakeRotationIntensity = 1.5f;

    [Header("몬스터 배치(선택)")]
    public bool placeMonsterAtCamera = true;    // 실패/외부 트리거에서도 확실히 보이도록 카메라 앞에 배치
    public Vector3 monsterCameraLocalPos = new Vector3(0f, -0.15f, 0.75f);
    public Vector3 monsterCameraLocalEuler = new Vector3(0f, 180f, 0f);
    public Vector3 monsterPositionJitter = new Vector3(0.03f, 0.02f, 0.03f);
    public Vector3 monsterRotationJitter = new Vector3(0f, 4f, 0f);

    [Header("Safety")]
    public bool photosensitiveSafeMode = true;
    [Range(0f, 1f)] public float safeMaxFlashAlpha = 0.6f;
    public int safeMaxFlashCount = 1;
    public float safeMinFlashDuration = 0.08f;
    [Range(0f, 1f)] public float safeMaxBlackoutAlpha = 0.8f;
    public float safeMinBlackoutDuration = 0.05f;
    public float safeMinLightPulseTime = 0.08f;
    public float safeMaxMonsterLightIntensity = 6f;

    [Header("Impact")]
    public bool useImpactFlash = true;
    [Range(0f, 1f)] public float impactFlashAlpha = 0.85f;
    public int impactFlashCount = 1;
    public float impactFlashDuration = 0.05f;
    public float impactFlashGap = 0.05f;
    public bool useFovKick = true;
    public Camera targetCamera;
    public float fovKick = -12f;
    public float fovKickInTime = 0.04f;
    public float fovKickOutTime = 0.12f;
    public bool usePreImpactBlackout = true;
    [Range(0f, 1f)] public float preImpactBlackoutAlpha = 1f;
    public float preImpactBlackoutDuration = 0.03f;
    public float preImpactBlackoutLeadTime = 0.02f;

    [Header("Audio Ducking")]
    public bool useAudioDucking = true;
    public AudioSource[] duckSources;
    [Range(0f, 1f)] public float duckTargetVolume = 0.1f;
    public float duckInTime = 0.05f;
    public float duckHoldTime = 0.12f;
    public float duckOutTime = 0.25f;
    public float duckLeadTime = 0.02f;

    [Header("Monster Light")]
    public bool useMonsterLightPulse = true;
    public Light monsterLight;
    public bool autoFindMonsterLight = true;
    public float monsterLightTargetIntensity = 8f;
    public float monsterLightInTime = 0.05f;
    public float monsterLightHoldTime = 0.1f;
    public float monsterLightOutTime = 0.2f;
    public bool overrideMonsterLightColor = false;
    public Color monsterLightColor = new Color(0.75f, 0.85f, 1f, 1f);

    [Header("Monster Lunge")]
    public bool useMonsterLunge = true;
    public float lungeDistance = 0.25f;
    public float lungeDuration = 0.08f;

    [Header("Auto Trigger")]
    public bool autoTriggerOnStart = false;
    public float autoTriggerDelay = 0f;
    public bool disableColliderWhenAutoTrigger = true;

    [Header("Trigger Control")]
    public bool enableTriggerOnEnter = true;

    [Header("Editor Preview")]
    public bool showPlacementPreview = true;
    public Color previewColor = new Color(1f, 0f, 0f, 0.3f);
    public float previewForwardLength = 0.35f;
    public bool showJitterPreview = true;
    public Color previewJitterColor = new Color(1f, 0.6f, 0f, 0.2f);

    [Header("디버그 / 개발용")]
    public bool triggerOnlyOnce = true;         // true면 한 번만 발동
    public bool editorOnly = false;             // 에디터에서만 발동(빌드에선 무시)

    bool _triggered = false;

    float _originalAmbienceVolume;
    bool[] _originalLightEnabled;
    float[] _originalLightIntensity;
    Collider _collider;
    Coroutine _fovKickCo;
    Coroutine _impactFlashCo;
    Coroutine _lungeCo;
    Coroutine _restorePitchCo;
    Coroutine _duckCo;
    Coroutine _monsterLightCo;
    readonly List<AudioSource> _duckTargets = new List<AudioSource>(8);
    readonly List<float> _duckOriginalVolumes = new List<float>(8);
    readonly List<float> _duckStartVolumes = new List<float>(8);
    readonly List<float> _duckTargetVolumes = new List<float>(8);

    void Awake()
    {
        _collider = GetComponent<Collider>();

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;

        if (monster != null)
            monster.SetActive(false);

        if (ambienceSource != null)
            _originalAmbienceVolume = ambienceSource.volume;

        if (flickerLights != null && flickerLights.Length > 0)
        {
            _originalLightEnabled = new bool[flickerLights.Length];
            _originalLightIntensity = new float[flickerLights.Length];
            for (int i = 0; i < flickerLights.Length; i++)
            {
                if (flickerLights[i] != null)
                {
                    _originalLightEnabled[i] = flickerLights[i].enabled;
                    _originalLightIntensity[i] = flickerLights[i].intensity;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!enableTriggerOnEnter) return;
        if (!other.CompareTag("Player")) return;
        Trigger();
    }

    void Start()
    {
        if (!Application.isPlaying || !autoTriggerOnStart) return;
        if (disableColliderWhenAutoTrigger && _collider != null) _collider.enabled = false;

        if (autoTriggerDelay <= 0f) TryTrigger();
        else StartCoroutine(AutoTriggerAfterDelay(autoTriggerDelay));
    }

    // 다른 시스템(실패 처리 등)에서 직접 호출할 수 있도록 공개 트리거 제공
    public void Trigger()
    {
        TryTrigger();
    }

    // 코드에서 성공/실패(이미 발동됨 등) 확인용
    public bool TryTrigger()
    {
        if (triggerOnlyOnce && _triggered) return false;

#if UNITY_EDITOR
        if (editorOnly && !Application.isEditor) return false;
#endif

        _triggered = true;
        if (!_collider) _collider = GetComponent<Collider>();
        if (_collider && triggerOnlyOnce) _collider.enabled = false;
        StartCoroutine(JumpscareSequence());
        return true;
    }

    IEnumerator JumpscareSequence()
    {
        // 1. 플레이어 조작 막기
        if (playerController != null)
            playerController.enabled = false;

        // 2. 빌드업 단계 (라이트 깜빡임 + 앰비언스 볼륨 업)
        if (usePreScare)
            yield return StartCoroutine(PreScare());

        // 3. 메인 점프스케어
        float jitter = Random.Range(-timingJitter, timingJitter);

        // 3-1. 사운드/몬스터 타이밍 분리 (음수 오프셋도 즉시 처리)
        float monsterDelay = Mathf.Max(0f, delayBeforeMonster + jitter);
        float soundDelay = Mathf.Max(0f, delayBeforeMonster + soundToMonsterOffset + jitter);
        float duckDelay = (jumpscareClip != null && sfxSource != null) ? soundDelay : monsterDelay;

        StartAudioDucking(duckDelay);

        // 사운드
        if (jumpscareClip != null && sfxSource != null)
        {
            if (soundDelay <= 0f) PlaySfx();
            else StartCoroutine(PlaySoundAfterDelay(soundDelay));
        }

        // 몬스터 + 카메라 쉐이크
        yield return StartCoroutine(WaitForMonsterWithBlackout(monsterDelay));
        if (monster != null)
        {
            if (placeMonsterAtCamera && cameraTransform != null)
            {
                Vector3 jitterPos = new Vector3(
                    Random.Range(-monsterPositionJitter.x, monsterPositionJitter.x),
                    Random.Range(-monsterPositionJitter.y, monsterPositionJitter.y),
                    Random.Range(-monsterPositionJitter.z, monsterPositionJitter.z));
                Vector3 jitterEuler = new Vector3(
                    Random.Range(-monsterRotationJitter.x, monsterRotationJitter.x),
                    Random.Range(-monsterRotationJitter.y, monsterRotationJitter.y),
                    Random.Range(-monsterRotationJitter.z, monsterRotationJitter.z));
                monster.transform.position = cameraTransform.TransformPoint(monsterCameraLocalPos + jitterPos);
                monster.transform.rotation = cameraTransform.rotation * Quaternion.Euler(monsterCameraLocalEuler + jitterEuler);
            }
            monster.SetActive(true);
        }

        StartImpactFlash();
        StartFovKick();
        StartMonsterLightPulse();
        StartMonsterLunge();

        if (useCameraShake && cameraTransform != null)
            StartCoroutine(CameraShake());

        // 4. 몬스터가 보이는 채로 잠깐 정지(여운)
        yield return new WaitForSeconds(holdOnMonster);

        // 5. 화면 페이드아웃
        yield return StartCoroutine(FadeOut());

        // 6. 재시작 or 멈추기
        if (showCursorOnEnd)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (autoRestart)
        {
            yield return new WaitForSeconds(delayBeforeRestart);
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
        else
        {
            // 자동 재시작을 사용하지 않으면 플레이어 컨트롤을 돌려줌
            if (playerController != null) playerController.enabled = true;
            if (monster != null) monster.SetActive(false);
        }
    }

    IEnumerator PreScare()
    {
        float elapsed = 0f;
        float nextFlickerTime = 0f;
        float startFadeAlpha = fadeCanvasGroup != null ? fadeCanvasGroup.alpha : 0f;

        while (elapsed < preScareDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float t = preScareDuration > 0f ? Mathf.Clamp01(elapsed / preScareDuration) : 1f;

            // 앰비언스 볼륨 서서히 올리기
            if (useAmbienceRamp && ambienceSource != null)
            {
                ambienceSource.volume = Mathf.Lerp(_originalAmbienceVolume, ambienceTargetVolume, t);
            }

            if (usePreScareFade && fadeCanvasGroup != null)
            {
                float eased = Mathf.SmoothStep(0f, 1f, t);
                fadeCanvasGroup.alpha = Mathf.Lerp(startFadeAlpha, preScareFadeAlpha, eased);
            }

            // 라이트 깜빡임
            if (flickerLights != null && flickerLights.Length > 0)
            {
                nextFlickerTime -= dt;
                if (nextFlickerTime <= 0f)
                {
                    nextFlickerTime = Random.Range(flickerIntervalMin, flickerIntervalMax);
                    for (int i = 0; i < flickerLights.Length; i++)
                    {
                        if (flickerLights[i] == null) continue;
                        if (useIntensityFlicker && _originalLightIntensity != null && i < _originalLightIntensity.Length)
                        {
                            bool wasEnabled = _originalLightEnabled != null && i < _originalLightEnabled.Length && _originalLightEnabled[i];
                            flickerLights[i].enabled = wasEnabled;
                            if (!wasEnabled) continue;
                            float min = Mathf.Min(flickerIntensityMin, flickerIntensityMax);
                            float max = Mathf.Max(flickerIntensityMin, flickerIntensityMax);
                            flickerLights[i].intensity = _originalLightIntensity[i] * Random.Range(min, max);
                        }
                        else
                        {
                            // enabled 토글
                            flickerLights[i].enabled = !flickerLights[i].enabled;
                        }
                    }
                }
            }

            yield return null;
        }

        // 빌드업 끝나면 라이트/앰비언스 원래대로 (원하면 여기 안 돌려도 됨)
        if (useAmbienceRamp && ambienceSource != null)
            ambienceSource.volume = ambienceTargetVolume;

        if (usePreScareFade && fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = preScareFadeAlpha;

        if (flickerLights != null && _originalLightEnabled != null)
        {
            for (int i = 0; i < flickerLights.Length; i++)
            {
                if (flickerLights[i] == null) continue;
                if (useIntensityFlicker && _originalLightIntensity != null && i < _originalLightIntensity.Length)
                    flickerLights[i].intensity = _originalLightIntensity[i];
                flickerLights[i].enabled = _originalLightEnabled[i];
            }
        }
    }

    void PlaySfx()
    {
        if (jumpscareClip == null || sfxSource == null) return;
        float min = Mathf.Min(sfxVolumeRange.x, sfxVolumeRange.y);
        float max = Mathf.Max(sfxVolumeRange.x, sfxVolumeRange.y);
        float volume = Mathf.Max(0f, Random.Range(min, max));
        float pitchMin = Mathf.Min(sfxPitchRange.x, sfxPitchRange.y);
        float pitchMax = Mathf.Max(sfxPitchRange.x, sfxPitchRange.y);
        float pitch = Mathf.Max(0.01f, Random.Range(pitchMin, pitchMax));
        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(jumpscareClip, volume);
        if (restoreSfxPitchAfterPlay)
        {
            float duration = jumpscareClip.length / Mathf.Max(0.01f, pitch);
            if (_restorePitchCo != null) StopCoroutine(_restorePitchCo);
            _restorePitchCo = StartCoroutine(ResetSfxPitchAfterDelay(duration, originalPitch));
        }
    }

    IEnumerator PlaySoundAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        PlaySfx();
    }

    IEnumerator AutoTriggerAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        TryTrigger();
    }

    IEnumerator WaitForMonsterWithBlackout(float monsterDelay)
    {
        if (!usePreImpactBlackout || fadeCanvasGroup == null)
        {
            if (monsterDelay > 0f) yield return new WaitForSeconds(monsterDelay);
            yield break;
        }

        float duration = Mathf.Max(0.01f, preImpactBlackoutDuration);
        duration = GetSafeBlackoutDuration(duration);
        float baseAlpha = fadeCanvasGroup.alpha;
        float blackoutAlpha = Mathf.Clamp01(preImpactBlackoutAlpha);
        blackoutAlpha = GetSafeBlackoutAlpha(blackoutAlpha);

        if (monsterDelay <= 0f)
        {
            fadeCanvasGroup.alpha = blackoutAlpha;
            yield return new WaitForSeconds(duration);
            fadeCanvasGroup.alpha = baseAlpha;
            yield break;
        }

        float lead = Mathf.Clamp(preImpactBlackoutLeadTime, 0f, monsterDelay);
        float durationClamped = Mathf.Min(duration, monsterDelay);
        float waitBefore = Mathf.Max(0f, monsterDelay - lead - durationClamped);
        if (waitBefore > 0f) yield return new WaitForSeconds(waitBefore);

        fadeCanvasGroup.alpha = blackoutAlpha;
        yield return new WaitForSeconds(durationClamped);
        fadeCanvasGroup.alpha = baseAlpha;

        float remaining = monsterDelay - waitBefore - durationClamped;
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
    }

    void StartImpactFlash()
    {
        if (!useImpactFlash || fadeCanvasGroup == null) return;
        if (_impactFlashCo != null) StopCoroutine(_impactFlashCo);
        float baseAlpha = fadeCanvasGroup.alpha;
        _impactFlashCo = StartCoroutine(ImpactFlash(baseAlpha));
    }

    IEnumerator ImpactFlash(float baseAlpha)
    {
        int count = Mathf.Max(1, impactFlashCount);
        count = GetSafeFlashCount(count);
        float duration = Mathf.Max(0.01f, impactFlashDuration);
        duration = GetSafeFlashDuration(duration);
        float gap = Mathf.Max(0f, impactFlashGap);
        float flashAlpha = Mathf.Clamp01(impactFlashAlpha);
        flashAlpha = GetSafeFlashAlpha(flashAlpha);
        for (int i = 0; i < count; i++)
        {
            fadeCanvasGroup.alpha = flashAlpha;
            yield return new WaitForSeconds(duration);
            fadeCanvasGroup.alpha = baseAlpha;
            if (gap > 0f && i < count - 1)
                yield return new WaitForSeconds(gap);
        }
    }

    void StartFovKick()
    {
        if (!useFovKick) return;
        Camera cam = ResolveImpactCamera();
        if (cam == null) return;
        if (_fovKickCo != null) StopCoroutine(_fovKickCo);
        _fovKickCo = StartCoroutine(FovKickRoutine(cam));
    }

    IEnumerator FovKickRoutine(Camera cam)
    {
        float baseFov = cam.fieldOfView;
        float targetFov = Mathf.Clamp(baseFov + fovKick, 10f, 170f);
        float inTime = Mathf.Max(0.01f, fovKickInTime);
        float outTime = Mathf.Max(0.01f, fovKickOutTime);

        float elapsed = 0f;
        while (elapsed < inTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / inTime);
            cam.fieldOfView = Mathf.Lerp(baseFov, targetFov, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < outTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / outTime);
            cam.fieldOfView = Mathf.Lerp(targetFov, baseFov, t);
            yield return null;
        }

        cam.fieldOfView = baseFov;
    }

    void StartAudioDucking(float soundDelay)
    {
        if (!useAudioDucking) return;
        if (_duckCo != null) StopCoroutine(_duckCo);
        _duckCo = StartCoroutine(AudioDuckingRoutine(soundDelay));
    }

    IEnumerator AudioDuckingRoutine(float soundDelay)
    {
        BuildDuckTargets();
        if (_duckTargets.Count == 0) yield break;

        CaptureVolumes(_duckTargets, _duckOriginalVolumes);
        float wait = Mathf.Max(0f, soundDelay - Mathf.Max(0f, duckLeadTime));
        if (wait > 0f) yield return new WaitForSeconds(wait);

        float inTime = Mathf.Max(0.01f, duckInTime);
        float outTime = Mathf.Max(0.01f, duckOutTime);
        float target = Mathf.Clamp01(duckTargetVolume);

        FillTargetVolumes(_duckTargets.Count, target, _duckTargetVolumes);
        CaptureVolumes(_duckTargets, _duckStartVolumes);
        yield return FadeVolumes(_duckTargets, _duckStartVolumes, _duckTargetVolumes, inTime);

        if (duckHoldTime > 0f) yield return new WaitForSeconds(duckHoldTime);

        CaptureVolumes(_duckTargets, _duckStartVolumes);
        yield return FadeVolumes(_duckTargets, _duckStartVolumes, _duckOriginalVolumes, outTime);
    }

    void BuildDuckTargets()
    {
        _duckTargets.Clear();
        if (duckSources != null && duckSources.Length > 0)
        {
            for (int i = 0; i < duckSources.Length; i++)
            {
                var src = duckSources[i];
                if (src == null || src == sfxSource) continue;
                if (!_duckTargets.Contains(src)) _duckTargets.Add(src);
            }
        }

        if (ambienceSource != null && ambienceSource != sfxSource && !_duckTargets.Contains(ambienceSource))
            _duckTargets.Add(ambienceSource);
    }

    static void CaptureVolumes(List<AudioSource> sources, List<float> volumes)
    {
        volumes.Clear();
        for (int i = 0; i < sources.Count; i++)
        {
            var src = sources[i];
            volumes.Add(src != null ? src.volume : 0f);
        }
    }

    static void FillTargetVolumes(int count, float target, List<float> volumes)
    {
        volumes.Clear();
        for (int i = 0; i < count; i++) volumes.Add(target);
    }

    static IEnumerator FadeVolumes(List<AudioSource> sources, List<float> from, List<float> to, float duration)
    {
        if (sources.Count == 0) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            for (int i = 0; i < sources.Count; i++)
            {
                var src = sources[i];
                if (src == null) continue;
                float start = i < from.Count ? from[i] : src.volume;
                float end = i < to.Count ? to[i] : src.volume;
                src.volume = Mathf.Lerp(start, end, k);
            }
            yield return null;
        }

        for (int i = 0; i < sources.Count; i++)
        {
            var src = sources[i];
            if (src == null) continue;
            float end = i < to.Count ? to[i] : src.volume;
            src.volume = end;
        }
    }

    void StartMonsterLightPulse()
    {
        if (!useMonsterLightPulse) return;
        var light = ResolveMonsterLight();
        if (light == null) return;
        if (_monsterLightCo != null) StopCoroutine(_monsterLightCo);
        _monsterLightCo = StartCoroutine(MonsterLightPulse(light));
    }

    IEnumerator MonsterLightPulse(Light light)
    {
        if (light == null) yield break;
        bool wasEnabled = light.enabled;
        float baseIntensity = light.intensity;
        Color baseColor = light.color;

        float target = monsterLightTargetIntensity;
        target = GetSafeLightIntensity(target);
        float inTime = Mathf.Max(0.01f, monsterLightInTime);
        float outTime = Mathf.Max(0.01f, monsterLightOutTime);
        inTime = GetSafeLightTime(inTime);
        outTime = GetSafeLightTime(outTime);

        if (overrideMonsterLightColor) light.color = monsterLightColor;
        light.enabled = true;

        yield return LerpLightIntensity(light, baseIntensity, target, inTime);
        if (monsterLightHoldTime > 0f) yield return new WaitForSeconds(monsterLightHoldTime);
        yield return LerpLightIntensity(light, target, baseIntensity, outTime);

        light.intensity = baseIntensity;
        light.color = baseColor;
        light.enabled = wasEnabled;
    }

    static IEnumerator LerpLightIntensity(Light light, float from, float to, float duration)
    {
        if (light == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            light.intensity = Mathf.Lerp(from, to, k);
            yield return null;
        }
        light.intensity = to;
    }

    void StartMonsterLunge()
    {
        if (!useMonsterLunge || monster == null) return;
        if (_lungeCo != null) StopCoroutine(_lungeCo);
        _lungeCo = StartCoroutine(MonsterLunge());
    }

    IEnumerator MonsterLunge()
    {
        if (monster == null) yield break;
        float duration = Mathf.Max(0.01f, lungeDuration);
        if (lungeDistance <= 0f) yield break;

        Transform m = monster.transform;
        Vector3 startPos = m.position;
        Vector3 dir;
        if (cameraTransform != null)
            dir = (cameraTransform.position - startPos).normalized;
        else
            dir = -m.forward;

        if (dir.sqrMagnitude < 0.0001f) yield break;

        float maxLunge = lungeDistance;
        if (cameraTransform != null)
        {
            float distance = Vector3.Distance(startPos, cameraTransform.position);
            maxLunge = Mathf.Min(lungeDistance, Mathf.Max(0f, distance - 0.1f));
        }
        if (maxLunge <= 0f) yield break;

        Vector3 endPos = startPos + dir * maxLunge;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            m.position = Vector3.Lerp(startPos, endPos, eased);
            yield return null;
        }
        m.position = endPos;
    }

    IEnumerator ResetSfxPitchAfterDelay(float delay, float originalPitch)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (sfxSource != null) sfxSource.pitch = originalPitch;
    }

    IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null)
            yield break;

        float elapsed = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    IEnumerator CameraShake()
    {
        if (cameraTransform == null)
            yield break;

        float elapsed = 0f;
        Vector3 startPos = cameraTransform.localPosition;
        Quaternion startRot = cameraTransform.localRotation;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, shakeDuration));
            float damper = 1f - Mathf.SmoothStep(0f, 1f, t);
            float x = Random.Range(-1f, 1f) * shakeIntensity * damper;
            float y = Random.Range(-1f, 1f) * shakeIntensity * damper;
            cameraTransform.localPosition = startPos + new Vector3(x, y, 0f);
            if (useRotationShake)
            {
                float rx = Random.Range(-1f, 1f) * shakeRotationIntensity * damper;
                float ry = Random.Range(-1f, 1f) * shakeRotationIntensity * damper;
                float rz = Random.Range(-1f, 1f) * shakeRotationIntensity * 0.6f * damper;
                cameraTransform.localRotation = startRot * Quaternion.Euler(rx, ry, rz);
            }
            yield return null;
        }

        cameraTransform.localPosition = startPos;
        cameraTransform.localRotation = startRot;
    }

    void OnDrawGizmosSelected()
    {
        // 씬 뷰에서 트리거 영역 빨간 상자로 표시
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.matrix = col.transform.localToWorldMatrix;
        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(box.center, box.size);
        }

        DrawPlacementPreview();
    }

    void DrawPlacementPreview()
    {
        if (!showPlacementPreview || !placeMonsterAtCamera || monster == null) return;
        Transform previewCamera = GetPreviewCameraTransform();
        if (previewCamera == null) return;

        Vector3 basePos = previewCamera.TransformPoint(monsterCameraLocalPos);
        Quaternion baseRot = previewCamera.rotation * Quaternion.Euler(monsterCameraLocalEuler);
        Vector3 baseScale = monster.transform.lossyScale;

        Color oldColor = Gizmos.color;
        Matrix4x4 oldMatrix = Gizmos.matrix;

        bool drewMesh = false;
        Matrix4x4 baseMatrix = Matrix4x4.TRS(basePos, baseRot, baseScale);
        var meshFilters = monster.GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters != null && meshFilters.Length > 0)
        {
            Gizmos.color = previewColor;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                var mf = meshFilters[i];
                if (mf == null || mf.sharedMesh == null) continue;
                Matrix4x4 localFromRoot = monster.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                Gizmos.matrix = baseMatrix * localFromRoot;
                Gizmos.DrawWireMesh(mf.sharedMesh);
                drewMesh = true;
            }
        }

        var skinnedMeshes = monster.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skinnedMeshes != null && skinnedMeshes.Length > 0)
        {
            Gizmos.color = previewColor;
            for (int i = 0; i < skinnedMeshes.Length; i++)
            {
                var smr = skinnedMeshes[i];
                if (smr == null || smr.sharedMesh == null) continue;
                Matrix4x4 localFromRoot = monster.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                Gizmos.matrix = baseMatrix * localFromRoot;
                Gizmos.DrawWireMesh(smr.sharedMesh);
                drewMesh = true;
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = previewColor;
        if (!drewMesh)
        {
            Gizmos.DrawWireSphere(basePos, 0.08f);
        }

        if (previewForwardLength > 0f)
        {
            Vector3 forward = baseRot * Vector3.forward;
            Gizmos.DrawLine(basePos, basePos + forward * previewForwardLength);
        }

        if (showJitterPreview)
        {
            Vector3 size = new Vector3(
                Mathf.Abs(monsterPositionJitter.x) * 2f,
                Mathf.Abs(monsterPositionJitter.y) * 2f,
                Mathf.Abs(monsterPositionJitter.z) * 2f);
            if (size.sqrMagnitude > 0.0001f)
            {
                Gizmos.color = previewJitterColor;
                Gizmos.matrix = Matrix4x4.TRS(basePos, previewCamera.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
            }
        }

        Gizmos.color = oldColor;
        Gizmos.matrix = oldMatrix;
    }

    Transform GetPreviewCameraTransform()
    {
        if (cameraTransform != null) return cameraTransform;
        Camera main = Camera.main;
        if (main != null) return main.transform;
        return Camera.current != null ? Camera.current.transform : null;
    }

    Camera ResolveImpactCamera()
    {
        if (targetCamera != null) return targetCamera;
        if (cameraTransform != null)
        {
            var cam = cameraTransform.GetComponentInChildren<Camera>();
            if (cam != null) return cam;
        }
        return Camera.main;
    }

    Light ResolveMonsterLight()
    {
        if (monsterLight != null) return monsterLight;
        if (!autoFindMonsterLight || monster == null) return null;
        monsterLight = monster.GetComponentInChildren<Light>(true);
        return monsterLight;
    }

    float GetSafeFlashAlpha(float alpha)
        => photosensitiveSafeMode ? Mathf.Min(alpha, Mathf.Clamp01(safeMaxFlashAlpha)) : alpha;

    int GetSafeFlashCount(int count)
        => photosensitiveSafeMode ? Mathf.Min(count, Mathf.Max(1, safeMaxFlashCount)) : count;

    float GetSafeFlashDuration(float duration)
        => photosensitiveSafeMode ? Mathf.Max(duration, safeMinFlashDuration) : duration;

    float GetSafeBlackoutAlpha(float alpha)
        => photosensitiveSafeMode ? Mathf.Min(alpha, Mathf.Clamp01(safeMaxBlackoutAlpha)) : alpha;

    float GetSafeBlackoutDuration(float duration)
        => photosensitiveSafeMode ? Mathf.Max(duration, safeMinBlackoutDuration) : duration;

    float GetSafeLightTime(float duration)
        => photosensitiveSafeMode ? Mathf.Max(duration, safeMinLightPulseTime) : duration;

    float GetSafeLightIntensity(float intensity)
        => photosensitiveSafeMode ? Mathf.Min(intensity, safeMaxMonsterLightIntensity) : intensity;
}
