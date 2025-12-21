using UnityEngine;

[CreateAssetMenu(menuName = "Game/Configs/Jumpscare Config", fileName = "JumpscareConfig")]
public class JumpscareConfig : ScriptableObject
{
    [Header("Safety")]
    public bool photosensitiveSafeMode = true;
    [Range(0f, 1f)] public float safeMaxFlashAlpha = 0.6f;
    [Min(0)] public int safeMaxFlashCount = 1;
    [Min(0f)] public float safeMinFlashDuration = 0.08f;
    [Range(0f, 1f)] public float safeMaxBlackoutAlpha = 0.8f;
    [Min(0f)] public float safeMinBlackoutDuration = 0.05f;
    [Min(0f)] public float safeMinLightPulseTime = 0.08f;
    [Min(0f)] public float safeMaxMonsterLightIntensity = 6f;

    [Header("Impact / Camera (Optional Overrides)")]
    public bool overrideImpact = false;
    public bool useImpactFlash = true;
    [Range(0f, 1f)] public float impactFlashAlpha = 0.85f;
    [Min(0)] public int impactFlashCount = 1;
    [Min(0f)] public float impactFlashDuration = 0.05f;
    [Min(0f)] public float impactFlashGap = 0.05f;
    public bool useCameraShake = true;
    [Min(0f)] public float shakeDuration = 0.3f;
    [Min(0f)] public float shakeIntensity = 0.1f;
    public bool useRotationShake = true;
    [Min(0f)] public float shakeRotationIntensity = 1.5f;
}

