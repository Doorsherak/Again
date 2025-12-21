using UnityEngine;

[CreateAssetMenu(menuName = "Game/Configs/Horror Atmosphere Director Config", fileName = "HorrorAtmosphereDirectorConfig")]
public class HorrorAtmosphereDirectorConfig : ScriptableObject
{
    [Header("Enable")]
    public bool enableDirector = true;
    public bool disableInMenuScenes = true;
    public string[] menuSceneNames = { "StartScreen", "Options", "Credits" };

    [Header("Tension")]
    [Range(0f, 1f)] public float baseTension = 0.25f;
    [Range(0f, 1f)] public float watchingTension = 0.85f;
    [Range(0f, 1f)] public float progressWeight = 0.45f;
    [Min(0f)] public float responseSpeed = 4f;

    [Header("Safety")]
    public bool photosensitiveSafeMode = true;
    [Range(0f, 1f)] public float safeMaxVignettePulseAmount = 0.03f;
    [Min(0f)] public float safeMaxVignettePulseSpeed = 0.8f;

    [Header("Vignette")]
    public bool enableVignette = true;
    [Range(0f, 1f)] public float vignetteMinAlpha = 0.06f;
    [Range(0f, 1f)] public float vignetteMaxAlpha = 0.28f;
    [Min(0f)] public float vignettePulseSpeed = 0.6f;
    [Range(0f, 0.2f)] public float vignettePulseAmount = 0.02f;
    [Range(64, 512)] public int vignetteTextureSize = 256;
    [Range(0f, 1f)] public float vignetteInnerRadius = 0.35f;
    [Range(0f, 1f)] public float vignetteOuterRadius = 0.95f;
    public int overlaySortingOrder = 200;

    [Header("Audio Low-Pass (Optional)")]
    public bool enableLowPass = false;
    [Min(0f)] public float lowPassMinCutoff = 6500f;
    [Min(0f)] public float lowPassMaxCutoff = 22000f;
    [Range(0.1f, 10f)] public float lowPassResonanceQ = 1f;

    [Header("Light Flicker (Optional)")]
    public bool enableLampFlickerOnWatchStart = true;
    [Range(0f, 1f)] public float watchStartFlickerChance = 0.65f;
    [Min(0)] public int flickerBurstCount = 2;
    [Min(0f)] public float flickerBurstSpacing = 0.12f;

    [Header("Stingers (Optional)")]
    public bool enableStingers = true;
    public AudioClip[] stingerClips;
    public Vector2 stingerInterval = new Vector2(12f, 24f);
    public Vector2 stingerVolumeRange = new Vector2(0.12f, 0.28f);
    public Vector2 stingerPitchRange = new Vector2(0.95f, 1.05f);
    public bool spatializeStingers = true;
    [Range(0f, 1f)] public float behindChance = 0.6f;
    [Min(0f)] public float stingerDistance = 3.5f;
}

