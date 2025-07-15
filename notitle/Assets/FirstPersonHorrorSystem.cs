using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.PostProcessing;
using System.Collections.Generic;
using System.Linq;

public class FirstPersonHorrorSystem : MonoBehaviour
{
    [Header("Movement Detection")]
    public float sneakThreshold = 1.5f;    // �� ���� ����
    public float walkThreshold = 3.5f;       // �� ���� ����
    public float runThreshold = 7f;        // �� ���� ����

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

    // �ӵ� ����� ���� �߰� ����
    private float speedCalculationTimer = 0f;
    private float speedCalculationInterval = 0.1f; // 0.1�ʸ��� ���

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

        // Shadow Atlas ũ�� ����
        QualitySettings.shadowResolution = ShadowResolution.Medium;

        // ���� ���� ����ȭ
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.shadows != LightShadows.None)
            {
                light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low; // ������ �ڵ�
            }
        }

        // Post-processing ����
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGetSettings(out colorGrading);
            postProcessVolume.profile.TryGetSettings(out vignette);
            postProcessVolume.profile.TryGetSettings(out chromaticAberration);
        }

        // �ʱ� ����� ����
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
        // ���� �������θ� �ӵ� ���
        speedCalculationTimer += Time.deltaTime;

        if (speedCalculationTimer >= speedCalculationInterval)
        {
            Vector3 currentPosition = transform.position;
            float distance = Vector3.Distance(currentPosition, lastPosition);
            currentSpeed = distance / speedCalculationTimer;

            lastPosition = currentPosition;
            speedCalculationTimer = 0f;

            // �ӵ� �����丮 ���� (�ֱ� 10�� ���ø� ����)
            speedHistory.Enqueue(currentSpeed);
            if (speedHistory.Count > 10)
                speedHistory.Dequeue();

            // ��� �ӵ� ��� (�� ���� ����)
            averageSpeed = speedHistory.Count > 0 ? speedHistory.Average() : 0f;

            // �ʹ� ���� ���� 0���� ó��
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
            // ���� �Ǵ� �ſ� ���� ������ - ���尨 ����
            targetIntensity = 0.8f * maxHorrorIntensity;
        }
        else if (averageSpeed > walkThreshold)
        {
            // ���� ������ - �д� ����
            targetIntensity = 1f * maxHorrorIntensity;
        }
        else if (averageSpeed > sneakThreshold)
        {
            // ���� �ȱ� - �߰� ����
            targetIntensity = 0.5f * maxHorrorIntensity;
        }

        // �ε巯�� ��ȯ
        float changeSpeed = intensityChangeSpeed * Time.deltaTime;
        if (targetIntensity > horrorIntensity)
            changeSpeed *= 0.5f; // ���尨 ������ õõ��

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
        // ����ڵ� ȿ��
        if (heartbeatSource != null)
        {
            heartbeatSource.volume = horrorIntensity * 1f;
            heartbeatSource.pitch = Mathf.Lerp(0.5f, 1.5f, horrorIntensity); // ����ڵ� �ӵ� ��ȭ
        }

        // ���Ҹ� ȿ�� (������ ���� ��)
        if (breathingSource != null)
        {
            breathingSource.pitch = 1f; // ���Ҹ��� �����̸� �����ϰ� ����
            if (averageSpeed < sneakThreshold)
            {
                breathingSource.volume = horrorIntensity * 0.1f;
            }
            else
            {
                breathingSource.volume = Mathf.Lerp(breathingSource.volume, 0f, Time.deltaTime * 2f);
            }
        }

        // �ֺ� ���� (ambient �Ҹ� ���� ����)
        if (ambientSource != null)
        {
            if (averageSpeed > walkThreshold)
            {
                ambientSource.volume = horrorIntensity * 0.5f; // ���� 0.8f���� 0.5f�� ����
            }
            else
            {
                ambientSource.volume = horrorIntensity * 0.1f; // ���� 0.3f���� 0.1f�� ����
            }
        }

        // �߼Ҹ� ȿ��
        PlayFootstepEffects();
    }

    void ApplyVisualEffects()
    {
        // ������ ȿ��
        if (flashlight != null)
        {
            flashlight.intensity = Mathf.Lerp(1f, 0.3f + (Mathf.Sin(Time.time * 10f) * 0.1f), horrorIntensity);
            flashlight.range = Mathf.Lerp(10f, 6f, horrorIntensity);
        }

        // Post-processing ȿ��
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            // ���� ����
            if (colorGrading != null)
            {
                colorGrading.temperature.value = Mathf.Lerp(0f, -20f, horrorIntensity);
                colorGrading.saturation.value = Mathf.Lerp(0f, -30f, horrorIntensity);
            }

            // ����� ȿ��
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0.2f, 0.6f, horrorIntensity);
            }

            // ������ ȿ��
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(0f, 0.3f, horrorIntensity);
            }
        }
    }

    void ApplyCameraEffects()
    {
        if (!enableCameraShake || playerCamera == null) return;

        // ī�޶� ��鸲 ȿ��
        float shakeIntensity = 0f;

        if (averageSpeed > walkThreshold)
        {
            // ������ ������ �� - �޸��� ��鸲
            shakeIntensity = horrorIntensity * 0.02f;
        }
        else if (averageSpeed < sneakThreshold)
        {
            // ������ ���� �� - ���尨 ��鸲
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

        // �����ӿ� ���� �߼Ҹ� ��ȭ
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
        // ����� �ҽ��� �ʱ� ����
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

    // ������ - ���� ���� Ȯ��
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