using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class HorrorAtmosphere : MonoBehaviour
{
    [Header("Lighting Control")]
    public Light[] allLights;
    public float lightFailureChance = 0.001f;
    public float powerOutageDuration = 10f;

    [Header("Horror Effects")]
    public AudioSource ambientSound;
    public AudioClip[] creepySounds;
    public float soundTriggerChance = 0.0005f;

    [Header("Visual Effects")]
    public PostProcessVolume postProcessVolume;
    public Material fogMaterial;
    public ParticleSystem dustParticles;

    private bool isPowerOut = false;
    private float originalAmbientIntensity;

    void Start()
    {
        // 모든 조명 찾기
        if (allLights.Length == 0)
        {
            allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        }
    }

    void Update()
    {
        // 랜덤 정전 효과
        if (!isPowerOut && Random.value < lightFailureChance)
        {
            StartCoroutine(PowerOutage());
        }

        // 랜덤 무서운 소리
        if (Random.value < soundTriggerChance)
        {
            PlayRandomCreepySound();
        }

        // 조명 깜빡임 효과
        RandomLightFlicker();
    }

    void SetupDustParticles()
    {
        if (dustParticles != null)
        {
            var main = dustParticles.main;
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.1f);
            main.startSize = 0.01f;
            main.startSpeed = 0.1f;
            main.maxParticles = 50;

            var emission = dustParticles.emission;
            emission.rateOverTime = 2f;

            var shape = dustParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 3f, 20f);
        }
    }

    void SetupHorrorSounds()
    {
        if (ambientSound != null)
        {
            ambientSound.volume = 0.3f;
            ambientSound.loop = true;
            ambientSound.pitch = Random.Range(0.95f, 1.05f);
        }
    }

    System.Collections.IEnumerator PowerOutage()
    {
        isPowerOut = true;

        // 모든 조명 끄기
        foreach (Light light in allLights)
        {
            if (light != null)
            {
                light.intensity *= 0.1f;
            }
        }

        // 무서운 소리 재생
        PlayRandomCreepySound();

        yield return new WaitForSeconds(powerOutageDuration);

        // 조명 복구
        foreach (Light light in allLights)
        {
            if (light != null)
            {
                light.intensity *= 10f;
            }
        }

        isPowerOut = false;
    }

    void RandomLightFlicker()
    {
        if (isPowerOut) return;

        foreach (Light light in allLights)
        {
            if (light != null && Random.value < 0.001f)
            {
                StartCoroutine(FlickerLight(light));
            }
        }
    }

    System.Collections.IEnumerator FlickerLight(Light light)
    {
        float originalIntensity = light.intensity;

        for (int i = 0; i < 5; i++)
        {
            light.intensity = originalIntensity * Random.Range(0.1f, 1f);
            yield return new WaitForSeconds(0.1f);
        }

        light.intensity = originalIntensity;
    }

    void PlayRandomCreepySound()
    {
        if (creepySounds.Length > 0 && ambientSound != null)
        {
            AudioClip randomSound = creepySounds[Random.Range(0, creepySounds.Length)];
            ambientSound.PlayOneShot(randomSound, Random.Range(0.3f, 0.7f));
        }
    }

    // 외부에서 호출 가능한 메서드들
    public void TriggerPowerOutage()
    {
        if (!isPowerOut)
        {
            StartCoroutine(PowerOutage());
        }
    }

    public void SetHorrorLevel(float level)
    {
        // 공포 레벨에 따른 조정 (0-1)
        lightFailureChance = 0.001f * level;
        soundTriggerChance = 0.0005f * level;
        RenderSettings.fogDensity = 0.01f + (0.03f * level);
    }
}