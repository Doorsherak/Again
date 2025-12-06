using System.Collections;
using UnityEngine;
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

    [Header("빌드업 옵션")]
    public bool usePreScare = true;             // 빌드업(라이트/앰비언스) 사용할지
    public float preScareDuration = 2.0f;       // 빌드업 전체 길이
    public Light[] flickerLights;               // 깜빡이게 만들 라이트들
    public bool useAmbienceRamp = true;         // 앰비언스 볼륨 올릴지
    public AudioSource ambienceSource;          // 배경 앰비언스 AudioSource
    public float ambienceTargetVolume = 0.8f;   // 빌드업 끝에서의 목표 볼륨
    public float flickerIntervalMin = 0.05f;
    public float flickerIntervalMax = 0.2f;

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

    [Header("디버그 / 개발용")]
    public bool triggerOnlyOnce = true;         // true면 한 번만 발동
    public bool editorOnly = false;             // 에디터에서만 발동(빌드에선 무시)

    bool _triggered = false;
    Vector3 _originalCamPos;

    float _originalAmbienceVolume;
    bool[] _originalLightEnabled;
    Collider _collider;

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
            for (int i = 0; i < flickerLights.Length; i++)
            {
                if (flickerLights[i] != null)
                    _originalLightEnabled[i] = flickerLights[i].enabled;
            }
        }

        if (cameraTransform != null)
            _originalCamPos = cameraTransform.localPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && _triggered) return;
        if (!other.CompareTag("Player")) return;

#if UNITY_EDITOR
        if (editorOnly && !Application.isEditor) return;
#endif

        _triggered = true;
        if (_collider && triggerOnlyOnce) _collider.enabled = false;
        StartCoroutine(JumpscareSequence());
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

        // 사운드
        if (jumpscareClip != null && sfxSource != null)
        {
            if (soundDelay > 0f) yield return new WaitForSeconds(soundDelay);
            sfxSource.PlayOneShot(jumpscareClip);
        }

        // 몬스터 + 카메라 쉐이크
        if (monsterDelay > 0f) yield return new WaitForSeconds(monsterDelay);
        if (monster != null)
            monster.SetActive(true);

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

        while (elapsed < preScareDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            // 앰비언스 볼륨 서서히 올리기
            if (useAmbienceRamp && ambienceSource != null)
            {
                float t = Mathf.Clamp01(elapsed / preScareDuration);
                ambienceSource.volume = Mathf.Lerp(_originalAmbienceVolume, ambienceTargetVolume, t);
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
                        // enabled 토글
                        flickerLights[i].enabled = !flickerLights[i].enabled;
                    }
                }
            }

            yield return null;
        }

        // 빌드업 끝나면 라이트/앰비언스 원래대로 (원하면 여기 안 돌려도 됨)
        if (useAmbienceRamp && ambienceSource != null)
            ambienceSource.volume = ambienceTargetVolume;

        if (flickerLights != null && _originalLightEnabled != null)
        {
            for (int i = 0; i < flickerLights.Length; i++)
            {
                if (flickerLights[i] == null) continue;
                flickerLights[i].enabled = _originalLightEnabled[i];
            }
        }
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
        Vector3 startPos = _originalCamPos;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            cameraTransform.localPosition = startPos + new Vector3(x, y, 0f);
            yield return null;
        }

        cameraTransform.localPosition = startPos;
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
    }
}
