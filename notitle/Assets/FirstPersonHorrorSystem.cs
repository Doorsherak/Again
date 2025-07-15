using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.PostProcessing;
using System.Collections.Generic;
using System.Linq;

public class FirstPersonHorrorSystem : MonoBehaviour
{
    [Header("Movement Detection")]
    public float sneakThreshold = 1.5f;    // 더 낮게 조정
    public float walkThreshold = 3.5f;       // 더 낮게 조정
    public float runThreshold = 7f;        // 더 낮게 조정

    [Header("Audio Effects")]
    public AudioSource heartbeatSource;
    public AudioSource breathingSource;
    public AudioSource ambientSource;
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;

    [Header("Visual Effects")]
    public Light flashlight;
    public PostProcessVolume postProcessVolume;
    public Camera playerCamera;

    [Header("Horror Settings")]
    public float maxHorrorIntensity = 1f;
    public float intensityChangeSpeed = 1f;
    public bool enableCameraShake = true;

    // Private variables
    private CharacterController controller;
    private Vector3 lastPosition;
    private float currentSpeed;
    private Queue<float> speedHistory = new Queue<float>();
    private float averageSpeed;
    private float horrorIntensity = 0f;
    private Vector3 originalCameraPosition;

    // 속도 계산을 위한 추가 변수
    private float speedCalculationTimer = 0f;
    private float speedCalculationInterval = 0.1f; // 0.1초마다 계산

    // Post-processing effects
    private ColorGrading colorGrading;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    // ...

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;

        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.localPosition;
        }

        // Shadow Atlas 크기 조정
        QualitySettings.shadowResolution = ShadowResolution.Medium;

        // 조명 설정 최적화
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.shadows != LightShadows.None)
            {
                light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low; // 수정된 코드
            }
        }

        // Post-processing 설정
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGetSettings(out colorGrading);
            postProcessVolume.profile.TryGetSettings(out vignette);
            postProcessVolume.profile.TryGetSettings(out chromaticAberration);
        }

        // 초기 오디오 설정
        InitializeAudioSources();
    }

    void Update()
    {
        CalculateMovementSpeed();
        UpdateHorrorIntensity();
        ApplyHorrorEffects();
    }

    void CalculateMovementSpeed()
    {
        // 일정 간격으로만 속도 계산
        speedCalculationTimer += Time.deltaTime;

        if (speedCalculationTimer >= speedCalculationInterval)
        {
            Vector3 currentPosition = transform.position;
            float distance = Vector3.Distance(currentPosition, lastPosition);
            currentSpeed = distance / speedCalculationTimer;

            lastPosition = currentPosition;
            speedCalculationTimer = 0f;

            // 속도 히스토리 관리 (최근 10개 샘플만 유지)
            speedHistory.Enqueue(currentSpeed);
            if (speedHistory.Count > 10)
                speedHistory.Dequeue();

            // 평균 속도 계산 (더 빠른 반응)
            averageSpeed = speedHistory.Count > 0 ? speedHistory.Average() : 0f;

            // 너무 작은 값은 0으로 처리
            if (averageSpeed < 0.1f)
            {
                averageSpeed = 0f;
                currentSpeed = 0f;
            }
        }
    }

    void UpdateHorrorIntensity()
    {
        float targetIntensity = 0f;

        if (averageSpeed < sneakThreshold)
        {
            // 정지 또는 매우 느린 움직임 - 긴장감 증가
            targetIntensity = 0.8f * maxHorrorIntensity;
        }
        else if (averageSpeed > walkThreshold)
        {
            // 빠른 움직임 - 패닉 상태
            targetIntensity = 1f * maxHorrorIntensity;
        }
        else if (averageSpeed > sneakThreshold)
        {
            // 보통 걷기 - 중간 긴장
            targetIntensity = 0.5f * maxHorrorIntensity;
        }

        // 부드러운 전환
        float changeSpeed = intensityChangeSpeed * Time.deltaTime;
        if (targetIntensity > horrorIntensity)
            changeSpeed *= 0.5f; // 긴장감 증가는 천천히

        horrorIntensity = Mathf.Lerp(horrorIntensity, targetIntensity, changeSpeed);
    }

    void ApplyHorrorEffects()
    {
        ApplyAudioEffects();
        ApplyVisualEffects();
        ApplyCameraEffects();
    }

    void ApplyAudioEffects()
    {
        // 심장박동 효과
        if (heartbeatSource != null)
        {
            heartbeatSource.volume = horrorIntensity * 1f;
            heartbeatSource.pitch = Mathf.Lerp(0.5f, 1.5f, horrorIntensity); // 심장박동 속도 변화
        }

        // 숨소리 효과 (가만히 있을 때)
        if (breathingSource != null)
        {
            breathingSource.pitch = 1f; // 숨소리의 높낮이를 일정하게 유지
            if (averageSpeed < sneakThreshold)
            {
                breathingSource.volume = horrorIntensity * 0.1f;
            }
            else
            {
                breathingSource.volume = Mathf.Lerp(breathingSource.volume, 0f, Time.deltaTime * 2f);
            }
        }

        // 주변 소음 (ambient 소리 볼륨 감소)
        if (ambientSource != null)
        {
            if (averageSpeed > walkThreshold)
            {
                ambientSource.volume = horrorIntensity * 0.5f; // 기존 0.8f에서 0.5f로 감소
            }
            else
            {
                ambientSource.volume = horrorIntensity * 0.1f; // 기존 0.3f에서 0.1f로 감소
            }
        }

        // 발소리 효과
        PlayFootstepEffects();
    }

    void ApplyVisualEffects()
    {
        // 손전등 효과
        if (flashlight != null)
        {
            flashlight.intensity = Mathf.Lerp(1f, 0.3f + (Mathf.Sin(Time.time * 10f) * 0.1f), horrorIntensity);
            flashlight.range = Mathf.Lerp(10f, 6f, horrorIntensity);
        }

        // Post-processing 효과
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            // 색조 조정
            if (colorGrading != null)
            {
                colorGrading.temperature.value = Mathf.Lerp(0f, -20f, horrorIntensity);
                colorGrading.saturation.value = Mathf.Lerp(0f, -30f, horrorIntensity);
            }

            // 비네팅 효과
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0.2f, 0.6f, horrorIntensity);
            }

            // 색수차 효과
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(0f, 0.3f, horrorIntensity);
            }
        }
    }

    void ApplyCameraEffects()
    {
        if (!enableCameraShake || playerCamera == null) return;

        // 카메라 흔들림 효과
        float shakeIntensity = 0f;

        if (averageSpeed > walkThreshold)
        {
            // 빠르게 움직일 때 - 달리기 흔들림
            shakeIntensity = horrorIntensity * 0.02f;
        }
        else if (averageSpeed < sneakThreshold)
        {
            // 가만히 있을 때 - 긴장감 흔들림
            shakeIntensity = horrorIntensity * 0.005f;
        }

        if (shakeIntensity > 0f)
        {
            Vector3 shake = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                Random.Range(-shakeIntensity, shakeIntensity),
                0f
            );
            playerCamera.transform.localPosition = originalCameraPosition + shake;
        }
        else
        {
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                originalCameraPosition,
                Time.deltaTime * 5f
            );
        }
    }

    void PlayFootstepEffects()
    {
        if (footstepSource == null || footstepClips.Length == 0) return;

        // 움직임에 따른 발소리 변화
        if (averageSpeed > walkThreshold && !footstepSource.isPlaying)
        {
            footstepSource.clip = footstepClips[Random.Range(0, footstepClips.Length)];
            footstepSource.volume = Mathf.Lerp(0.3f, 0.8f, horrorIntensity);
            footstepSource.pitch = Mathf.Lerp(0.8f, 1.2f, averageSpeed / runThreshold);
            footstepSource.Play();
        }
    }

    void InitializeAudioSources()
    {
        // 오디오 소스들 초기 설정
        if (heartbeatSource != null)
        {
            heartbeatSource.loop = true;
            heartbeatSource.volume = 0f;
            heartbeatSource.Play();
        }

        if (breathingSource != null)
        {
            breathingSource.loop = true;
            breathingSource.volume = 0f;
            breathingSource.Play();
        }

        if (ambientSource != null)
        {
            ambientSource.loop = true;
            ambientSource.volume = 0f;
            ambientSource.Play();
        }
    }

    // 디버깅용 - 현재 상태 확인
    void OnGUI()
    {
        if (Application.isEditor)
        {
            GUILayout.Label($"Speed: {currentSpeed:F2}");
            GUILayout.Label($"Avg Speed: {averageSpeed:F2}");
            GUILayout.Label($"Horror Intensity: {horrorIntensity:F2}");

            string state = "Still";
            if (averageSpeed < sneakThreshold) state = "Still/Sneaking";
            else if (averageSpeed > walkThreshold) state = "Running";
            else if (averageSpeed > sneakThreshold) state = "Walking";
           

            GUILayout.Label($"State: {state}");
        }
    }
}